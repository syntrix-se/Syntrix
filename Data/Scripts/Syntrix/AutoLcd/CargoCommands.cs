// Syntrix AutoLCD - MIT License, Copyright (c) 2026 Syntrix contributors.
using System;
using System.Collections.Generic;
using Sandbox.Definitions;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.ModAPI.Ingame;
using IMyCargoContainer = Sandbox.ModAPI.Ingame.IMyCargoContainer;
using IMyGasGenerator = Sandbox.ModAPI.Ingame.IMyGasGenerator;
using IMyReactor = Sandbox.ModAPI.Ingame.IMyReactor;
using IMyShipController = Sandbox.ModAPI.Ingame.IMyShipController;
using IMyUserControllableGun = Sandbox.ModAPI.Ingame.IMyUserControllableGun;

namespace Syntrix.AutoLcd
{
    /// <summary>Cargo, CargoAll, Mass, ShipMass, ShipMassBase, Amount.</summary>
    internal static class CargoCommands
    {
        private const int Chunk = 32;

        public static void Register()
        {
            CommandRegistry.Add("Cargo", CommandRegistry.Heavy, c => Cargo(c, false));
            CommandRegistry.Add("CargoAll", CommandRegistry.Heavy, c => Cargo(c, true));
            CommandRegistry.Add("Mass", CommandRegistry.Heavy, Mass);
            CommandRegistry.Add("ShipMass", CommandRegistry.Normal, ShipMass);
            CommandRegistry.Add("Amount", CommandRegistry.Heavy, Amount);
        }

        /// <summary>Ship controller on the construct of the reference block, null if none.</summary>
        public static IMyShipController Controller(CommandContext context)
        {
            IMyShipController any = null;
            foreach (var block in context.Source.Blocks)
            {
                var controller = block as IMyShipController;
                if (controller == null || block.Closed || !block.IsSameConstructAs(context.Reference))
                    continue;
                if (controller.IsMainCockpit)
                    return controller;
                if (any == null)
                    any = controller;
            }
            return any;
        }

        private static IEnumerable<int> Cargo(CommandContext context, bool all)
        {
            var blocks = context.Blocks(context.Line.Arg(0, "*"), b => all ? b.HasInventory : b is IMyCargoContainer);
            double used = 0, max = 0;
            int count = 0;
            foreach (var block in blocks)
            {
                for (int i = 0; i < block.InventoryCount; i++)
                {
                    var inventory = block.GetInventory(i);
                    if (inventory == null)
                        continue;
                    used += (double)inventory.CurrentVolume;
                    max += (double)inventory.MaxVolume;
                }
                if (++count % Chunk == 0)
                    yield return 0;
            }
            string title = context.Line.Arg(1, context.T("SyxAutoLcd_CargoUsed", "Cargo used:"));
            context.Canvas.Stat(title, Formatting.Volume(used) + " / " + Formatting.Volume(max), max > 0 ? used / max : 0, context.Mode(context.Suffix));
        }

        private static IEnumerable<int> Mass(CommandContext context)
        {
            var blocks = context.Blocks(context.Line.Arg(0, "*"), b => b.HasInventory);
            double mass = 0;
            int count = 0;
            foreach (var block in blocks)
            {
                for (int i = 0; i < block.InventoryCount; i++)
                {
                    var inventory = block.GetInventory(i);
                    if (inventory != null)
                        mass += (double)inventory.CurrentMass;
                }
                if (++count % Chunk == 0)
                    yield return 0;
            }
            ShowMass(context, context.T("SyxAutoLcd_CargoMass", "Cargo mass:"), mass, 1);
        }

        private static IEnumerable<int> ShipMass(CommandContext context)
        {
            bool baseMass = context.SuffixIs("Base");
            double mass = 0;
            var controller = Controller(context);
            if (controller != null)
            {
                var masses = controller.CalculateShipMass();
                mass = baseMass ? masses.BaseMass : masses.TotalMass;
            }
            else if (context.Reference.CubeGrid.Physics != null)
                mass = context.Reference.CubeGrid.Physics.Mass;
            string title = baseMass ? context.T("SyxAutoLcd_ShipMassBase", "Ship base mass:") : context.T("SyxAutoLcd_ShipMass", "Ship mass:");
            ShowMass(context, title, mass, 0);
            yield break;
        }

        // Max mass in kg from the arguments at index first (value) and first + 1 (prefix: k = kg, M = t ...).
        private static void ShowMass(CommandContext context, string title, double mass, int first)
        {
            double max;
            string suffix = context.Suffix == "BASE" ? "" : context.Suffix;
            if (!Formatting.TryNumber(context.Line.Arg(first, ""), out max) || max <= 0)
            {
                context.Canvas.Pair(title, Formatting.Mass(mass));
                return;
            }
            string prefix = context.Line.Arg(first + 1, "k");
            max *= Formatting.PrefixFactor(prefix) / 1000;
            context.Canvas.Stat(title, Formatting.Mass(mass) + " / " + Formatting.Mass(max), mass / max, context.Mode(suffix));
        }

        private static IEnumerable<int> Amount(CommandContext context)
        {
            var words = BlockTypes.Words(context.Line, 1);
            var blocks = context.Blocks(context.Line.Arg(0, "*"), b => b.HasInventory && (words.Count > 0
                ? BlockTypes.MatchesAny(b, words)
                : b is IMyUserControllableGun || b is IMyReactor || b is IMyGasGenerator));
            var items = new List<MyInventoryItem>();
            int count = 0;
            foreach (var block in blocks)
            {
                var inventory = block.GetInventory(0);
                if (inventory == null)
                    continue;
                items.Clear();
                inventory.GetItems(items);
                double amount = 0;
                MyItemType? type = null;
                foreach (var item in items)
                {
                    if (type == null)
                        type = item.Type;
                    if (item.Type == type.Value)
                        amount += (double)item.Amount;
                }
                ItemCatalog.ItemInfo info = type.HasValue ? ItemCatalog.Find(type.Value.TypeId, type.Value.SubtypeId) : ExpectedItem(block);
                if (info == null || info.Volume <= 0)
                {
                    context.Canvas.Pair(block.CustomName, context.T("SyxAutoLcd_Empty", "EMPTY"));
                    continue;
                }
                double max = Math.Floor((double)inventory.MaxVolume / info.Volume + 1e-6);
                double fraction = max > 0 ? amount / max : 0;
                string value = Formatting.Amount(amount) + " / " + Formatting.Amount(max);
                if (context.SuffixIs("Bar"))
                    context.Canvas.Bar(fraction);
                else if (context.SuffixIs("X"))
                    context.Canvas.Pair(block.CustomName, value);
                else
                    context.Canvas.PairBar(block.CustomName, fraction, value);
                if (++count % Chunk == 0)
                    yield return 0;
            }
        }

        // The item a weapon or reactor takes when it is empty (from its definitions).
        private static ItemCatalog.ItemInfo ExpectedItem(IMyTerminalBlock block)
        {
            try
            {
                var definition = MyDefinitionManager.Static.GetCubeBlockDefinition(block.BlockDefinition);
                var weapon = definition as MyWeaponBlockDefinition;
                if (weapon != null)
                {
                    var weaponDefinition = MyDefinitionManager.Static.GetWeaponDefinition(weapon.WeaponDefinitionId);
                    if (weaponDefinition != null && weaponDefinition.AmmoMagazinesId.Length > 0)
                    {
                        var id = weaponDefinition.AmmoMagazinesId[0];
                        return ItemCatalog.Find(id.TypeId.ToString(), id.SubtypeName);
                    }
                }
                var reactor = definition as MyReactorDefinition;
                if (reactor != null && reactor.FuelInfos != null && reactor.FuelInfos.Length > 0)
                {
                    var id = reactor.FuelInfos[0].FuelId;
                    return ItemCatalog.Find(id.TypeId.ToString(), id.SubtypeName);
                }
            }
            catch (Exception)
            {
                // Unknown definition layout of a mod block: shown as empty.
            }
            return null;
        }
    }
}
