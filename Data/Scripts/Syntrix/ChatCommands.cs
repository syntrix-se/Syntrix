// Syntrix - shared services framework for Space Engineers mods.
// MIT License, see LICENSE in the repository root.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sandbox.ModAPI;
using VRage.Game.ModAPI;

namespace Syntrix
{
    /// <summary>
    /// One chat hook for all mods: "/command args". Checks the promote level on the
    /// player and again on the server, runs the handler where the mod asked for it
    /// and shows the reply to the player who typed the command.
    /// </summary>
    public class ChatCommands
    {
        class Entry
        {
            public string Mod;
            public string Command;
            public MyPromoteLevel Level;
            public bool OnServer;
            public string Help;
            public Func<ulong, string[], string> Handler;
        }

        readonly SyntrixCore _core;
        readonly Dictionary<string, Entry> _commands = new Dictionary<string, Entry>(StringComparer.OrdinalIgnoreCase);

        public ChatCommands(SyntrixCore core) { _core = core; }

        public void Start()
        {
            if (_core.IsPlayer)
                MyAPIGateway.Utilities.MessageEnteredSender += OnMessageEntered;
        }

        public void Stop()
        {
            if (_core.IsPlayer)
                MyAPIGateway.Utilities.MessageEnteredSender -= OnMessageEntered;
            _commands.Clear();
        }

        public void Register(string mod, string command, int level, bool onServer, string help, Func<ulong, string[], string> handler)
        {
            if (string.IsNullOrWhiteSpace(command) || handler == null)
                return;
            command = command.Trim().TrimStart('/');
            if (command.Length == 0 || command.Contains(" ") || command.Equals("syntrix", StringComparison.OrdinalIgnoreCase))
                return;
            Entry old;
            if (_commands.TryGetValue(command, out old) && old.Mod != mod)
            {
                Log.Warn("Chat command /" + command + " of " + mod + " is already used by " + old.Mod + ", ignored.");
                return;
            }
            _commands[command] = new Entry
            {
                Mod = mod, Command = command, Level = (MyPromoteLevel)Math.Max(0, Math.Min(level, (int)MyPromoteLevel.Owner)),
                OnServer = onServer, Help = help ?? "", Handler = handler,
            };
        }

        /// <summary>API 2: removes a command, only by the mod that registered it.</summary>
        public void Unregister(string mod, string command)
        {
            if (string.IsNullOrWhiteSpace(command))
                return;
            command = command.Trim().TrimStart('/');
            Entry e;
            if (_commands.TryGetValue(command, out e) && e.Mod == mod)
                _commands.Remove(command);
        }

        void OnMessageEntered(ulong sender, string text, ref bool sendToOthers)
        {
            if (string.IsNullOrEmpty(text) || text[0] != '/')
                return;
            var parts = text.Substring(1).Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0)
                return;
            string cmd = parts[0];
            var args = parts.Skip(1).ToArray();

            if (cmd.Equals("syntrix", StringComparison.OrdinalIgnoreCase))
            {
                sendToOthers = false;
                ShowHelp();
                return;
            }

            Entry e;
            if (!_commands.TryGetValue(cmd, out e))
                return;
            sendToOthers = false;

            if (MyAPIGateway.Session.PromoteLevel < e.Level)
            {
                Show(e.Mod, "/" + e.Command + ": " + e.Level + " required.");
                return;
            }
            if (!e.OnServer || _core.IsServer)
            {
                Run(e, MyAPIGateway.Multiplayer.MyId, args, null);
                return;
            }
            _core.Net.ToServer(new Packet { Kind = PacketKind.ChatRun, Channel = e.Command, Texts = args });
        }

        public void OnPacket(Packet p, ulong sender)
        {
            if (p.Kind == PacketKind.ChatRun)
            {
                Entry e;
                if (p.Channel == null || !_commands.TryGetValue(p.Channel, out e))
                {
                    Reply(sender, "Syntrix", "Unknown command /" + p.Channel + " on the server.");
                    return;
                }
                if (_core.PromoteLevel(sender) < e.Level)
                {
                    Reply(sender, e.Mod, "/" + e.Command + ": " + e.Level + " required.");
                    return;
                }
                Run(e, sender, p.Texts ?? new string[0], sender);
            }
            else if (p.Kind == PacketKind.ChatReply && p.Texts != null && p.Texts.Length == 2)
            {
                Show(p.Texts[0], p.Texts[1]);
            }
        }

        void Run(Entry e, ulong sender, string[] args, ulong? replyTo)
        {
            string reply;
            try { reply = e.Handler(sender, args); }
            catch (Exception ex)
            {
                Log.Error("chat command /" + e.Command + " of " + e.Mod, ex);
                reply = "/" + e.Command + " failed, see log.";
            }
            if (string.IsNullOrEmpty(reply))
                return;
            if (replyTo.HasValue)
                Reply(replyTo.Value, e.Mod, reply);
            else
                Show(e.Mod, reply);
        }

        void Reply(ulong steamId, string from, string text)
        {
            _core.Net.ToPlayer(new Packet { Kind = PacketKind.ChatReply, Texts = new[] { from, text } }, steamId);
        }

        static void Show(string from, string text)
        {
            foreach (var line in text.Split('\n'))
                MyAPIGateway.Utilities.ShowMessage(from, line.TrimEnd('\r'));
        }

        void ShowHelp()
        {
            var sb = new StringBuilder();
            sb.Append("Syntrix ").Append(Protocol.VersionText).Append(", docked: ");
            sb.Append(string.Join(", ", _core.DockedMods.Select(m => m.Key).OrderBy(n => n)));
            var level = MyAPIGateway.Session.PromoteLevel;
            foreach (var e in _commands.Values.Where(c => c.Level <= level).OrderBy(c => c.Command))
                sb.Append("\n/").Append(e.Command).Append(" - ").Append(e.Help).Append(" (").Append(e.Mod).Append(')');
            Show("Syntrix", sb.ToString());
        }
    }
}
