// Syntrix AutoLCD - MIT License, Copyright (c) 2026 Syntrix contributors.
using System;
using System.Collections.Generic;
using System.Globalization;
using Sandbox.ModAPI;
using VRageMath;
using Ingame = Sandbox.ModAPI.Ingame;

namespace Syntrix.AutoLcd
{
    /// <summary>
    /// Pos (XYZ, GPS), Altitude (Sea), Speed (Kmh, Mph), Accel, Gravity (Natural, Artificial, Total),
    /// StopDistance, StopTime (Best), Distance. Ship values come from the ship controller of the construct, else from
    /// the grid of the reference block.
    /// </summary>
    internal static class ShipCommands
    {
        private const double RecentSeconds = 10;

        private static readonly Dictionary<long, Braking> _braking = new Dictionary<long, Braking>();

        public static void Register()
        {
            CommandRegistry.Add("Pos", CommandRegistry.Fast, Pos);
            CommandRegistry.Add("Altitude", CommandRegistry.Fast, Altitude);
            CommandRegistry.Add("Speed", CommandRegistry.Fast, Speed);
            CommandRegistry.Add("Accel", CommandRegistry.Fast, Accel);
            CommandRegistry.Add("Gravity", CommandRegistry.Fast, Gravity);
            CommandRegistry.Add("StopDistance", CommandRegistry.Fast, c => Stop(c, true));
            CommandRegistry.Add("StopTime", CommandRegistry.Fast, c => Stop(c, false));
            CommandRegistry.Add("Distance", CommandRegistry.Fast, Distance);
        }

        public static void Clear()
        {
            _braking.Clear();
        }

        private static IMyTerminalBlock Located(CommandContext context)
        {
            if (context.Line.Args.Count == 0)
                return context.ScreenBlock;
            var blocks = context.Blocks(context.Line.Arg(0));
            return blocks.Count > 0 ? blocks[0] : null;
        }

        private static string Coordinate(double value)
        {
            return value.ToString("0.##", LcdText.Culture);
        }

        private static IEnumerable<int> Pos(CommandContext context)
        {
            var block = Located(context);
            if (block == null)
            {
                context.Canvas.Text(context.T("SyxAutoLcd_BlockNotFound", "Block not found:") + " " + context.Line.Arg(0));
                yield break;
            }
            Vector3D pos = block.GetPosition();
            string title = context.T("SyxAutoLcd_Location", "Location:");
            if (context.SuffixIs("XYZ"))
            {
                context.Canvas.Text(title);
                context.Canvas.Pair("X:", Coordinate(pos.X));
                context.Canvas.Pair("Y:", Coordinate(pos.Y));
                context.Canvas.Pair("Z:", Coordinate(pos.Z));
            }
            else if (context.SuffixIs("GPS"))
            {
                var invariant = CultureInfo.InvariantCulture;
                context.Canvas.Text("GPS:" + block.CustomName.Replace(":", "") + ":" + pos.X.ToString("0.##", invariant) + ":"
                    + pos.Y.ToString("0.##", invariant) + ":" + pos.Z.ToString("0.##", invariant) + ":");
            }
            else
                context.Canvas.Pair(title, "X " + Coordinate(pos.X) + "  Y " + Coordinate(pos.Y) + "  Z " + Coordinate(pos.Z));
        }

        private static IEnumerable<int> Altitude(CommandContext context)
        {
            var controller = CargoCommands.Controller(context);
            bool sea = context.SuffixIs("Sea");
            string title = sea ? context.T("SyxAutoLcd_AltitudeSea", "Altitude (sea level):") : context.T("SyxAutoLcd_Altitude", "Altitude:");
            double altitude;
            if (controller != null && controller.TryGetPlanetElevation(sea ? Ingame.MyPlanetElevation.Sealevel : Ingame.MyPlanetElevation.Surface, out altitude))
                context.Canvas.Pair(title, Formatting.Distance(altitude));
            else
                context.Canvas.Pair(title, "-");
            yield break;
        }

        private static Vector3D Velocity(CommandContext context)
        {
            var controller = CargoCommands.Controller(context);
            if (controller != null)
                return controller.GetShipVelocities().LinearVelocity;
            var physics = context.Reference.CubeGrid.Physics;
            return physics != null ? (Vector3D)physics.LinearVelocity : Vector3D.Zero;
        }

        private static IEnumerable<int> Speed(CommandContext context)
        {
            double speed = Velocity(context).Length();
            double factor = 1;
            string unit = "m/s";
            if (context.SuffixIs("Kmh"))
            {
                factor = 3.6;
                unit = "km/h";
            }
            else if (context.SuffixIs("Mph"))
            {
                factor = 2.23694;
                unit = "mph";
            }
            ShowWithMax(context, context.T("SyxAutoLcd_Speed", "Speed:"), speed, (speed * factor).ToString("0.##", LcdText.Culture) + " " + unit);
            yield break;
        }

        private static IEnumerable<int> Accel(CommandContext context)
        {
            var physics = context.Reference.CubeGrid.Physics;
            double accel = physics != null ? physics.LinearAcceleration.Length() : 0;
            ShowWithMax(context, context.T("SyxAutoLcd_Acceleration", "Acceleration:"), accel, accel.ToString("0.##", LcdText.Culture) + " m/s²");
            yield break;
        }

        // First argument: maximum for a bar.
        private static void ShowWithMax(CommandContext context, string title, double value, string text)
        {
            double max;
            if (Formatting.TryNumber(context.Line.Arg(0, ""), out max) && max > 0)
            {
                context.Canvas.Pair(title, text);
                context.Canvas.Bar(value / max);
            }
            else
                context.Canvas.Pair(title, text);
        }

        private static IEnumerable<int> Gravity(CommandContext context)
        {
            Vector3D natural, artificial;
            var controller = CargoCommands.Controller(context);
            if (controller != null)
            {
                natural = controller.GetNaturalGravity();
                artificial = controller.GetArtificialGravity();
            }
            else
            {
                Vector3D pos = context.Reference.GetPosition();
                float interference;
                natural = MyAPIGateway.Physics.CalculateNaturalGravityAt(pos, out interference);
                artificial = MyAPIGateway.Physics.CalculateArtificialGravityAt(pos, interference);
            }
            Vector3D total = natural + artificial;
            bool all = context.Suffix.Length == 0;
            if (all || context.SuffixIs("Natural"))
                context.Canvas.Pair(context.T("SyxAutoLcd_GravityNatural", "Natural gravity:"), Accel(natural.Length()));
            if (all || context.SuffixIs("Artificial"))
                context.Canvas.Pair(context.T("SyxAutoLcd_GravityArtificial", "Artificial gravity:"), Accel(artificial.Length()));
            if (all || context.SuffixIs("Total"))
                context.Canvas.Pair(context.T("SyxAutoLcd_GravityTotal", "Total gravity:"), Accel(total.Length()));
            yield break;
        }

        private static string Accel(double value)
        {
            return value.ToString("0.##", LcdText.Culture) + " m/s²";
        }

        private static IEnumerable<int> Stop(CommandContext context, bool distance)
        {
            bool best = context.SuffixIs("Best");
            Vector3D velocity = Velocity(context);
            double speed = velocity.Length();
            var grid = context.Reference.CubeGrid;
            Braking braking;
            if (!_braking.TryGetValue(grid.EntityId, out braking))
            {
                braking = new Braking();
                _braking[grid.EntityId] = braking;
            }
            double now = context.Now;
            if (grid.Physics != null && speed > 0.01)
            {
                double decel = -Vector3D.Dot(grid.Physics.LinearAcceleration, velocity / speed);
                if (decel > 0)
                {
                    braking.Best = Math.Max(braking.Best, decel);
                    if (decel >= braking.Recent || now - braking.RecentTime > RecentSeconds)
                    {
                        braking.Recent = decel;
                        braking.RecentTime = now;
                    }
                }
            }
            double rate = best ? braking.Best : braking.Recent;
            string title = distance ? context.T("SyxAutoLcd_StopDistance", "Stop distance:") : context.T("SyxAutoLcd_StopTime", "Stop time:");
            string value;
            if (speed < 0.01)
                value = distance ? Formatting.Distance(0) : Formatting.Duration(0);
            else if (rate <= 0.001)
                value = "-";
            else
                value = distance ? Formatting.Distance(speed * speed / (2 * rate)) : Formatting.Duration(speed / rate);
            context.Canvas.Pair(title, value);
            yield break;
        }

        private static IEnumerable<int> Distance(CommandContext context)
        {
            Vector3D target;
            if (!TryGps(context.Line.Arg(1, ""), out target))
            {
                context.Canvas.Text(context.T("SyxAutoLcd_ErrorGps", "Distance needs a block and GPS:Name:X:Y:Z:"));
                yield break;
            }
            var blocks = context.Blocks(context.Line.Arg(0, ""));
            if (blocks.Count == 0)
            {
                context.Canvas.Text(context.T("SyxAutoLcd_BlockNotFound", "Block not found:") + " " + context.Line.Arg(0));
                yield break;
            }
            string name = context.Line.Arg(1, "").Split(':').Length > 1 ? context.Line.Arg(1, "").Split(':')[1] : "";
            context.Canvas.Pair(name.Length > 0 ? name + ":" : context.T("SyxAutoLcd_Distance", "Distance:"),
                Formatting.Distance(Vector3D.Distance(blocks[0].GetPosition(), target)));
        }

        private static bool TryGps(string text, out Vector3D position)
        {
            position = Vector3D.Zero;
            var parts = text.Split(':');
            if (parts.Length < 5 || !string.Equals(parts[0], "GPS", StringComparison.OrdinalIgnoreCase))
                return false;
            double x, y, z;
            var invariant = CultureInfo.InvariantCulture;
            if (!double.TryParse(parts[2], NumberStyles.Float, invariant, out x)
                || !double.TryParse(parts[3], NumberStyles.Float, invariant, out y)
                || !double.TryParse(parts[4], NumberStyles.Float, invariant, out z))
                return false;
            position = new Vector3D(x, y, z);
            return true;
        }

        private class Braking
        {
            public double Recent;

            public double RecentTime;

            public double Best;
        }
    }
}
