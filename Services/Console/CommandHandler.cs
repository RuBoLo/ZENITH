using Avalonia.Controls.ApplicationLifetimes;
using ZENITH.Services;

namespace ZENITH.Services.Console;

public static class CommandHandler
{
    public static void Execute(string input, IConsoleService console)
    {
        string commandLine = input.Trim();
        if (commandLine.Length == 0)
            return;

        int argumentStart = commandLine.IndexOf(' ');
        string command = argumentStart < 0
            ? commandLine
            : commandLine[..argumentStart];
        string arguments = argumentStart < 0
            ? string.Empty
            : commandLine[(argumentStart + 1)..].Trim();

        switch (command.ToLowerInvariant())
        {
            case "?":
            case "help":
                console.Write("Available commands:", ConsoleMessageType.System);
                console.Write("  help      Show this list");
                console.Write("  clear     Clear the console");
                console.Write("  echo      Write text back to the console");
                console.Write("  hide      Hide the console");
                console.Write("  version   Show the current version");
                console.Write("  exit      Exit the application");
                break;

            case "cls":
            case "clear":
                console.Clear();
                break;

            case "echo":
                if (arguments.Length == 0)
                    console.Write("Usage: echo <text>", ConsoleMessageType.Warning);
                else
                    console.Write(arguments);
                break;

            case "hide":
                console.Hide();
                break;

            case "quit":
            case "exit":
                console.Write("Exiting ZENITH.", ConsoleMessageType.System);
                if (Avalonia.Application.Current?.ApplicationLifetime
                    is IClassicDesktopStyleApplicationLifetime desktop)
                {
                    desktop.Shutdown();
                }
                else
                {
                    console.Write("Desktop lifetime is unavailable.", ConsoleMessageType.Error);
                }
                break;

            case "version":
                console.Write(ApplicationInfo.NameAndVersion, ConsoleMessageType.Success);
                break;

            default:
                console.Write($"Unknown command: '{command}'. Type 'help' for available commands.", ConsoleMessageType.Error);
                break;
        }
    }

}
