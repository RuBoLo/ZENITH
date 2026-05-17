using CommunityToolkit.Mvvm.ComponentModel;
using System;

namespace ZENITH.Services.Console;

public enum ConsoleMessageType
{
    Default,
    System,
    Command,
    Error,
    Warning,
    Success
}

public partial class ConsoleEntry(string text, ConsoleMessageType type) : ObservableObject
{
    public DateTime Timestamp { get; } = DateTime.Now;
    public string TimestampText => Timestamp.ToString("HH:mm:ss");

    [ObservableProperty]
    private string text = text;

    public ConsoleMessageType Type { get; } = type;

    public bool IsSystem => Type == ConsoleMessageType.System;
    public bool IsCommand => Type == ConsoleMessageType.Command;
    public bool IsError => Type == ConsoleMessageType.Error;
    public bool IsWarning => Type == ConsoleMessageType.Warning;
    public bool IsSuccess => Type == ConsoleMessageType.Success;
}
