using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pyratron.Frameworks.LogConsole
{
    /// <summary>
    /// A category for logging messages.
    /// </summary>
    public class LogType
    {
        /// <summary>
        /// A log type with no display name.
        /// </summary>
        public static LogType None { get; } = new LogType(string.Empty);

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

        internal static List<LogType> Types { get; private set; }

        public LogType(string name, ConsoleColor fg = ConsoleColor.White, ConsoleColor bg = ConsoleColor.Black)
        {
            if (Types == null)
                Types = new List<LogType>();
            Name = name;
            FG = fg;
            BG = bg;
        }

        public override string ToString() => Name;
    }
}
