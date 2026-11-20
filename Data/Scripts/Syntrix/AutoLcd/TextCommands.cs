// Syntrix AutoLCD - MIT License, Copyright (c) 2026 Syntrix contributors.
using System;
using System.Collections.Generic;
using System.Text;
using Sandbox.ModAPI;
using Ingame = Sandbox.ModAPI.Ingame;

namespace Syntrix.AutoLcd
{
    /// <summary>Echo, Center, Right, HScroll, HScrollR, CustomData, TextLCD.</summary>
    internal static class TextCommands
    {
        public static void Register()
        {
            CommandRegistry.Add("Echo", CommandRegistry.Heavy, c => Static(c, 0));
            CommandRegistry.Add("Center", CommandRegistry.Heavy, c => Static(c, 1));
            CommandRegistry.Add("Right", CommandRegistry.Heavy, c => Static(c, 2));
            CommandRegistry.Add("HScroll", CommandRegistry.Fast, HScroll);
            CommandRegistry.Add("CustomData", CommandRegistry.Normal, CustomData);
            CommandRegistry.Add("TextLCD", CommandRegistry.Normal, TextLcd);
        }

        // align 0 left, 1 centre, 2 right.
        private static IEnumerable<int> Static(CommandContext context, int align)
        {
            string text = context.Line.Rest;
            if (align == 1)
                context.Canvas.Center(text);
            else if (align == 2)
                context.Canvas.Right(text);
            else
                context.Canvas.Add(text);
            yield break;
        }

        private static IEnumerable<int> HScroll(CommandContext context)
        {
            string text = context.Line.Rest;
            if (text.Length == 0)
                yield break;
            bool toRight = context.SuffixIs("R");
            int offset = context.Line.State is int ? (int)context.Line.State : 0;
            offset = (offset + (toRight ? text.Length - 1 : 1)) % text.Length;
            context.Line.State = offset;
            var line = new StringBuilder();
            float width = 0;
            float max = context.Canvas.Width;
            int index = offset;
            while (true)
            {
                char c = text[index];
                float w = context.Canvas.Metrics.Width(c);
                if (width + w > max)
                    break;
                line.Append(c);
                width += w;
                index = (index + 1) % text.Length;
                if (line.Length > 2000)
                    break;
            }
            context.Canvas.Add(line.ToString());
        }

        private static IMyTerminalBlock FirstBlock(CommandContext context, Func<IMyTerminalBlock, bool> extra)
        {
            var blocks = context.Blocks(context.Line.Arg(0, ""), extra);
            return blocks.Count > 0 ? blocks[0] : null;
        }

        private static IEnumerable<int> CustomData(CommandContext context)
        {
            if (context.Line.Args.Count == 0)
                yield break;
            var block = FirstBlock(context, null);
            if (block == null)
            {
                context.Canvas.Text(context.T("SyxAutoLcd_BlockNotFound", "Block not found:") + " " + context.Line.Arg(0));
                yield break;
            }
            context.Canvas.Text(block.CustomData);
        }

        private static IEnumerable<int> TextLcd(CommandContext context)
        {
            if (context.Line.Args.Count == 0)
                yield break;
            var block = FirstBlock(context, b => b is Ingame.IMyTextSurface || b is Ingame.IMyTextSurfaceProvider);
            if (block == null)
            {
                context.Canvas.Text(context.T("SyxAutoLcd_BlockNotFound", "Block not found:") + " " + context.Line.Arg(0));
                yield break;
            }
            var surface = block as Ingame.IMyTextSurface;
            if (surface == null)
            {
                var provider = (Ingame.IMyTextSurfaceProvider)block;
                if (provider.SurfaceCount == 0)
                    yield break;
                surface = provider.GetSurface(0);
            }
            context.Canvas.Text(surface.GetText());
        }
    }
}
