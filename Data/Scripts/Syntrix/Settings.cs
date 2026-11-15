// Syntrix - shared services framework for Space Engineers mods.
// MIT License, see LICENSE in the repository root.
using System;
using System.Collections.Generic;

namespace Syntrix
{
    /// <summary>
    /// Settings texts per mod and file (API 2). The dock file reads and writes the file in the
    /// mod's own world storage on the server and publishes the text here; Syntrix keeps it and
    /// hands it to every player: once when the player's Syntrix starts (it asks the server)
    /// and again whenever the server publishes a new text.
    /// </summary>
    public class SettingsService
    {
        const char Sep = '\u001F';

        readonly SyntrixCore _core;
        readonly Dictionary<string, string> _values = new Dictionary<string, string>();
        readonly Dictionary<string, List<Action<string>>> _subs = new Dictionary<string, List<Action<string>>>();

        public SettingsService(SyntrixCore core) { _core = core; }

        public void Start()
        {
            if (!_core.IsServer)
                _core.Net.RawToServer(new Packet { Kind = PacketKind.SettingsRequest });
        }

        public void Stop()
        {
            _values.Clear();
            _subs.Clear();
        }

        static string Key(string mod, string file) { return mod + Sep + file; }

        /// <summary>Server: keep the text and send it to every other machine.</summary>
        public void Publish(string mod, string file, string text)
        {
            if (!_core.IsServer || string.IsNullOrEmpty(mod) || string.IsNullOrEmpty(file))
                return;
            text = text ?? "";
            if (text.Length > Protocol.MaxSettingsLength)
            {
                Log.Warn("settings '" + file + "' of " + mod + " have " + text.Length + " characters, more than "
                    + Protocol.MaxSettingsLength + "; players do not get them.");
                return;
            }
            _values[Key(mod, file)] = text;
            _core.Net.RawToOthers(Value(mod, file, text));
        }

        /// <summary>Player: handler runs with the server's text, at once if it is already here.</summary>
        public void Subscribe(string mod, string file, Action<string> handler)
        {
            if (_core.IsServer || string.IsNullOrEmpty(mod) || string.IsNullOrEmpty(file) || handler == null)
                return;
            string key = Key(mod, file);
            List<Action<string>> list;
            if (!_subs.TryGetValue(key, out list))
                _subs[key] = list = new List<Action<string>>();
            if (!list.Contains(handler))
                list.Add(handler);
            string text;
            if (_values.TryGetValue(key, out text))
                Call(mod, file, handler, text);
        }

        static Packet Value(string mod, string file, string text)
        {
            return new Packet { Kind = PacketKind.SettingsValue, Channel = mod, Texts = new[] { file, text } };
        }

        public void OnPacket(Packet p, ulong sender, bool fromServer)
        {
            if (p.Kind == PacketKind.SettingsRequest)
            {
                if (!_core.IsServer)
                    return;
                foreach (var kv in _values)
                {
                    int i = kv.Key.IndexOf(Sep);
                    _core.Net.RawToPlayer(Value(kv.Key.Substring(0, i), kv.Key.Substring(i + 1), kv.Value), sender);
                }
            }
            else if (p.Kind == PacketKind.SettingsValue)
            {
                if (_core.IsServer || !fromServer || p.Channel == null || p.Texts == null || p.Texts.Length != 2)
                    return;
                string key = Key(p.Channel, p.Texts[0]);
                string text = p.Texts[1] ?? "";
                _values[key] = text;
                List<Action<string>> list;
                if (_subs.TryGetValue(key, out list))
                    foreach (var h in list.ToArray())
                        Call(p.Channel, p.Texts[0], h, text);
            }
        }

        static void Call(string mod, string file, Action<string> handler, string text)
        {
            try { handler(text); }
            catch (Exception e) { Log.Error("settings '" + file + "' of " + mod, e); }
        }
    }
}
