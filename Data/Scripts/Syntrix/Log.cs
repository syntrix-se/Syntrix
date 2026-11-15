// Syntrix - shared services framework for Space Engineers mods.
// MIT License, see LICENSE in the repository root.
using System;
using VRage.Utils;

namespace Syntrix
{
    /// <summary>Lines of Syntrix in the game log.</summary>
    public static class Log
    {
        /// <summary>Writes an information line.</summary>
        /// <param name="text">The text.</param>
        public static void Info(string text) { MyLog.Default.WriteLineAndConsole("Syntrix: " + text); }

        /// <summary>Writes a warning line.</summary>
        /// <param name="text">The text.</param>
        public static void Warn(string text) { MyLog.Default.WriteLineAndConsole("Syntrix: WARNING " + text); }

        /// <summary>Writes an error with the exception.</summary>
        /// <param name="where">Where it happened.</param>
        /// <param name="e">The exception.</param>
        public static void Error(string where, Exception e) { MyLog.Default.WriteLineAndConsole("Syntrix: ERROR in " + where + ": " + e); }
    }
}
