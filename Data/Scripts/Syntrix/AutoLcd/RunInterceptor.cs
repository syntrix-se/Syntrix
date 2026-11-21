// Syntrix AutoLCD - MIT License, Copyright (c) 2026 Syntrix contributors.
using System;
using System.Collections.Generic;
using System.Text;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using VRage.Collections;
using VRage.Game.ModAPI;
using VRage.Utils;
using IMyTextSurface = Sandbox.ModAPI.Ingame.IMyTextSurface;
using IMyTextSurfaceProvider = Sandbox.ModAPI.Ingame.IMyTextSurfaceProvider;
using TerminalActionParameter = Sandbox.ModAPI.Ingame.TerminalActionParameter;

namespace Syntrix.AutoLcd
{
    /// <summary>
    /// Buttons, timers and toolbars of the original keep working: while in-game scripts are off, "Run" (with
    /// argument) and "Run with default argument" of an AutoLCD programmable block are passed to AutoLCD (scrollUp,
    /// scrollDown, clear, boot). Own toolbar actions on every block with screens: SyxAutoLcd.ScrollUp, ScrollDown,
    /// Clear, Boot. Players send them to the server; the server checks access. Also the info text of screens in the
    /// terminal (status requested from the server when a block is shown).
    /// </summary>
    internal static class RunInterceptor
    {
        private const double StatusSeconds = 3;

        private static readonly List<IMyTerminalAction> _ownActions = new List<IMyTerminalAction>();
        private static readonly Dictionary<long, string> _status = new Dictionary<long, string>();
        private static readonly Dictionary<long, double> _statusAsked = new Dictionary<long, double>();
        private static readonly HashSet<long> _infoHooked = new HashSet<long>();
        private static readonly List<IMyTerminalBlock> _infoBlocks = new List<IMyTerminalBlock>();
        private static bool _runWrapped;
        private static bool _defaultWrapped;
        private static bool _hooked;
        private static double _nextLookup;

        public static void Load()
        {
            _nextLookup = 0;   // static: survives loading a world, ElapsedPlayTime starts again from the save
            MyAPIGateway.TerminalControls.CustomActionGetter += OnActions;
            if (!MyAPIGateway.Utilities.IsDedicated)
                MyAPIGateway.TerminalControls.CustomControlGetter += OnControls;
            _hooked = true;
        }

        public static void Unload()
        {
            if (_hooked)
            {
                MyAPIGateway.TerminalControls.CustomActionGetter -= OnActions;
                if (!MyAPIGateway.Utilities.IsDedicated)
                    MyAPIGateway.TerminalControls.CustomControlGetter -= OnControls;
            }
            foreach (var block in _infoBlocks)
                block.AppendingCustomInfo -= AppendInfo;
            _hooked = false;
            _ownActions.Clear();
            _status.Clear();
            _statusAsked.Clear();
            _infoHooked.Clear();
            _infoBlocks.Clear();
            _runWrapped = _defaultWrapped = false;
            _nextLookup = 0;
        }

        /// <summary>The game time went back: due times start anew.</summary>
        public static void ResetTimes()
        {
            _nextLookup = 0;
            _statusAsked.Clear();
        }

        /// <summary>Looks for the actions of programmable blocks until they are wrapped (needed on servers).</summary>
        public static void Update()
        {
            if (_runWrapped && _defaultWrapped)
                return;
            double now = AutoLcdCore.Now;
            if (now < _nextLookup)
                return;
            _nextLookup = now + 10;
            List<IMyTerminalAction> actions;
            MyAPIGateway.TerminalControls.GetActions<IMyProgrammableBlock>(out actions);
            if (actions != null)
                Wrap(actions);
        }

        private static bool HasScreens(IMyTerminalBlock block)
        {
            if (block is IMyTextSurface)
                return true;
            var provider = block as IMyTextSurfaceProvider;
            return provider != null && provider.SurfaceCount > 0;
        }

        // ---------------------------------------------------------------- actions

        private static void OnActions(IMyTerminalBlock block, List<IMyTerminalAction> actions)
        {
            try
            {
                if (block is IMyProgrammableBlock)
                    Wrap(actions);
                if (!HasScreens(block))
                    return;
                if (_ownActions.Count == 0)
                    CreateActions();
                actions.AddRange(_ownActions);
            }
            catch (Exception e)
            {
                AutoLcdCore.Log.Error("actions", e);
            }
        }

        private static void CreateActions()
        {
            _ownActions.Add(Action("SyxAutoLcd.ScrollUp", "SyxAutoLcd_ActionScrollUp", "Textures\\GUI\\Icons\\Actions\\Decrease.dds", "scrollup"));
            _ownActions.Add(Action("SyxAutoLcd.ScrollDown", "SyxAutoLcd_ActionScrollDown", "Textures\\GUI\\Icons\\Actions\\Increase.dds", "scrolldown"));
            _ownActions.Add(Action("SyxAutoLcd.Clear", "SyxAutoLcd_ActionClear", "Textures\\GUI\\Icons\\Actions\\Reset.dds", "clear"));
            _ownActions.Add(Action("SyxAutoLcd.Boot", "SyxAutoLcd_ActionBoot", "Textures\\GUI\\Icons\\Actions\\Start.dds", "boot"));
        }

        private static IMyTerminalAction Action(string id, string nameKey, string icon, string command)
        {
            var action = MyAPIGateway.TerminalControls.CreateAction<IMyTerminalBlock>(id);
            action.Name = new StringBuilder(SyntrixTextOr(nameKey, command));
            action.Icon = icon;
            action.Enabled = HasScreens;
            action.ValidForGroups = true;
            action.Action = b => Trigger(b, command);
            return action;
        }

        private static string SyntrixTextOr(string key, string fallback)
        {
            var id = MyStringId.GetOrCompute(key);
            return VRage.MyTexts.Exists(id) ? VRage.MyTexts.GetString(id) : "AutoLCD " + fallback;
        }

        private static void Trigger(IMyTerminalBlock block, string command)
        {
            if (MyAPIGateway.Session.IsServer)
                HandleAction(block, command, 0);
            else
                Send("action", block.EntityId.ToString(), command);
        }

        private static void Wrap(List<IMyTerminalAction> actions)
        {
            foreach (var action in actions)
            {
                if (!_runWrapped && action.Id == "Run")
                {
                    _runWrapped = true;
                    var original = action.ActionWithParameters;
                    action.ActionWithParameters = (block, parameters) =>
                    {
                        Intercept(block, Argument(parameters));
                        original(block, parameters);
                    };
                }
                else if (!_defaultWrapped && action.Id == "RunWithDefaultArgument")
                {
                    _defaultWrapped = true;
                    var original = action.Action;
                    action.Action = block =>
                    {
                        var program = block as IMyProgrammableBlock;
                        Intercept(block, program != null ? program.TerminalRunArgument : "");
                        original(block);
                    };
                }
            }
        }

        private static string Argument(ListReader<TerminalActionParameter> parameters)
        {
            foreach (var parameter in parameters)
                if (!parameter.IsEmpty && parameter.TypeCode == TypeCode.String)
                    return parameter.Value as string ?? "";
            return "";
        }

        private static void Intercept(IMyTerminalBlock block, string argument)
        {
            try
            {
                if (AutoLcdCore.ScriptsEnabled || string.IsNullOrWhiteSpace(argument))
                    return;
                if (MyAPIGateway.Session.IsServer)
                    HandleRun(block, argument, 0);
                else
                    Send("run", block.EntityId.ToString(), argument);
            }
            catch (Exception e)
            {
                AutoLcdCore.Log.Error("run argument", e);
            }
        }

        // ---------------------------------------------------------------- network

        private static void Send(params string[] parts)
        {
            AutoLcdCore.Syx.NetSendToServer(AutoLcdCore.ChannelToServer, Encoding.UTF8.GetBytes(string.Join(AutoLcdCore.Separator.ToString(), parts)));
        }

        public static void OnServerMessage(ulong sender, byte[] data)
        {
            try
            {
                var parts = Encoding.UTF8.GetString(data).Split(AutoLcdCore.Separator);
                long id;
                if (parts.Length < 2 || !long.TryParse(parts[1], out id))
                    return;
                var block = MyAPIGateway.Entities.GetEntityById(id) as IMyTerminalBlock;
                if (block == null)
                    return;
                switch (parts[0])
                {
                    case "run":
                        if (parts.Length >= 3 && !AutoLcdCore.ScriptsEnabled)
                            HandleRun(block, parts[2], sender);
                        break;
                    case "action":
                        if (parts.Length >= 3)
                            HandleAction(block, parts[2], sender);
                        break;
                    case "status":
                        if (MayOperate(block, sender))
                        {
                            string text = StatusOf(block);
                            AutoLcdCore.Syx.NetSendToPlayer(AutoLcdCore.ChannelToPlayer, sender,
                                Encoding.UTF8.GetBytes("status" + AutoLcdCore.Separator + id + AutoLcdCore.Separator + text));
                        }
                        break;
                }
            }
            catch (Exception e)
            {
                AutoLcdCore.Log.Error("server message", e);
            }
        }

        public static void OnPlayerMessage(ulong sender, byte[] data)
        {
            try
            {
                var parts = Encoding.UTF8.GetString(data).Split(AutoLcdCore.Separator);
                long id;
                if (parts.Length < 3 || parts[0] != "status" || !long.TryParse(parts[1], out id))
                    return;
                _status[id] = parts[2];
                var block = MyAPIGateway.Entities.GetEntityById(id) as IMyTerminalBlock;
                if (block != null)
                    block.RefreshCustomInfo();
            }
            catch (Exception e)
            {
                AutoLcdCore.Log.Error("player message", e);
            }
        }

        /// <summary>True if the player may use the block (0 = the server itself).</summary>
        public static bool MayOperate(IMyTerminalBlock block, ulong steamId)
        {
            if (steamId == 0)
                return true;
            var players = new List<IMyPlayer>();
            MyAPIGateway.Players.GetPlayers(players, p => p.SteamUserId == steamId);
            return players.Count > 0 && block.HasPlayerAccess(players[0].IdentityId);
        }

        // ---------------------------------------------------------------- server handling

        /// <summary>Run argument of an AutoLCD programmable block: scrollUp/scrollDown {screen} [n], clear, boot.</summary>
        public static void HandleRun(IMyTerminalBlock block, string argument, ulong sender)
        {
            var context = ScreenRegistry.ContextOf(block.EntityId);
            if (context == null || !MayOperate(block, sender))
                return;
            var args = CommandLine.Split(argument.Trim());
            if (args.Count == 0)
                return;
            string word = args[0].ToLowerInvariant();
            if (word == "clear")
            {
                foreach (var screen in ScreenRegistry.ScreensOfContext(context))
                    screen.Clear();
                return;
            }
            if (word == "boot")
            {
                foreach (var screen in ScreenRegistry.ScreensOfContext(context))
                    screen.Boot();
                return;
            }
            if ((word == "scrollup" || word == "scrolldown") && args.Count >= 2)
            {
                int lines = 1;
                if (args.Count >= 3)
                    int.TryParse(args[2], out lines);
                lines = Math.Max(1, lines);
                foreach (var screen in ScreenRegistry.FindScreens(args[1], block, block.OwnerId))
                    screen.Scroll(word == "scrollup" ? -lines : lines);
            }
        }

        /// <summary>Own action on a block: its screens; on an AutoLCD programmable block also the screens it covers.</summary>
        public static void HandleAction(IMyTerminalBlock block, string command, ulong sender)
        {
            if (!MayOperate(block, sender))
                return;
            var screens = new List<ManagedScreen>(ScreenRegistry.ScreensOf(block.EntityId));
            var context = ScreenRegistry.ContextOf(block.EntityId);
            if (context != null && (command == "clear" || command == "boot"))
                screens.AddRange(ScreenRegistry.ScreensOfContext(context));
            int lines = Math.Max(1, AutoLcdCore.Settings.ScrollLines);
            foreach (var screen in screens)
            {
                switch (command)
                {
                    case "scrollup":
                        screen.Scroll(-lines);
                        break;
                    case "scrolldown":
                        screen.Scroll(lines);
                        break;
                    case "clear":
                        screen.Clear();
                        break;
                    case "boot":
                        screen.Boot();
                        break;
                }
            }
        }

        /// <summary>Info text of a block: its screens, or the settings of an AutoLCD programmable block.</summary>
        public static string StatusOf(IMyTerminalBlock block)
        {
            var text = new StringBuilder();
            var context = ScreenRegistry.ContextOf(block.EntityId);
            if (context != null)
            {
                text.Append(LcdText.Get("SyxAutoLcd_InfoContext", "AutoLCD settings read from this block")).Append('\n');
                text.Append("LCD_TAG = ").Append(context.Tag).Append('\n');
                text.Append(LcdText.Get("SyxAutoLcd_InfoScreens", "Screens")).Append(": ").Append(ScreenRegistry.ScreensOfContext(context).Count).Append('\n');
                if (context.RunsItself)
                    text.Append(LcdText.Get("SyxAutoLcd_InfoRunsItself", "The script runs in this block; AutoLCD leaves its screens alone.")).Append('\n');
            }
            foreach (var screen in ScreenRegistry.ScreensOf(block.EntityId))
                text.Append(screen.Status());
            return text.ToString();
        }

        // ---------------------------------------------------------------- info text (player)

        private static void OnControls(IMyTerminalBlock block, List<IMyTerminalControl> controls)
        {
            try
            {
                if (!HasScreens(block) && !(block is IMyProgrammableBlock))
                    return;
                if (_infoHooked.Add(block.EntityId))
                {
                    block.AppendingCustomInfo += AppendInfo;
                    _infoBlocks.Add(block);
                }
                double now = AutoLcdCore.Now;
                double asked;
                if (_statusAsked.TryGetValue(block.EntityId, out asked) && now - asked < StatusSeconds)
                    return;
                _statusAsked[block.EntityId] = now;
                if (MyAPIGateway.Session.IsServer)
                {
                    _status[block.EntityId] = StatusOf(block);
                    block.RefreshCustomInfo();
                }
                else
                    Send("status", block.EntityId.ToString());
            }
            catch (Exception e)
            {
                AutoLcdCore.Log.Error("info text", e);
            }
        }

        private static void AppendInfo(IMyTerminalBlock block, StringBuilder text)
        {
            string status;
            if (_status.TryGetValue(block.EntityId, out status) && status.Length > 0)
                text.Append('\n').Append(status);
        }
    }
}
