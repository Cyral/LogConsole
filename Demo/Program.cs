using System;
using System.IO;
using System.Timers;

namespace Pyratron.Frameworks.LogConsole.Demo
{
    /// <summary>
    /// Example program.
    /// https://github.com/Pyratron/LogConsole/
    /// </summary>
    public class Program
    {
        private static Timer timer;
        private static Random random;
        public static void Main()
        {
            Logger.LogDirectory = Path.Combine("C:", "Logs");
            Logger.MessageLogged += (message, level, type, time, fullmessage) =>
            {
                Logger.LogToFile(fullmessage);
            };
            random = new Random();
            timer = new Timer(500);
            var testLogType = new LogType("Test", ConsoleColor.Cyan);
            timer.Elapsed += (sender, eventArgs) =>
            {
                var rand = random.Next(1, 8);
                switch (rand)
                {
                    case 1:
                        Logger.Fatal("Ouch!");
                        break;
                    case 2:
                        Logger.Error(testLogType, "Whoops.");
                        break;
                    case 3:
                        Logger.Warn("Something.");
                        break;
                    case 4:
                        Logger.Info("Something.");
                        break;
                    case 5:
                        Logger.Debug("Output.");
                        break;
                    case 6:
                        Logger.Trace("Hint.");
                        break;
                    case 7:
                        Logger.Log(testLogType, ConsoleColor.Magenta, "Test Color.");
                        break;
                }
            };
            timer.Start();
            Logger.Wait();
        }
    }
}