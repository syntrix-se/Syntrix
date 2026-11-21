// Syntrix - shared services framework for Space Engineers mods.
// MIT License, see LICENSE in the repository root.
using System;
using System.Collections.Generic;
using System.Globalization;
using Sandbox.Definitions;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using VRage;
using VRage.Game;
using VRage.Game.ModAPI;
using VRageMath;

namespace Syntrix.Time
{
    /// <summary>
    /// World clock, local time and planet calendars (API 6, Syntrix 1.5.0).
    /// - World time: computed from the real UTC time and <see cref="TimeSettings"/>, the same on every server. Player
    ///   machines align their clock with the server once after joining.
    /// - Local time: hour angle of the sun at a place near a planet (12:00 sun highest, 06:00 sunrise). The game turns
    ///   the sun around the whole world, planets do not rotate.
    /// - Server: couples the sun to the world time (optional). Player: turns the sky with the sun (optional) and
    ///   publishes the HUD values. Event controller events and programmable block values: <see cref="TimeBlocks"/>.
    /// </summary>
    public sealed class TimeService
    {
        /// <summary>File in the world storage of Syntrix.</summary>
        public const string FileName = "Syntrix-Time.xml";
        /// <summary>Server: a new world day began, MyTuple&lt;int year, int day&gt;.</summary>
        public const string NewDayEvent = "Syntrix.Time.NewDay";
        /// <summary>Local time applies up to this multiple of the planet radius (orbits included).</summary>
        public const double Range = 2.0;
        /// <summary>Like the game's night check: sun direction dot local up below -0.1 is night.</summary>
        public const float NightThreshold = -0.1f;

        private static readonly DateTime GameStart = new DateTime(2081, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        private readonly SyntrixCore _core;
        private TimeSettings _settings = new TimeSettings();
        private string _settingsXml;
        private DateTime _reference = new DateTime(2026, 11, 11, 0, 0, 0, DateTimeKind.Utc);
        private readonly List<Calendar> _calendars = new List<Calendar>();
        private TimeSpan _offset = TimeSpan.Zero;

        private bool _axisKnown;
        private Vector3 _axis = Vector3.Up;

        // sky (player)
        private bool _sky, _baseRead;
        private Quaternion _base = Quaternion.Identity;
        private MyOrientation _baseOrientation;
        // sun coupling (server)
        private bool _couple;
        private int _alignIn = 600;
        // HUD (player)
        private int _hudTick;
        private int _hudLocalMinute = int.MinValue, _hudWorldMinute = int.MinValue, _hudNight = -2;
        private string _hudCalendar = "\u0000";
        private bool _hudSent;
        // new day (server)
        private long _lastDay = long.MinValue;
        private int _dayTick;

        private sealed class Calendar
        {
            public string Name;
            public string[] Planets;
            public bool Days;
            public DateTime StartDate;
            public long StartNumber;
        }

        /// <summary>Makes the service.</summary>
        public TimeService(SyntrixCore core) { _core = core; }

        // ---------------------------------------------------------------- start and stop

        /// <summary>Server: reads or writes the settings. Player: asks the server for settings and clock.</summary>
        public void Start()
        {
            if (_core.IsServer)
                LoadSettings();
            else
                Request();
            _core.Events.Subscribe(AutoLcdReady, OnAutoLcdReady);
            if (_core.IsServer)
                RegisterAutoLcd();
            _core.Chat.Register("Syntrix", "syxtime", (int)MyPromoteLevel.None, false, "local time, sun height, world date", OnChat);
            Apply();
        }

        /// <summary>Stops and gives the sky its orientation back.</summary>
        public void Stop()
        {
            if (_baseRead)
            {
                try { MyDefinitionManager.Static.EnvironmentDefinition.EnvironmentOrientation = _baseOrientation; }
                catch (Exception) { }
            }
            _core.Events.Unsubscribe(AutoLcdReady, OnAutoLcdReady);
            _calendars.Clear();
            _axisKnown = false;
        }

        private void Apply()
        {
            _sky = _core.IsPlayer && _settings.RotateSky && SunActive;
            _couple = _core.IsServer && _settings.CoupleSun && SunActive;
        }

        // ---------------------------------------------------------------- settings

        private void LoadSettings()
        {
            string text = null;
            try
            {
                if (MyAPIGateway.Utilities.FileExistsInWorldStorage(FileName, typeof(TimeService)))
                    using (var r = MyAPIGateway.Utilities.ReadFileInWorldStorage(FileName, typeof(TimeService)))
                        text = r.ReadToEnd();
            }
            catch (Exception e) { Log.Error("time settings", e); }
            if (text == null)
            {
                text = MyAPIGateway.Utilities.SerializeToXML(new TimeSettings());
                try
                {
                    using (var w = MyAPIGateway.Utilities.WriteFileInWorldStorage(FileName, typeof(TimeService)))
                        w.Write(text);
                }
                catch (Exception e) { Log.Error("time settings", e); }
            }
            Read(text);
        }

        private void Read(string text)
        {
            var s = new TimeSettings();
            try { s = MyAPIGateway.Utilities.SerializeFromXML<TimeSettings>(text) ?? new TimeSettings(); }
            catch (Exception e) { Log.Warn(FileName + " not readable, defaults: " + e.Message); }
            if (s.Factor <= 0)
                s.Factor = 6.0;
            if (s.YearLength < 1)
                s.YearLength = 365;
            DateTime reference;
            if (DateTime.TryParse(s.ReferenceDate, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out reference))
                _reference = DateTime.SpecifyKind(reference, DateTimeKind.Utc);
            else
                Log.Warn(FileName + ": ReferenceDate '" + s.ReferenceDate + "' not readable");
            _calendars.Clear();
            if (s.Calendars != null)
                foreach (var c in s.Calendars)
                {
                    if (c == null || string.IsNullOrWhiteSpace(c.Planets))
                        continue;
                    var cal = new Calendar
                    {
                        Name = c.Name ?? "",
                        Planets = c.Planets.Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries),
                        Days = string.Equals(c.Kind, "Days", StringComparison.OrdinalIgnoreCase),
                    };
                    DateTime d;
                    if (cal.Days)
                        long.TryParse(c.Start, NumberStyles.Integer, CultureInfo.InvariantCulture, out cal.StartNumber);
                    else if (DateTime.TryParse(c.Start, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out d))
                        cal.StartDate = DateTime.SpecifyKind(d.Date, DateTimeKind.Utc);
                    else
                    {
                        Log.Warn(FileName + ": calendar '" + c.Name + "' start '" + c.Start + "' not readable");
                        continue;
                    }
                    _calendars.Add(cal);
                }
            _settings = s;
            _settingsXml = text;
            Log.Info("World clock: factor " + s.Factor.ToString(CultureInfo.InvariantCulture) + ", reference "
                + _reference.ToString("u", CultureInfo.InvariantCulture) + ", start year " + s.StartYear + " day " + s.StartDay
                + ", " + _calendars.Count + " planet calendars");
        }

        /// <summary>The settings in use.</summary>
        public TimeSettings Settings { get { return _settings; } }

        // ---------------------------------------------------------------- network

        private void Request()
        {
            _core.Net.RawToServer(new Packet { Kind = PacketKind.TimeRequest, Data = BitConverter.GetBytes(DateTime.UtcNow.Ticks) });
        }

        /// <summary>Server: the player's send time comes back with the server's UTC and the settings. Player: sets both.</summary>
        public void OnPacket(Packet p, ulong sender)
        {
            if (p.Kind == PacketKind.TimeRequest)
            {
                if (!_core.IsServer || p.Data == null || p.Data.Length < 8)
                    return;
                var answer = new byte[16];
                Array.Copy(p.Data, 0, answer, 0, 8);
                Array.Copy(BitConverter.GetBytes(DateTime.UtcNow.Ticks), 0, answer, 8, 8);
                _core.Net.RawToPlayer(new Packet { Kind = PacketKind.Time, Data = answer, Texts = new[] { _settingsXml ?? "" } }, sender);
                return;
            }
            if (_core.IsServer)
                return;
            if (p.Texts != null && p.Texts.Length > 0 && p.Texts[0].Length > 0)
            {
                Read(p.Texts[0]);
                Apply();
            }
            if (p.Data != null && p.Data.Length >= 16)
            {
                long sent = BitConverter.ToInt64(p.Data, 0);
                long server = BitConverter.ToInt64(p.Data, 8);
                long now = DateTime.UtcNow.Ticks;
                _offset = TimeSpan.FromTicks(server - (sent + (now - sent) / 2));
                Log.Info("Clock offset to the server " + _offset.TotalSeconds.ToString("0.0", CultureInfo.InvariantCulture) + " s");
            }
        }

        // ---------------------------------------------------------------- world time

        /// <summary>Real UTC time, aligned to the server clock.</summary>
        public DateTime Utc { get { return DateTime.UtcNow + _offset; } }

        /// <summary>World days since year 1, day 1, 00:00 (fraction = time of day).</summary>
        public double WorldDays
        {
            get
            {
                var s = _settings;
                double start = (double)(s.StartYear - 1) * s.YearLength + (s.StartDay - 1) + s.StartHour / 24.0;
                return start + (Utc - _reference).TotalDays * s.Factor;
            }
        }

        /// <summary>World date: year, day of the year from 1, hour 0 to 24.</summary>
        public MyTuple<int, int, double> WorldDate()
        {
            double total = WorldDays;
            double whole = Math.Floor(total);
            long days = (long)whole;
            long length = _settings.YearLength;
            long y = days >= 0 ? days / length : -((-days - 1) / length) - 1;
            return new MyTuple<int, int, double>((int)y + 1, (int)(days - y * length) + 1, (total - whole) * 24.0);
        }

        /// <summary>World date as text in the language of this machine.</summary>
        public string WorldText(bool twelveHour)
        {
            var d = WorldDate();
            return string.Format(Network.Resolve("Syntrix_Time_WorldFormat", "Year {0}, day {1}, {2}"), d.Item1, d.Item2, Clock(d.Item3, true, twelveHour));
        }

        /// <summary>"HH:mm" (or 12 hour form) from hours; "--:--" when not valid.</summary>
        public static string Clock(double hours, bool valid, bool twelveHour)
        {
            if (!valid)
                return "--:--";
            int minutes = (int)Math.Floor(hours * 60.0 + 1e-6) % 1440;
            int h = minutes / 60, m = minutes % 60;
            if (!twelveHour)
                return h.ToString("00", CultureInfo.InvariantCulture) + ":" + m.ToString("00", CultureInfo.InvariantCulture);
            int h12 = h % 12;
            return (h12 == 0 ? 12 : h12).ToString("00", CultureInfo.InvariantCulture) + ":" + m.ToString("00", CultureInfo.InvariantCulture) + (h < 12 ? " AM" : " PM");
        }

        // ---------------------------------------------------------------- sun and local time

        /// <summary>Sun rotation is on in this world.</summary>
        public bool SunActive
        {
            get
            {
                return MyAPIGateway.Session != null && MyAPIGateway.Session.SessionSettings.EnableSunRotation
                    && MyVisualScriptLogicProvider.SunRotationGetDayLength() > 0f;
            }
        }

        /// <summary>Length of a solar day in real seconds (0 = no rotation).</summary>
        public double DaySeconds { get { return MyVisualScriptLogicProvider.SunRotationGetDayLength(); } }

        /// <summary>Rotation axis of the sun, computed like the game does from the world's start direction.</summary>
        public Vector3 Axis
        {
            get
            {
                if (_axisKnown)
                    return _axis;
                _axisKnown = true;
                try
                {
                    var sector = MyAPIGateway.Session.GetSector();
                    Vector3 b;
                    Vector3.CreateFromAzimuthAndElevation(sector.Environment.SunAzimuth, sector.Environment.SunElevation, out b);
                    b.Normalize();
                    Vector3 a = Math.Abs(Vector3.Dot(b, Vector3.Up)) > 0.95f
                        ? Vector3.Cross(Vector3.Cross(b, Vector3.Left), b)
                        : Vector3.Cross(Vector3.Cross(b, Vector3.Up), b);
                    _axis = a.LengthSquared() > 1e-6f ? Vector3.Normalize(a) : Vector3.Up;
                }
                catch (Exception) { _axis = Vector3.Up; }
                return _axis;
            }
        }

        /// <summary>Local days at the reference meridian; a local day lasts one solar day of this server.</summary>
        public double LocalDaysTotal
        {
            get
            {
                double total = WorldDays, day = DaySeconds;
                return day <= 0 ? total : total * (86400.0 / _settings.Factor) / day;
            }
        }

        // -Z side of every planet (longitude 0 of a cube map planet); -X if -Z lies on the axis.
        private Vector3 Reference
        {
            get { return Math.Abs(Vector3.Dot(Axis, Vector3.Forward)) < 0.95f ? Vector3.Forward : Vector3.Left; }
        }

        /// <summary>Planet whose local time applies at pos, or null.</summary>
        public MyPlanet PlanetAt(Vector3D pos)
        {
            var planet = MyGamePruningStructure.GetClosestPlanet(pos);
            if (planet == null)
                return null;
            double r = Math.Max(planet.MaximumRadius, 1.0) * Range;
            return Vector3D.DistanceSquared(pos, planet.PositionComp.GetPosition()) > r * r ? null : planet;
        }

        /// <summary>Local time at pos: valid, hours 0 to 24, sun height in degrees, night.</summary>
        public MyTuple<bool, double, float, bool> Local(Vector3D pos)
        {
            var none = new MyTuple<bool, double, float, bool>(false, 0, 0f, false);
            if (!SunActive)
                return none;
            var planet = PlanetAt(pos);
            if (planet == null)
                return none;
            Vector3D d = pos - planet.PositionComp.GetPosition();
            double len = d.Length();
            if (len < 1e-3)
                return none;
            Vector3 u = (Vector3)(d / len);
            Vector3 s = MyVisualScriptLogicProvider.GetSunDirection();
            float su = Vector3.Dot(s, u);
            float height = MathHelper.ToDegrees((float)Math.Asin(MathHelper.Clamp(su, -1f, 1f)));
            double hours;
            bool ok = HourAngle(u, s, out hours);
            return new MyTuple<bool, double, float, bool>(ok, hours, height, su < NightThreshold);
        }

        private bool HourAngle(Vector3 u, Vector3 s, out double hours)
        {
            hours = 0;
            Vector3 a = Axis;
            Vector3 uS = u - Vector3.Dot(u, a) * a;
            Vector3 sS = s - Vector3.Dot(s, a) * a;
            if (uS.Length() < 0.02f || sS.Length() < 0.02f)
                return false;   // at the axis the sun runs along the horizon
            double phi = Math.Atan2(Vector3.Dot(a, Vector3.Cross(uS, sS)), Vector3.Dot(uS, sS));
            hours = ((12.0 + phi * 12.0 / Math.PI) % 24.0 + 24.0) % 24.0;
            return true;
        }

        /// <summary>Local time at pos as text, "--:--" without one.</summary>
        public string LocalText(Vector3D pos, bool twelveHour)
        {
            var t = Local(pos);
            return Clock(t.Item2, t.Item1, twelveHour);
        }

        /// <summary>Night at pos by sun height (false without local time).</summary>
        public bool IsNight(Vector3D pos)
        {
            var t = Local(pos);
            return t.Item1 && t.Item4;
        }

        /// <summary>Planet calendar at pos: name and date text, both null without one.</summary>
        public MyTuple<string, string> CalendarAt(Vector3D pos)
        {
            var none = new MyTuple<string, string>(null, null);
            var planet = PlanetAt(pos);
            if (planet == null || planet.Generator == null || _calendars.Count == 0)
                return none;
            string subtype = planet.Generator.Id.SubtypeName;
            Calendar cal = null;
            foreach (var c in _calendars)
                foreach (var p in c.Planets)
                    if (string.Equals(p, subtype, StringComparison.OrdinalIgnoreCase))
                        cal = c;
            if (cal == null)
                return none;
            var t = Local(pos);
            if (!t.Item1)
                return none;
            long day = LocalDay(t.Item2);
            string name = Network.Resolve("Syntrix_Time_Calendar_" + cal.Name, cal.Name);
            if (cal.Days)
                return new MyTuple<string, string>(name, (cal.StartNumber + day).ToString(CultureInfo.InvariantCulture));
            DateTime start = cal.StartDate;
            if (day < -(long)(start - DateTime.MinValue).TotalDays || day > (long)(DateTime.MaxValue - start).TotalDays - 1)
                return none;
            return new MyTuple<string, string>(name, start.AddDays(day).ToString("dd.MM.yyyy", CultureInfo.InvariantCulture));
        }

        // Local day number at a place with this local time; changes at 00:00 local time.
        private long LocalDay(double hours)
        {
            double e = LocalDaysTotal;
            double offset = ((hours / 24.0 - e) % 1.0 + 1.0) % 1.0;
            if (offset > 0.5)
                offset -= 1.0;
            return (long)Math.Floor(e + offset + 1e-9);
        }

        // ---------------------------------------------------------------- every frame

        /// <summary>Every frame: sun coupling and new day (server), sky and HUD (player).</summary>
        public void Tick()
        {
            try
            {
                if (_couple && --_alignIn <= 0)
                {
                    _alignIn = 3600;
                    AlignSun();
                }
                if (_core.IsServer && ++_dayTick >= 60)
                {
                    _dayTick = 0;
                    CheckDay();
                }
                if (_sky)
                    RotateSky();
                if (_core.IsPlayer && ++_hudTick >= 60)
                {
                    _hudTick = 0;
                    SendHud();
                }
            }
            catch (Exception e)
            {
                Log.Error("time", e);
                _sky = false;
                _couple = false;
            }
        }

        private void CheckDay()
        {
            long day = (long)Math.Floor(WorldDays);
            if (_lastDay != long.MinValue && day != _lastDay)
            {
                var d = WorldDate();
                _core.Events.Publish(NewDayEvent, new MyTuple<int, int>(d.Item1, d.Item2));
            }
            _lastDay = day;
        }

        // Game time = origin + 60 * minutes of the admin "time of day" slider; only forward, at most one solar day.
        private void AlignSun()
        {
            double actual;
            if (!SunActive || !HourAngle(Reference, MyVisualScriptLogicProvider.GetSunDirection(), out actual))
                return;
            double day = DaySeconds;
            double target = ((LocalDaysTotal % 1.0) + 1.0) % 1.0 * 24.0;
            double ahead = ((((target - actual) / 24.0) % 1.0) + 1.0) % 1.0 * day;
            if (ahead < 10.0 || ahead > day - 120.0)
                return;
            double before = GameSeconds;
            double goal = before + ahead;
            MyVisualScriptLogicProvider.SunRotationSetTime(0f);
            double origin = GameSeconds;
            MyVisualScriptLogicProvider.SunRotationSetTime((float)((goal - origin) / 60.0));
            if (Math.Abs(GameSeconds - goal) > 5.0)
            {
                Log.Warn("Sun alignment failed, coupling off");
                _couple = false;
                return;
            }
            Log.Info("Sun aligned to world time, game time +" + ahead.ToString("0", CultureInfo.InvariantCulture) + " s");
        }

        private static double GameSeconds { get { return (MyAPIGateway.Session.GameDateTime - GameStart).TotalSeconds; } }

        private void RotateSky()
        {
            var env = MyDefinitionManager.Static.EnvironmentDefinition;
            if (!_baseRead)
            {
                _baseRead = true;
                _baseOrientation = env.EnvironmentOrientation;
                _base = _baseOrientation.ToQuaternion();
            }
            double day = DaySeconds;
            float angle = day <= 0 ? 0f : (float)(2.0 * Math.PI * ((MyAPIGateway.Session.GameDateTime - GameStart).TotalSeconds / day));
            var q = Quaternion.Concatenate(_base, Quaternion.CreateFromAxisAngle(Axis, angle));
            Vector3 e = MyMath.QuaternionToEuler(q);
            env.EnvironmentOrientation = new MyOrientation(e.Y, e.X, e.Z);
        }

        // HUD keys: Time.Local, Time.Night, Time.SunHeight, Time.WorldYear, Time.WorldDay, Time.WorldHour,
        // Time.Calendar (name), Time.CalendarDate (text).
        private void SendHud()
        {
            var player = MyAPIGateway.Session.Player;
            if (player == null)
                return;
            Vector3D pos = player.GetPosition();
            var t = Local(pos);
            int localMinute = t.Item1 ? (int)Math.Floor(t.Item2 * 60.0) : -1;
            int night = t.Item1 ? (t.Item4 ? 1 : 0) : -1;
            var d = WorldDate();
            int worldMinute = (int)Math.Floor(d.Item3 * 60.0) + 1440 * d.Item2;
            var cal = CalendarAt(pos);
            string calendar = cal.Item1 == null ? null : cal.Item1 + "|" + cal.Item2;
            if (_hudSent && localMinute == _hudLocalMinute && night == _hudNight && worldMinute == _hudWorldMinute && calendar == _hudCalendar)
                return;
            if (!_hudSent || calendar != _hudCalendar)
            {
                Hud("Time.Calendar", cal.Item1);
                Hud("Time.CalendarDate", cal.Item2);
            }
            if (!_hudSent || localMinute != _hudLocalMinute || night != _hudNight)
            {
                Hud("Time.Local", t.Item1 ? (object)(localMinute / 60.0) : null);
                Hud("Time.Night", t.Item1 ? (object)(double)night : null);
                Hud("Time.SunHeight", t.Item1 ? (object)Math.Round((double)t.Item3, 1) : null);
            }
            if (!_hudSent || worldMinute != _hudWorldMinute)
            {
                Hud("Time.WorldYear", (double)d.Item1);
                Hud("Time.WorldDay", (double)d.Item2);
                Hud("Time.WorldHour", Math.Floor(d.Item3 * 60.0) / 60.0);
            }
            _hudLocalMinute = localMinute;
            _hudNight = night;
            _hudWorldMinute = worldMinute;
            _hudCalendar = calendar;
            _hudSent = true;
        }

        private void Hud(string key, object value)
        {
            _core.Hud.Set(key, value);   // shared HUD values (HudService), only changes go out
        }

        // ---------------------------------------------------------------- chat

        private string OnChat(ulong sender, string[] args)
        {
            var player = MyAPIGateway.Session.Player;
            if (player == null)
                return null;
            var pos = player.GetPosition();
            var t = Local(pos);
            var cal = CalendarAt(pos);
            var d = WorldDate();
            var axis = Axis;
            return Network.Resolve("Syntrix_Time_LocalTime", "Local time") + " " + Clock(t.Item2, t.Item1, false)
                + (t.Item1 ? " (" + t.Item3.ToString("0.0", CultureInfo.InvariantCulture) + "°" + (t.Item4 ? ", " + Network.Resolve("Syntrix_Time_Night", "night") : "") + ")" : "")
                + (cal.Item1 != null ? " · " + cal.Item1 + " " + cal.Item2 : "")
                + " · " + Network.Resolve("Syntrix_Time_WorldTime", "World time") + " " + WorldText(false)
                + " · UTC " + Utc.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture) + "Z"
                + " · " + d.Item1 + "/" + d.Item2 + " " + d.Item3.ToString("0.0000", CultureInfo.InvariantCulture) + " h"
                + " · axis " + axis.X.ToString("0.000", CultureInfo.InvariantCulture) + " " + axis.Y.ToString("0.000", CultureInfo.InvariantCulture)
                + " " + axis.Z.ToString("0.000", CultureInfo.InvariantCulture);
        }

        // ---------------------------------------------------------------- AutoLCD commands (server, where screens are written)

        private const string AutoLcdRegister = "SyxAutoLcd.RegisterCommand";
        private const string AutoLcdReady = "SyxAutoLcd.Ready";

        private void OnAutoLcdReady(object payload)
        {
            if (_core.IsServer)
                RegisterAutoLcd();
        }

        private void RegisterAutoLcd()
        {
            _core.Events.Publish(AutoLcdRegister, new MyTuple<string, Func<IMyTerminalBlock, string[], List<MyTuple<string, string, float>>>>("WorldTime", LcdWorld));
            _core.Events.Publish(AutoLcdRegister, new MyTuple<string, Func<IMyTerminalBlock, string[], List<MyTuple<string, string, float>>>>("LocalTime", LcdLocal));
        }

        // "WorldTime": world date and time.
        private List<MyTuple<string, string, float>> LcdWorld(IMyTerminalBlock block, string[] args)
        {
            return new List<MyTuple<string, string, float>>
            {
                new MyTuple<string, string, float>(Network.Resolve("Syntrix_Time_WorldTime", "World time"), WorldText(false), -1f),
            };
        }

        // "LocalTime": local time at the screen, night, and the planet calendar if there is one.
        private List<MyTuple<string, string, float>> LcdLocal(IMyTerminalBlock block, string[] args)
        {
            var rows = new List<MyTuple<string, string, float>>();
            if (block == null)
                return rows;
            var pos = block.GetPosition();
            var t = Local(pos);
            string value = Clock(t.Item2, t.Item1, false) + (t.Item1 && t.Item4 ? " (" + Network.Resolve("Syntrix_Time_Night", "night") + ")" : "");
            rows.Add(new MyTuple<string, string, float>(Network.Resolve("Syntrix_Time_LocalTime", "Local time"), value, -1f));
            var cal = CalendarAt(pos);
            if (cal.Item1 != null)
                rows.Add(new MyTuple<string, string, float>(cal.Item1, cal.Item2, -1f));
            return rows;
        }
    }
}
