// Syntrix AutoLCD - MIT License, Copyright (c) 2026 Syntrix contributors.
using System;
using System.Collections.Generic;
using Sandbox.ModAPI;
using VRage;

namespace Syntrix.AutoLcd
{
    /// <summary>
    /// All commands by base name. A command word is matched case-insensitively against the longest base name it
    /// starts with; the rest is the variant suffix ("InventoryNNBX" = Inventory + "NNBX"). Commands of other mods
    /// (provider interface, event SyxAutoLcd.RegisterCommand) match the whole word and win over built-in ones.
    /// Provider payload: MyTuple&lt;string, Func&lt;IMyTerminalBlock, string[], List&lt;MyTuple&lt;string, string, float&gt;&gt;&gt;&gt;
    /// = command name and a function that gets the screen block and the arguments and returns lines of label,
    /// value and fill (0 to 1, negative = no bar). The function runs on the server in the game thread.
    /// </summary>
    internal static class CommandRegistry
    {
        /// <summary>Seconds between runs of a command class.</summary>
        public const double Fast = 1;

        public const double Normal = 3;

        public const double Heavy = 5;

        private static readonly List<Entry> _builtIn = new List<Entry>();
        private static readonly Dictionary<string, Entry> _providers = new Dictionary<string, Entry>(StringComparer.OrdinalIgnoreCase);

        /// <summary>One command.</summary>
        public class Entry
        {
            public string Name;

            public double Interval;

            public Func<CommandContext, IEnumerable<int>> Run;

            public Func<IMyTerminalBlock, string[], List<MyTuple<string, string, float>>> Provider;
        }

        /// <summary>Changes whenever commands are added or removed (screens resolve their lines again).</summary>
        public static int Version;

        /// <summary>Number of commands registered by other mods.</summary>
        public static int ProviderCount
        {
            get { return _providers.Count; }
        }

        public static void Add(string name, double interval, Func<CommandContext, IEnumerable<int>> run)
        {
            _builtIn.Add(new Entry { Name = name, Interval = interval, Run = run });
            _builtIn.Sort((a, b) => b.Name.Length.CompareTo(a.Name.Length));
        }

        /// <summary>Registers all built-in commands.</summary>
        public static void RegisterBuiltIn()
        {
            _builtIn.Clear();
            Version++;
            InventoryCommands.Register();
            CargoCommands.Register();
            PowerCommands.Register();
            JumpDriveCommands.Register();
            BlockCommands.Register();
            DetailCommands.Register();
            GasCommands.Register();
            TextCommands.Register();
            TimeCommands.Register();
            ShipCommands.Register();
            ReputationCommands.Register();
        }

        /// <summary>Handles a SyxAutoLcd.RegisterCommand event.</summary>
        public static void OnRegister(object payload)
        {
            try
            {
                if (!(payload is MyTuple<string, Func<IMyTerminalBlock, string[], List<MyTuple<string, string, float>>>>))
                {
                    AutoLcdCore.Log.Warn("provider registration with wrong payload ignored");
                    return;
                }
                var tuple = (MyTuple<string, Func<IMyTerminalBlock, string[], List<MyTuple<string, string, float>>>>)payload;
                AddProvider(tuple.Item1, Normal, tuple.Item2);
            }
            catch (Exception e)
            {
                AutoLcdCore.Log.Error("provider registration", e);
            }
        }

        /// <summary>Handles a SyxAutoLcd.RegisterCommandTimed event (seconds between two runs, 1 to 60).</summary>
        public static void OnRegisterTimed(object payload)
        {
            try
            {
                if (!(payload is MyTuple<string, double, Func<IMyTerminalBlock, string[], List<MyTuple<string, string, float>>>>))
                {
                    AutoLcdCore.Log.Warn("timed provider registration with wrong payload ignored");
                    return;
                }
                var tuple = (MyTuple<string, double, Func<IMyTerminalBlock, string[], List<MyTuple<string, string, float>>>>)payload;
                AddProvider(tuple.Item1, Math.Max(Fast, Math.Min(60, tuple.Item2)), tuple.Item3);
            }
            catch (Exception e)
            {
                AutoLcdCore.Log.Error("provider registration", e);
            }
        }

        private static void AddProvider(string name, double interval, Func<IMyTerminalBlock, string[], List<MyTuple<string, string, float>>> provider)
        {
            if (string.IsNullOrWhiteSpace(name))
                return;
            Version++;
            if (provider == null)
            {
                _providers.Remove(name);
                return;
            }
            _providers[name] = new Entry { Name = name, Interval = interval, Provider = provider };
            AutoLcdCore.Log.Info("provider command " + name + " registered (every " + interval.ToString("0.#") + " s)");
        }

        public static void ClearProviders()
        {
            _providers.Clear();
            Version++;
        }

        /// <summary>Finds the handler of a command word and sets its suffix; false if unknown.</summary>
        public static bool Resolve(CommandLine line)
        {
            Entry entry;
            if (_providers.TryGetValue(line.Word, out entry))
            {
                line.Handler = entry;
                line.Suffix = "";
                return true;
            }
            foreach (var candidate in _builtIn)
            {
                if (line.Word.StartsWith(candidate.Name, StringComparison.OrdinalIgnoreCase))
                {
                    line.Handler = candidate;
                    line.Suffix = line.Word.Substring(candidate.Name.Length);
                    return true;
                }
            }
            return false;
        }

        /// <summary>Runs a provider command and draws its lines.</summary>
        public static IEnumerable<int> RunProvider(CommandContext context)
        {
            List<MyTuple<string, string, float>> lines = null;
            try
            {
                lines = context.Line.Handler.Provider(context.ScreenBlock, context.Line.Args.ToArray());
            }
            catch (Exception e)
            {
                AutoLcdCore.Log.Error("provider command " + context.Line.Word, e);
                context.Screen.AddError(context.Line, "provider error");
            }
            if (lines == null)
                yield break;
            foreach (var line in lines)
            {
                if (line.Item3 >= 0)
                    context.Canvas.PairBar(line.Item1, line.Item3, line.Item2);
                else
                    context.Canvas.Pair(line.Item1, line.Item2);
            }
        }
    }
}
