using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using Timer = System.Timers.Timer;

namespace Pyratron.Frameworks.LogConsole
{
    /// <summary>
    /// Provides console logging support.
    /// </summary>
    public static class Logger
    {
        private static readonly int levelColumnLength;

        private static string input;
        private static int historyIndex;
        private static readonly List<string> inputHistory;
        private static readonly Queue<string> logQueue;
        private static readonly Timer flushTimer;
        private static DateTime lastFlush;

        private static int lastHeight = Console.BufferHeight;
        private static TimeSpan queueTime = TimeSpan.FromMinutes(2);
        private static TimeSpan minFlushTime = TimeSpan.FromSeconds(30);
        private static readonly object locker = new object();

        /// <summary>
        /// Maximum number of logged items before they are written to the file. Default is 50 items.
        /// </summary>
        public static int QueueSize { get; set; } = 50;

        /// <summary>
        /// Maximum amount of time before logged items are written to the file. Default is 2 minutes.
        /// </summary>
        public static TimeSpan QueueTime
        {
            get { return queueTime; }
            set
            {
                if (value < MinFlushTime)
                    throw new InvalidOperationException("Queue flush time must be above the minimum flush time.");
                queueTime = value;
                flushTimer.Interval = queueTime.TotalMilliseconds;
            }
        }

        /// <summary>
        /// The minimum amount of time that must pass between message queue flushes. This prevents a ton of messages from writing
        /// to the log file too quickly.
        /// Default is 30 seconds.
        /// </summary>
        public static TimeSpan MinFlushTime
        {
            get { return minFlushTime; }
            set
            {
                minFlushTime = value;
                if (value > QueueTime)
                    throw new InvalidOperationException("Queue flush time must be above the minimum flush time.");
            }
        }

        /// <summary>
        /// Defines the format messages should be logged in.
        /// </summary>
        /// <example>
        /// The following variables can be used:
        /// %timestamp
        /// %level
        /// %type
        /// %messages
        /// The following colors can be used:
        /// @Hex for console colors. (Ex: 0F or 7A)
        /// @XL for level color.
        /// @XT for type color.
        /// @XR to reset color.
        /// Color list: https://www.pyratron.com/assets/consolehex.png
        /// </example>
        public static string LogFormat { get; set; } = "@07[%timestamp]@XR @XL%level@07 | @XT%type: @XR%message";

        /// <summary>
        /// Directory where log files will be created daily.
        /// </summary>
        public static string LogDirectory { get; set; }

        /// <summary>
        /// Format for log file names.
        /// </summary>
        public static string LogFileFormat { get; set; } = "MM-dd-yyyy";

        /// <summary>
        /// Character to be displayed at the start of the command prompt.
        /// </summary>
        public static char Cursor { get; set; } = '>';

        /// <summary>
        /// Maximum lines of history to be stored that can be scrolled through with the arrow keys.
        /// </summary>
        public static int HistoryCount { get; set; } = 50;

        /// <summary>
        /// Color of the console cursor.
        /// </summary>
        public static ConsoleColor CursorColor { get; set; } = ConsoleColor.Green;

        /// <summary>
        /// Defines the DateTime format messages are logged with.
        /// </summary>
        public static string TimestampFormat { get; set; } = "HH:mm:ss";

        static Logger()
        {
            // Find max length of level type so the column widths can be even.
            foreach (var level in LogLevel.Levels)
            {
                levelColumnLength = Math.Max(levelColumnLength, level.ToString().Length);
            }

            LogDirectory = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "logs");

            logQueue = new Queue<string>();
            flushTimer = new Timer();
            flushTimer.Elapsed += (sender, args) => FlushLog();
            flushTimer.Start();
            inputHistory = new List<string>(HistoryCount);
            input = string.Empty;

            MessageLogged += (message, level, type, time, fullmessage) => { WriteCommandCursor(); };
        }

        /// <summary>
        /// Gets the current time using the <c>TimestampFormat</c>
        /// </summary>
        public static string GetTimestamp()
            => DateTime.Now.ToString(TimestampFormat);

        public static void Fatal(string message, params object[] args)
            => Log(LogLevel.Fatal, LogType.None, message, args);

        public static void Error(string message, params object[] args)
            => Log(LogLevel.Error, LogType.None, message, args);

        public static void Warn(string message, params object[] args)
            => Log(LogLevel.Warn, LogType.None, message, args);

        public static void Info(string message, params object[] args)
            => Log(LogLevel.Info, LogType.None, message, args);

        public static void Debug(string message, params object[] args)
            => Log(LogLevel.Debug, LogType.None, message, args);

        public static void Trace(string message, params object[] args)
            => Log(LogLevel.Trace, LogType.None, message, args);

        public static void Fatal(LogType type, string message, params object[] args)
            => Log(LogLevel.Fatal, type, message, args);

        public static void Error(LogType type, string message, params object[] args)
            => Log(LogLevel.Error, type, message, args);

        public static void Warn(LogType type, string message, params object[] args)
            => Log(LogLevel.Warn, type, message, args);

        public static void Info(LogType type, string message, params object[] args)
            => Log(LogLevel.Info, type, message, args);

        public static void Debug(LogType type, string message, params object[] args)
            => Log(LogLevel.Debug, type, message, args);

        public static void Trace(LogType type, string message, params object[] args)
            => Log(LogLevel.Trace, type, message, args);

        /// <summary>
        /// Log an informational message. (Same as <c>Info</c>).
        /// See https://www.pyratron.com/assets/consolehex.png for hex codes that can be used in the message.
        /// </summary>
        public static void Log(string message, params object[] args)
            => Info(message, args);

        /// <summary>
        /// Log a message with the specified level and type.
        /// See https://www.pyratron.com/assets/consolehex.png for hex codes that can be used in the message.
        /// </summary>
        public static void Log(LogLevel level, LogType type, string message, params object[] args)
            => Log(level, type, message, ConsoleColor.Gray, ConsoleColor.Black, args);

        /// <summary>
        /// Log a message with the specified level, type, and color.
        /// See https://www.pyratron.com/assets/consolehex.png for hex codes that can be used in the message.
        /// </summary>
        public static void Log(LogLevel level, LogType type, ConsoleColor color, string message, params object[] args)
            => Log(level, type, message, color, ConsoleColor.Black, args);


        /// <summary>
        /// Log a message with the specified level.
        /// See https://www.pyratron.com/assets/consolehex.png for hex codes that can be used in the message.
        /// </summary>
        public static void Log(LogLevel level, string message, params object[] args)
            => Log(level, LogType.None, message, ConsoleColor.Gray, ConsoleColor.Black, args);

        /// <summary>
        /// Log a message with the specified type.
        /// See https://www.pyratron.com/assets/consolehex.png for hex codes that can be used in the message.
        /// </summary>
        public static void Log(LogType type, string message, params object[] args)
            => Info(type, message, args);

        /// <summary>
        /// Log a message with the specified type and color.
        /// See https://www.pyratron.com/assets/consolehex.png for hex codes that can be used in the message.
        /// </summary>
        public static void Log(LogType type, ConsoleColor fg, string message, params object[] args)
            => Log(LogLevel.Info, type, message, fg, ConsoleColor.Black, args);

        /// <summary>
        /// Log a message with the specified level and color.
        /// See https://www.pyratron.com/assets/consolehex.png for hex codes that can be used in the message.
        /// </summary>
        public static void Log(LogLevel level, ConsoleColor fg, string message, params object[] args)
            => Log(level, LogType.None, message, fg, ConsoleColor.Black, args);

        /// <summary>
        /// Log a message with the specified foreground color.
        /// See https://www.pyratron.com/assets/consolehex.png for hex codes that can be used in the message.
        /// </summary>
        public static void Log(ConsoleColor fg, string message, params object[] args)
            => Log(LogLevel.Info, LogType.None, message, fg, ConsoleColor.Black, args);

        /// <summary>
        /// When any message is logged.
        /// </summary>
        public static event LogEventHandler MessageLogged;

        /// <summary>
        /// When the user inputs a console command.
        /// </summary>
        public static event InputEventHandler InputEntered;

        /// <summary>
        /// Enter a constant loop that checks for input and emulates console behavior.
        /// </summary>
        public static void Wait()
        {
            while (true)
            {
                try
                {
                    input = string.Empty;
                    WriteCommandCursor();

                    // Read input and parse commands.
                    while (true)
                    {
                        var key = Console.ReadKey(true);
                        FixBuffer();
                        if (key.Key == ConsoleKey.Backspace)
                        {
                            if (input.Length - 1 >= 0)
                            {
                                input = input.Substring(0, input.Length - 1);
                                Console.CursorLeft = 2 + input.Length;
                                Console.Write(" \b");
                            }
                            continue;
                        }
                        if (key.Key == ConsoleKey.Enter)
                        {
                            // Enter press will parse the command.
                            Console.WriteLine();
                            break;
                        }
                        // Arrow keys will scroll through command history.
                        switch (key.Key)
                        {
                            case ConsoleKey.UpArrow:
                                if (historyIndex > 0 && inputHistory.Count > 0)
                                {
                                    historyIndex--;
                                    Console.CursorLeft = 2;
                                    if (historyIndex >= inputHistory.Count - 1)
                                        Console.Write(inputHistory[inputHistory.Count - 1] +
                                                      new string(' ', input.Length));
                                    else if (historyIndex < inputHistory.Count - 1 &&
                                             inputHistory[historyIndex + 1].Length + 1 -
                                             inputHistory[historyIndex].Length <=
                                             0)
                                        Console.Write(inputHistory[historyIndex]);
                                    else
                                        Console.Write(inputHistory[historyIndex] +
                                                      new string(' ', Math.Max(0, Math.Max(input.Length,
                                                          inputHistory[historyIndex + 1].Length + 1 -
                                                          inputHistory[historyIndex].Length))));
                                    Console.CursorLeft =
                                        inputHistory[Math.Min(historyIndex, inputHistory.Count - 1)].Length + 2;
                                    input = inputHistory[Math.Min(historyIndex, inputHistory.Count - 1)];
                                }
                                continue;
                            case ConsoleKey.DownArrow:
                                if (historyIndex < inputHistory.Count && inputHistory.Count > 0)
                                {
                                    historyIndex++;
                                    Console.CursorLeft = 2;
                                    if (historyIndex == inputHistory.Count)
                                    {
                                        Console.Write(new string(' ',
                                            Math.Max(0, Math.Max(input.Length, inputHistory[historyIndex - 1].Length))));
                                        input = string.Empty;
                                        Console.CursorLeft = 2;
                                        continue;
                                    }
                                    if (inputHistory[historyIndex - 1].Length + 1 - inputHistory[historyIndex].Length <=
                                        0)
                                        Console.Write(inputHistory[historyIndex]);
                                    else
                                        Console.Write(inputHistory[historyIndex] +
                                                      new string(' ',
                                                          Math.Max(0, Math.Max(input.Length,
                                                              inputHistory[historyIndex - 1].Length + 1 -
                                                              inputHistory[historyIndex].Length))));
                                    Console.CursorLeft = inputHistory[historyIndex].Length + 2;
                                    input = inputHistory[historyIndex];
                                }
                                continue;
                            case ConsoleKey.LeftArrow:
                                if (Console.CursorLeft > 2)
                                    Console.CursorLeft--;
                                continue;
                            case ConsoleKey.RightArrow:
                                if (Console.CursorLeft < Math.Min(2 + input.Length, Console.BufferWidth - 1))
                                    Console.CursorLeft++;
                                continue;
                        }
                        input += key.KeyChar;
                        Console.Write(key.KeyChar);
                    }

                    if (!string.IsNullOrWhiteSpace(input))
                    {
                        input = input.Trim();
                        inputHistory.Add(input);
                        OnMessageInput(input);

                        // Set history index and remove old entries.
                        historyIndex = inputHistory.Count;
                        if (inputHistory.Count > HistoryCount)
                            inputHistory.RemoveAt(0);
                    }
                }
                catch (Exception e)
                {
                    Console.CursorTop = 0;
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Error in input loop: " + e);
                    Console.ForegroundColor = ConsoleColor.Gray;
                }
            }
        }

        /// <summary>
        /// Writes the command cursor.
        /// </summary>
        public static void WriteCommandCursor()
        {
            Console.ForegroundColor = CursorColor;
            Console.Write(Cursor + " ");
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write(input);
            Console.CursorLeft = 2 + input.Length;
        }

        /// <summary>
        /// Adds a message to the log queue. It is written to the file once it reaches a certain size or a certain amount of time
        /// elapses.
        /// Each day a new log file is created.
        /// </summary>
        public static void LogToFile(string message)
        {
            if (string.IsNullOrWhiteSpace(LogDirectory))
                throw new InvalidOperationException("LogDirectory must be set.");
            try
            {
                lock (logQueue)
                {
                    logQueue.Enqueue(message);

                    FlushLog();
                }
            }
            catch (Exception e)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Log error while writing to file: " + e);
                Console.ForegroundColor = ConsoleColor.Gray;
            }
        }

        /// <summary>
        /// Writes a line break/new line to the console.
        /// </summary>
        public static void WriteBreak()
        {
            Console.CursorLeft = 0;
            Console.WriteLine(new string(' ', Math.Min(Console.WindowWidth, Console.BufferWidth) - 1));
        }

        /// <summary>
        /// Helper method to write centered text.
        /// </summary>
        public static void WriteCenteredText(string message)
        {
            if (Console.WindowWidth - message.Length > 0)
            {
                Console.Write(new string(' ', (Console.WindowWidth - message.Length) / 2));
                Console.WriteLine(message);
            }
            else
                Console.WriteLine(message);
        }

        /// <summary>
        /// Writes log items to the log file after a certain number of items have been reached, time has elapsed, or the day has
        /// changed.
        /// </summary>
        public static void FlushLog()
        {
            // If the max number of items has been reached, enough time elapsed, or the day has changed, AND the minimum time between writes has passed, write the items to the log file.
            if ((logQueue.Count >= QueueSize || DateTime.Now - lastFlush > QueueTime ||
                lastFlush.Date != DateTime.Now.Date) && DateTime.Now - lastFlush > MinFlushTime)
            {
                if (string.IsNullOrWhiteSpace(LogDirectory))
                    throw new InvalidOperationException("LogDirectory must be set.");
                ThreadPool.QueueUserWorkItem(q =>
                {
                    try
                    {
                        lock (logQueue)
                        {
                            lastFlush = DateTime.Now;
                            var path = Path.Combine(LogDirectory, lastFlush.ToString(LogFileFormat) + ".txt");
                            using (var logWriter = new StreamWriter(new FileStream(path,
                                FileMode.Append, FileAccess.Write, FileShare.None, 1024)))
                            {
                                while (logQueue.Count > 0)
                                {
                                    logWriter.WriteLine(logQueue.Dequeue());
                                }
                                logWriter.Close();
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("Error writing log: " + e);
                        Console.ForegroundColor = ConsoleColor.Gray;
                    }
                });
            }
        }

        private static void Log(LogLevel level, LogType type, string message, ConsoleColor fg = ConsoleColor.Gray,
            ConsoleColor bg = ConsoleColor.Black, params object[] args)
        {
            try
            {
                // Prevent messages from appearing messed up due to Write calls being out of order.
                lock (locker)
                {
                    // Replace variables.
                    var str = LogFormat.Replace("%timestamp", GetTimestamp())
                        .Replace("%level", new string(' ', levelColumnLength - level.ToString().Length) + level);
                    if (type == LogType.None)
                        str = str.Remove(str.IndexOf("%type", StringComparison.Ordinal) + "%type".Length, 2);
                    str = str.Replace("%type", type.ToString());
                    var msgIndex = str.IndexOf("%message", StringComparison.Ordinal);
                    str = str.Replace("%message", string.Format(message, args));

                    var colors = str.Split('@');

                    // Reset console line.
                    Console.ForegroundColor = fg;
                    Console.BackgroundColor = bg;
                    FixBuffer();
                    Console.CursorLeft = 0;

                    // For each color string, change the console color and write the text.
                    for (var i = 0; i < colors.Length; i++)
                    {
                        var colorStr = colors[i];
                        if (string.IsNullOrWhiteSpace(colorStr)) continue;
                        var color = FromHex(colorStr, fg, bg, level, type);

                        Console.ForegroundColor = color.Item1;
                        Console.BackgroundColor = color.Item2;

                        if (i == colors.Length - 1)
                            Console.WriteLine(colorStr.Substring(2));
                        else
                            Console.Write(colorStr.Substring(2));

                        Console.ForegroundColor = fg;
                        Console.BackgroundColor = bg;
                    }

                    Console.ForegroundColor = ConsoleColor.Gray;
                    Console.BackgroundColor = ConsoleColor.Black;

                    var msg = str.Substring(msgIndex);
                    var fullmsg = string.Join("", colors.Select(x => x.Substring(Math.Min(x.Length, 2))));
                    OnMessageLogged(msg, level, type, DateTime.Now, fullmsg);
                }
            }
            catch (Exception e)
            {
                Console.CursorTop = 0;
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Error logging message: " + e);
                Console.ForegroundColor = ConsoleColor.Gray;
            }
        }

        /// <summary>
        /// Due to issues with the console implementation on mono, if the screen size changes, an error can occur.
        /// (CursorTop being more than buffer height) To fix this, if the screen size changes, it will be cleared.
        /// (Simply setting the CursorTop to 0 without clearing will result in text be written over old text).
        /// </summary>
        private static void FixBuffer()
        {
            if (Console.BufferHeight != lastHeight)
            {
                Console.CursorTop = 0;
                Console.Clear();
                for (var i = 0; i < lastHeight; i++)
                {
                    WriteBreak();
                }
                Console.CursorTop = Console.BufferHeight - 1;
            }
            lastHeight = Console.BufferHeight;
        }

        private static Tuple<ConsoleColor, ConsoleColor> FromHex(string colorStr, ConsoleColor fg, ConsoleColor bg,
            LogLevel level, LogType type)
        {
            // First 2 characters of segment contain color hex code:
            var hex = colorStr.Substring(0, 2);

            switch (hex)
            {
                // Special codes begin with an X.
                case "XL":
                    return new Tuple<ConsoleColor, ConsoleColor>(level.FG, level.BG);
                case "XT":
                    return new Tuple<ConsoleColor, ConsoleColor>(type.FG, type.BG);
                case "XR":
                    return new Tuple<ConsoleColor, ConsoleColor>(fg, bg);
                default:
                    // Parse regular hex codes.
                    return new Tuple<ConsoleColor, ConsoleColor>(
                        (ConsoleColor) int.Parse(hex[1].ToString(), NumberStyles.HexNumber),
                        (ConsoleColor) int.Parse(hex[0].ToString(), NumberStyles.HexNumber));
            }
        }

        private static void OnMessageInput(string input) => InputEntered?.Invoke(input);

        private static void OnMessageLogged(string message, LogLevel level, LogType type, DateTime time,
            string fullmessage)
            => MessageLogged?.Invoke(message, level, type, time, fullmessage);

        public delegate void InputEventHandler(string input);

        public delegate void LogEventHandler(
            string message, LogLevel level, LogType type, DateTime time, string fullmessage);
    }
}