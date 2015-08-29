# LogConsole
A library for logging messages to the console without overwriting console input.
LogConsole is used in our server applications to allow commands to be entered while messages are being logged to the console.

##Features:
 - Logging to files.
 - Multiple levels of messages. (Debug, Warn, Info, etc.)
 - Customizable message categories. (Example: `"Info | MyCategory: Test"`)
 - Customizable formats for time and message format.

##Example:

````
Logger.MessageLogged += (message, level, type, time, fullmessage) =>
{
    // A message was logged!
    // Example: Log to file (Logger.LogToFile)
};
Logger.InputEntered += input =>
{
    // Console input was entered!
    // Example: Parse command entered (With Command Parser, see https://www.pyratron.com/command-parser)
};

// TODO: Start services that log to the console.
// Examples:
Logger.Log("Hello!");
var reason = "Something happened";
Logger.Error("Uh oh.. {0}", reason);
var category = new LogType("Network", ConsoleColor.Cyan);
Logger.Info(category, "Connected!");

// Start infinite loop looking for input and handling keys.
Logger.Wait();
````

The following hex colors can be used in messages and the log format to be displayed in the console:
![Hex Colors](https://www.pyratron.com/assets/consolehex.png)
