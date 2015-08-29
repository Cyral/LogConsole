using System;
using System.Collections.Generic;

namespace Pyratron.Frameworks.LogConsole
{
    /// <summary>
    /// Various severity levels for logging.
    /// </summary>
    public sealed class LogLevel
    {
        public static LogLevel Fatal { get; } = new LogLevel("Fatal", ConsoleColor.Black, ConsoleColor.Red);
        public static LogLevel Error { get; } = new LogLevel("Error", ConsoleColor.Red);
        public static LogLevel Warn { get; } = new LogLevel("Warn", ConsoleColor.Yellow);
        public static LogLevel Info { get; } = new LogLevel("Info");
        public static LogLevel Debug { get; } = new LogLevel("Debug", ConsoleColor.Gray);
        public static LogLevel Trace { get; } = new LogLevel("Trace", ConsoleColor.DarkGray);

        /// <summary>
        /// Display name.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Foreground color of the name.
        /// </summary>
        public ConsoleColor FG { get; }

        /// <summary>
        /// Background color of the name.
        /// </summary>
        public ConsoleColor BG { get; }

        internal static List<LogLevel> Levels { get; private set; }

        private LogLevel(string name, ConsoleColor fg = ConsoleColor.White, ConsoleColor bg = ConsoleColor.Black)
        {
            if (Levels == null)
                 Levels = new List<LogLevel>();
            Name = name;
            FG = fg;
            BG = bg;
            Levels.Add(this);
        }

        public override string ToString() => Name;
    }
}