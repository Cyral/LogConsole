using System;
using System.Timers;
using Pyratron.Frameworks.LogConsole;

namespace Demo
{
    /// <summary>
    /// Example program.
    /// </summary>
    public class Program
    {
        private static Timer timer;
        private static Random random;
        public static void Main(string[] args)
        {
            Logger.MessageLogged += (message, level, type, time, fullmessage) =>
            {
                //Logger.LogToFile("C:\\Logs", fullmessage);
            };
            random = new Random();
            timer = new Timer(500);
            var testLogType = new LogType("Derp", ConsoleColor.Cyan);
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