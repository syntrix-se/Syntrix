// Syntrix AutoLCD - MIT License, Copyright (c) 2026 Syntrix contributors.
using System;
using System.Collections.Generic;
using Sandbox.ModAPI;

namespace Syntrix.AutoLcd
{
    /// <summary>
    /// Block type words of BlockCount, Working, Amount and others (reactor, refinery, door ...). Each word stands for
    /// one or more block type ids; a word that is no known type matches the type id itself (also mod types), e.g.
    /// "ConveyorSorter". Words are not case sensitive.
    /// </summary>
    internal static class BlockTypes
    {
        private static readonly Dictionary<string, string[]> _words = new Dictionary<string, string[]>
        {
            { "reactor", new[] { "Reactor" } },
            { "thruster", new[] { "Thrust" } },
            { "container", new[] { "CargoContainer" } },
            { "refinery", new[] { "Refinery" } },
            { "assembler", new[] { "Assembler", "SurvivalKit" } },
            { "solar", new[] { "SolarPanel" } },
            { "battery", new[] { "BatteryBlock" } },
            { "beacon", new[] { "Beacon" } },
            { "antenna", new[] { "RadioAntenna" } },
            { "gyro", new[] { "Gyro" } },
            { "sensor", new[] { "SensorBlock" } },
            { "connector", new[] { "ShipConnector" } },
            { "spotlight", new[] { "ReflectorLight" } },
            { "interlight", new[] { "InteriorLight" } },
            { "landgear", new[] { "LandingGear", "MagneticPlate" } },
            { "program", new[] { "MyProgrammableBlock" } },
            { "timer", new[] { "TimerBlock" } },
            { "rotor", new[] { "MotorStator", "MotorAdvancedStator" } },
            { "piston", new[] { "PistonBase", "ExtendedPistonBase" } },
            { "projector", new[] { "Projector" } },
            { "merge", new[] { "MergeBlock" } },
            { "sound", new[] { "SoundBlock", "Jukebox" } },
            { "collector", new[] { "Collector" } },
            { "door", new[] { "Door", "AirtightHangarDoor", "AirtightSlideDoor", "AdvancedDoor" } },
            { "spheregrav", new[] { "GravityGeneratorSphere" } },
            { "gravgen", new[] { "GravityGenerator" } },
            { "drill", new[] { "Drill" } },
            { "grinder", new[] { "ShipGrinder" } },
            { "welder", new[] { "ShipWelder" } },
            { "gatlingturret", new[] { "LargeGatlingTurret" } },
            { "missileturret", new[] { "LargeMissileTurret" } },
            { "interiorturret", new[] { "InteriorTurret" } },
            { "interturret", new[] { "InteriorTurret" } },
            { "gatlinggun", new[] { "SmallGatlingGun" } },
            { "reloadlauncher", new[] { "SmallMissileLauncherReload" } },
            { "launcher", new[] { "SmallMissileLauncher" } },
            { "mass", new[] { "VirtualMass" } },
            { "warhead", new[] { "Warhead" } },
            { "light", new[] { "InteriorLight", "ReflectorLight", "LightingBlock" } },
            { "control", new[] { "Cockpit", "RemoteControl", "CryoChamber" } },
            { "cockpit", new[] { "Cockpit" } },
            { "medi", new[] { "MedicalRoom" } },
            { "remote", new[] { "RemoteControl" } },
            { "button", new[] { "ButtonPanel" } },
            { "camera", new[] { "CameraBlock" } },
            { "detector", new[] { "OreDetector" } },
            { "laserantenna", new[] { "LaserAntenna" } },
            { "airvent", new[] { "AirVent" } },
            { "oxygenerator", new[] { "OxygenGenerator" } },
            { "sorter", new[] { "ConveyorSorter" } },
            { "oxyfarm", new[] { "OxygenFarm" } },
            { "cryo", new[] { "CryoChamber" } },
            { "jumpdrive", new[] { "JumpDrive" } },
            { "tank", new[] { "OxygenTank" } },
            { "parachute", new[] { "Parachute" } },
            { "windturbine", new[] { "WindTurbine" } },
            { "hydrogenengine", new[] { "HydrogenEngine" } },
            { "broadcastctrl", new[] { "BroadcastController" } },
            { "actionrelay", new[] { "TransponderBlock" } },
            { "lcd", new[] { "TextPanel", "LCDPanelsBlock" } },
            { "survival", new[] { "SurvivalKit" } },
            { "turretctrl", new[] { "TurretControlBlock" } },
            { "searchlight", new[] { "Searchlight" } },
            { "heatvent", new[] { "HeatVent" } },
            { "eventctrl", new[] { "EventControllerBlock" } },
            { "safezone", new[] { "SafeZoneBlock" } },
            { "store", new[] { "StoreBlock" } },
            { "contract", new[] { "ContractBlock" } },
            { "decoy", new[] { "Decoy" } },
            { "farm", new[] { "FarmPlot" } },
        };

        /// <summary>Type id of a block without "MyObjectBuilder_".</summary>
        public static string TypeOf(IMyTerminalBlock block)
        {
            return block.BlockDefinition.TypeIdString.Replace("MyObjectBuilder_", "");
        }

        /// <summary>True if the block is of the type word.</summary>
        public static bool Matches(IMyTerminalBlock block, string word)
        {
            if (string.IsNullOrEmpty(word))
                return true;
            string type = TypeOf(block);
            string[] types;
            if (_words.TryGetValue(word.ToLowerInvariant(), out types))
            {
                foreach (string t in types)
                    if (t == type)
                        return true;
                return false;
            }
            return string.Equals(type, word, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>Splits type arguments ("reactor,solar battery") into words.</summary>
        public static List<string> Words(CommandLine line, int firstArg)
        {
            var words = new List<string>();
            for (int i = firstArg; i < line.Args.Count; i++)
                foreach (string part in line.Args[i].Split(','))
                    if (part.Length > 0)
                        words.Add(part);
            return words;
        }

        /// <summary>True if the block matches one of the words (or the list is empty).</summary>
        public static bool MatchesAny(IMyTerminalBlock block, List<string> words)
        {
            if (words.Count == 0)
                return true;
            foreach (string word in words)
            {
                string type = word;
                int colon = word.IndexOf(':');
                if (colon >= 0)
                    type = word.Substring(0, colon);
                if (type.Length == 0 || Matches(block, type))
                    return true;
            }
            return false;
        }
    }
}
