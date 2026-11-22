// Syntrix - shared services framework for Space Engineers mods.
// MIT License, see LICENSE in the repository root.
using System;
using System.Collections.Generic;
using Sandbox.Common.ObjectBuilders;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces;
using Sandbox.ModAPI.Interfaces.Terminal;
using VRage.Game.Components;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRage.Utils;

namespace Syntrix.Time
{
    /// <summary>
    /// Event controller: two own events (API 6, Syntrix 1.5.0), triggered on the server.
    /// - "Time of day": the local time at the block reaches the set time (left toolbar column). The time is the block's
    ///   threshold (0 to 1 = 0 to 24 h), set with the own slider.
    /// - "Day and night": sunrise left column, sunset right column.
    /// The event checks itself whether it is selected, because the block reads its selection before mod events exist.
    /// </summary>
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_EventControllerBlock), false)]
    public class TimeEventLogic : MyGameLogicComponent
    {
        /// <summary>Selection id of "time of day".</summary>
        public const long TimeOfDayId = 1414219880;
        /// <summary>Selection id of "day and night".</summary>
        public const long DayNightId = 1414219881;
        private const string SelectionProperty = "AvailableEvents";

        private IMyEventControllerBlock _block;
        private bool _server, _first = true, _lastValid;
        private double _lastHour;
        private int _lastNight = -1;

        /// <summary>Waits one frame for the grid.</summary>
        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            _block = (IMyEventControllerBlock)Entity;
            NeedsUpdate |= MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
        }

        /// <summary>Adds the two events and the terminal controls.</summary>
        public override void UpdateOnceBeforeFrame()
        {
            try
            {
                // only the active Syntrix copy (several copies may be installed)
                if (_block == null || _block.CubeGrid == null || _block.CubeGrid.Physics == null || !Running)
                    return;
                if (!_block.Components.Has<TimeOfDayEvent>())
                    _block.Components.Add(new TimeOfDayEvent());
                if (!_block.Components.Has<DayNightEvent>())
                    _block.Components.Add(new DayNightEvent());
                TimeControls.EnsureEventController();
                _server = MyAPIGateway.Session.IsServer;
                if (_server)
                    NeedsUpdate |= MyEntityUpdateEnum.EACH_100TH_FRAME;
            }
            catch (Exception e) { Log.Error("time event", e); }
        }

        /// <summary>Server: triggers the selected event.</summary>
        public override void UpdateBeforeSimulation100()
        {
            try
            {
                var time = SyntrixCore.Instance != null ? SyntrixCore.Instance.Time : null;
                if (!_server || time == null || _block == null || _block.MarkedForClose || !_block.IsWorking)
                    return;
                long selected = Selected(_block);
                if (selected != TimeOfDayId && selected != DayNightId)
                {
                    _first = true;
                    return;
                }
                var t = time.Local(_block.GetPosition());
                if (_first || !t.Item1 || !_lastValid)
                {
                    _first = false;
                    Remember(t.Item1, t.Item2, t.Item4);
                    return;
                }
                if (selected == TimeOfDayId)
                {
                    double target = Math.Max(0, Math.Min(1, _block.Threshold)) * 24.0;
                    if (Crossed(_lastHour, t.Item2, target))
                        _block.TriggerAction(0);
                }
                else
                {
                    int n = t.Item4 ? 1 : 0;
                    if (_lastNight >= 0 && n != _lastNight)
                        _block.TriggerAction(t.Item4 ? 1 : 0);
                }
                Remember(t.Item1, t.Item2, t.Item4);
            }
            catch (Exception e) { Log.Error("time event", e); }
        }

        internal static bool Running { get { return SyntrixCore.Instance != null && SyntrixCore.Instance.Started && SyntrixCore.Instance.Time != null; } }

        internal static long Selected(IMyTerminalBlock block)
        {
            try { return block.GetValue<long>(SelectionProperty); }
            catch (Exception) { return 0; }
        }

        private void Remember(bool ok, double hour, bool night)
        {
            _lastValid = ok;
            _lastHour = hour;
            _lastNight = ok ? (night ? 1 : 0) : -1;
        }

        // Did the clock run from a to b across the target (also across midnight)? Jumps over 2 h do not count.
        private static bool Crossed(double a, double b, double target)
        {
            double step = ((b - a) % 24.0 + 24.0) % 24.0;
            if (step <= 0 || step > 2.0)
                return false;
            double distance = ((target - a) % 24.0 + 24.0) % 24.0;
            return distance > 0 && distance <= step;
        }
    }

    /// <summary>Shared part of both events (display only, the logic is in <see cref="TimeEventLogic"/>).</summary>
    public abstract class TimeEventBase : MyEntityComponentBase, IMyEventControllerEntityComponent
    {
        /// <summary>Selection id.</summary>
        public abstract long UniqueSelectionId { get; }
        /// <summary>Name in the event list.</summary>
        public abstract MyStringId EventDisplayName { get; }
        /// <summary>Set by the block.</summary>
        public bool IsSelected { get; set; }
        /// <summary>Text of the left toolbar column.</summary>
        public abstract string YesNoToolbarYesDescription { get; }
        /// <summary>Text of the right toolbar column.</summary>
        public abstract string YesNoToolbarNoDescription { get; }
        /// <summary>The slider is made by <see cref="TimeControls"/>.</summary>
        public void CreateTerminalInterfaceControls<T>() where T : IMyTerminalBlock { }
        /// <summary>Not saved.</summary>
        public override bool IsSerialized() { return false; }
    }

    /// <summary>Event "time of day".</summary>
    public class TimeOfDayEvent : TimeEventBase
    {
        /// <inheritdoc/>
        public override string ComponentTypeDebugString { get { return "Syntrix TimeOfDayEvent"; } }
        /// <inheritdoc/>
        public override long UniqueSelectionId { get { return TimeEventLogic.TimeOfDayId; } }
        /// <inheritdoc/>
        public override MyStringId EventDisplayName { get { return MyStringId.GetOrCompute("Syntrix_Time_Event_TimeOfDay"); } }
        /// <inheritdoc/>
        public override string YesNoToolbarYesDescription { get { return Network.Resolve("Syntrix_Time_Event_TimeOfDay_Yes", "Time reached"); } }
        /// <inheritdoc/>
        public override string YesNoToolbarNoDescription { get { return Network.Resolve("Syntrix_Time_Event_TimeOfDay_No", "(not used)"); } }
    }

    /// <summary>Event "day and night".</summary>
    public class DayNightEvent : TimeEventBase
    {
        /// <inheritdoc/>
        public override string ComponentTypeDebugString { get { return "Syntrix DayNightEvent"; } }
        /// <inheritdoc/>
        public override long UniqueSelectionId { get { return TimeEventLogic.DayNightId; } }
        /// <inheritdoc/>
        public override MyStringId EventDisplayName { get { return MyStringId.GetOrCompute("Syntrix_Time_Event_DayNight"); } }
        /// <inheritdoc/>
        public override string YesNoToolbarYesDescription { get { return Network.Resolve("Syntrix_Time_Event_DayNight_Yes", "Sunrise"); } }
        /// <inheritdoc/>
        public override string YesNoToolbarNoDescription { get { return Network.Resolve("Syntrix_Time_Event_DayNight_No", "Sunset"); } }
    }

    /// <summary>
    /// Programmable block values (API 6, Syntrix 1.5.0), read in an in-game script:
    ///   float h = Me.GetProperty("Syx.LocalTime").As&lt;float&gt;().GetValue(Me);   // hours 0 to 24, -1 = none
    ///   bool night = Me.GetProperty("Syx.Night").AsBool().GetValue(Me);
    ///   float sun = Me.GetProperty("Syx.SunHeight").As&lt;float&gt;().GetValue(Me); // degrees
    ///   string world = Me.GetProperty("Syx.WorldTime").As&lt;string&gt;().GetValue(Me);
    ///   float "Syx.WorldYear", "Syx.WorldDay", "Syx.WorldHour"; string "Syx.Calendar" (name and date, empty without one).
    /// Made with the first programmable block on every machine, also on dedicated servers where the scripts run.
    /// The game makes the vanilla controls of a block type with its first block. Asking for them or adding own ones
    /// earlier leaves the terminal of every block of that type empty (seen on players joining a server, whose grids
    /// arrive later), so everything touching the controls of programmable blocks waits for <see cref="Present"/>.
    /// </summary>
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_MyProgrammableBlock), false)]
    public class TimeProgrammableBlock : MyGameLogicComponent
    {
        /// <summary>True once a programmable block exists on this machine (its vanilla controls are made).</summary>
        public static bool Present { get; private set; }

        /// <summary>Waits one frame (the vanilla controls must exist first).</summary>
        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            if (!Present || !TimeControls.ProgramDone)
                NeedsUpdate |= MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
        }

        /// <summary>Marks programmable blocks as present and makes the properties once.</summary>
        public override void UpdateOnceBeforeFrame()
        {
            Present = true;
            try { TimeControls.EnsureProgrammable(); }
            catch (Exception e) { Log.Error("time properties", e); }
        }

        /// <summary>End of session.</summary>
        internal static void Reset() { Present = false; }
    }

    /// <summary>Terminal controls and properties of the time service, made once per session.</summary>
    public static class TimeControls
    {
        /// <summary>True once the slider of the event controller is made.</summary>
        public static bool EventDone { get; private set; }
        /// <summary>True once the properties of the programmable block are made.</summary>
        public static bool ProgramDone { get; private set; }

        private static readonly List<IMyTerminalControl> _made = new List<IMyTerminalControl>();

        /// <summary>Makes the slider of the event controller (called by an event controller, so its controls exist).</summary>
        public static void EnsureEventController()
        {
            if (EventDone || !TimeEventLogic.Running)
                return;
            EventDone = true;
            var tc = MyAPIGateway.TerminalControls;

            var slider = tc.CreateControl<IMyTerminalControlSlider, IMyEventControllerBlock>("Syntrix_TimeOfDay");
            slider.Title = MyStringId.GetOrCompute("Syntrix_Time_Event_Slider");
            slider.Tooltip = MyStringId.GetOrCompute("Syntrix_Time_Event_Slider_Tip");
            slider.SetLimits(0f, 24f);
            slider.Getter = b => ((IMyEventControllerBlock)b).Threshold * 24f;
            slider.Setter = (b, v) =>
            {
                float h = (float)(Math.Round(v * 4f) / 4f);
                ((IMyEventControllerBlock)b).Threshold = Math.Max(0f, Math.Min(1f, h / 24f));
            };
            slider.Writer = (b, sb) => sb.Append(TimeService.Clock(((IMyEventControllerBlock)b).Threshold * 24.0, true, false));
            slider.Visible = b => TimeEventLogic.Selected(b) == TimeEventLogic.TimeOfDayId;
            slider.SupportsMultipleBlocks = true;
            tc.AddControl<IMyEventControllerBlock>(slider);
            _made.Add(slider);
        }

        /// <summary>Makes the properties of the programmable block (only once one exists, see <see cref="TimeProgrammableBlock"/>).</summary>
        public static void EnsureProgrammable()
        {
            if (ProgramDone || !TimeProgrammableBlock.Present || !TimeEventLogic.Running)
                return;
            ProgramDone = true;
            Property("Syx.LocalTime", b => { var t = Time.Local(b.GetPosition()); return t.Item1 ? (float)t.Item2 : -1f; });
            Property("Syx.Night", b => Time.IsNight(b.GetPosition()));
            Property("Syx.SunHeight", b => Time.Local(b.GetPosition()).Item3);
            Property("Syx.WorldTime", b => Time.WorldText(false));
            Property("Syx.WorldYear", b => (float)Time.WorldDate().Item1);
            Property("Syx.WorldDay", b => (float)Time.WorldDate().Item2);
            Property("Syx.WorldHour", b => (float)Time.WorldDate().Item3);
            Property("Syx.Calendar", b => { var c = Time.CalendarAt(b.GetPosition()); return c.Item1 == null ? "" : c.Item1 + " " + c.Item2; });
        }

        /// <summary>Removes the controls (end of session).</summary>
        public static void Unload()
        {
            TimeProgrammableBlock.Reset();
            if (!EventDone && !ProgramDone)
                return;
            EventDone = ProgramDone = false;
            var tc = MyAPIGateway.TerminalControls;
            foreach (var c in _made)
            {
                try
                {
                    if (c is IMyTerminalControlSlider)
                        tc.RemoveControl<IMyEventControllerBlock>(c);
                    else
                        tc.RemoveControl<IMyProgrammableBlock>(c);
                }
                catch (Exception) { }
            }
            _made.Clear();
        }

        private static TimeService Time { get { return SyntrixCore.Instance.Time; } }

        private static void Property<T>(string id, Func<IMyTerminalBlock, T> getter)
        {
            var p = MyAPIGateway.TerminalControls.CreateProperty<T, IMyProgrammableBlock>(id);
            p.Getter = b =>
            {
                try { return SyntrixCore.Instance != null && SyntrixCore.Instance.Time != null ? getter(b) : default(T); }
                catch (Exception) { return default(T); }
            };
            p.Setter = (b, v) => { };
            MyAPIGateway.TerminalControls.AddControl<IMyProgrammableBlock>(p);
            _made.Add(p);
        }
    }
}
