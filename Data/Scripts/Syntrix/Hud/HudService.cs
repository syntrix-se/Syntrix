// Syntrix - shared services framework for Space Engineers mods.
// MIT License, see LICENSE in the repository root.
using System;
using System.Collections.Generic;
using VRage;

namespace Syntrix
{
    /// <summary>
    /// HUD values (API 6, 1.5.0): a shared table of values on the player's machine that mods write (a mod for the
    /// environment, hover thrusters, shields, the world clock) and any number of displays read (a HUD mod, AutoLCD
    /// commands). Keys carry a mod prefix ("Hover.Target"). Values are double or string; null removes the key.
    /// Syntrix keeps the last value of every key, so a display that docks later still sees everything sent before,
    /// and raises <see cref="ChangedEvent"/> with MyTuple&lt;string key, object value&gt; when a value changes.
    /// Players only; on a dedicated server all calls do nothing.
    /// </summary>
    public class HudService
    {
        /// <summary>Event raised on the player's machine when a value changed: MyTuple&lt;string, object&gt; (value null = removed).</summary>
        public const string ChangedEvent = "Syntrix.Hud";

        private const int MaxKeyLength = 128;
        private const int MaxTextLength = 256;
        private const int MaxKeys = 1024;

        private readonly SyntrixCore _core;
        private readonly Dictionary<string, object> _values = new Dictionary<string, object>();

        /// <summary>Makes the service.</summary>
        public HudService(SyntrixCore core) { _core = core; }

        /// <summary>Forgets all values.</summary>
        public void Stop() { _values.Clear(); }

        /// <summary>Sets a value: double (int, float and bool are turned into double), string, or null to remove it.</summary>
        public void Set(string key, object value)
        {
            if (!_core.IsPlayer || string.IsNullOrEmpty(key) || key.Length > MaxKeyLength)
                return;
            object v = Normalize(value);
            if (value != null && v == null)
            {
                Log.Info("HUD value '" + key + "' ignored: type " + value.GetType().Name + " is not double or string");
                return;
            }
            object old;
            bool had = _values.TryGetValue(key, out old);
            if (v == null)
            {
                if (!had)
                    return;
                _values.Remove(key);
            }
            else
            {
                if (had && old.Equals(v))
                    return;
                if (!had && _values.Count >= MaxKeys)
                    return;
                _values[key] = v;
            }
            _core.Events.Publish(ChangedEvent, new MyTuple<string, object>(key, v));
        }

        /// <summary>The value of a key (double or string), null when there is none.</summary>
        public object Get(string key)
        {
            object v;
            return key != null && _values.TryGetValue(key, out v) ? v : null;
        }

        /// <summary>All keys that have a value right now.</summary>
        public string[] Keys()
        {
            var keys = new string[_values.Count];
            _values.Keys.CopyTo(keys, 0);
            return keys;
        }

        private static object Normalize(object value)
        {
            if (value == null)
                return null;
            if (value is double)
                return value;
            if (value is float)
                return (double)(float)value;
            if (value is int)
                return (double)(int)value;
            if (value is long)
                return (double)(long)value;
            if (value is bool)
                return (bool)value ? 1.0 : 0.0;
            var s = value as string;
            if (s != null)
                return s.Length > MaxTextLength ? s.Substring(0, MaxTextLength) : s;
            return null;
        }
    }
}
