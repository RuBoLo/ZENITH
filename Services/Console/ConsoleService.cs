using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ZENITH.Services.Console;

public class ConsoleService : ObservableObject, IConsoleService
{
    private const int MaxEntries = 2048;
    private readonly List<string> _history = [];
    private readonly RelayCommand _clearCommand;
    private int _historyCursor;
    private bool _isVisible;
    private bool _isRecallingHistory;
    private string _inputText = string.Empty;

    public ObservableCollection<ConsoleEntry> Entries { get; } = [];

    public ConsoleService()
    {
        ExecuteCommand = new RelayCommand(Execute);
        _clearCommand = new RelayCommand(Clear, () => HasEntries);
        ClearCommand = _clearCommand;
        ToggleVisibilityCommand = new RelayCommand(ToggleVisibility);
        HideCommand = new RelayCommand(Hide);

        AddEntry(new ConsoleEntry("Console initialized.", ConsoleMessageType.System), reveal: true);

        Write("███████╗███████╗███╗   ██╗██╗████████╗██╗  ██╗\n" +
              "╚══███╔╝██╔════╝████╗  ██║██║╚══██╔══╝██║  ██║\n" +
              "  ███╔╝ █████╗  ██╔██╗ ██║██║   ██║   ███████║\n" +
              " ███╔╝  ██╔══╝  ██║╚██╗██║██║   ██║   ██╔══██║\n" +
              "███████╗███████╗██║ ╚████║██║   ██║   ██║  ██║\n" +
              "╚══════╝╚══════╝╚═╝  ╚═══╝╚═╝   ╚═╝   ╚═╝  ╚═╝", ConsoleMessageType.Default);
    }

    public string InputText
    {
        get => _inputText;
        set
        {
            if (SetProperty(ref _inputText, value) && !_isRecallingHistory)
                _historyCursor = _history.Count;
        }
    }

    public bool IsVisible
    {
        get => _isVisible;
        set => SetProperty(ref _isVisible, value);
    }

    public ICommand ExecuteCommand { get; }
    public ICommand ClearCommand { get; }
    public ICommand ToggleVisibilityCommand { get; }
    public ICommand HideCommand { get; }

    public bool HasEntries => Entries.Count > 0;

    public void Write(string text, ConsoleMessageType type = ConsoleMessageType.Default)
    {
        AddEntry(new ConsoleEntry(text, type), reveal: true);
    }

    public ConsoleEntry WritePersistent(string text, ConsoleMessageType type = ConsoleMessageType.Default)
    {
        ConsoleEntry entry = new(text, type);
        return AddEntry(entry, reveal: true);
    }

    public void Clear()
    {
        Entries.Clear();
        _history.Clear();
        NotifyEntriesChanged();
    }

    public void Execute()
    {
        Execute(InputText);
    }

    public void Execute(string input)
    {
        string command = input.Trim();
        InputText = string.Empty;

        if (command.Length == 0)
            return;

        AddToHistory(command);
        Write($"> {command}", ConsoleMessageType.Command);

        try
        {
            CommandHandler.Execute(command, this);
        }
        catch (System.Exception ex)
        {
            Write(ex.Message, ConsoleMessageType.Error);
        }
    }

    public void Show()
    {
        IsVisible = true;
    }

    public void Hide()
    {
        IsVisible = false;
    }

    public void ToggleVisibility()
    {
        IsVisible = !IsVisible;
    }

    public void RecallPrevious()
    {
        if (_history.Count == 0)
            return;

        _historyCursor = System.Math.Max(0, _historyCursor - 1);
        SetInputFromHistory(_history[_historyCursor]);
    }

    public void RecallNext()
    {
        if (_history.Count == 0)
            return;

        if (_historyCursor >= _history.Count - 1)
        {
            _historyCursor = _history.Count;
            SetInputFromHistory(string.Empty);
            return;
        }

        _historyCursor++;
        SetInputFromHistory(_history[_historyCursor]);
    }

    private ConsoleEntry AddEntry(ConsoleEntry entry, bool reveal)
    {
        Entries.Add(entry);

        while (Entries.Count > MaxEntries)
            Entries.RemoveAt(0);

        NotifyEntriesChanged();

        if (reveal) Show();

        return entry;
    }

    private void AddToHistory(string command)
    {
        if (_history.Count == 0 || _history[^1] != command)
            _history.Add(command);

        _historyCursor = _history.Count;
    }

    private void SetInputFromHistory(string input)
    {
        _isRecallingHistory = true;
        try
        {
            InputText = input;
        }
        finally
        {
            _isRecallingHistory = false;
        }
    }

    private void NotifyEntriesChanged()
    {
        OnPropertyChanged(nameof(HasEntries));
        _clearCommand.NotifyCanExecuteChanged();
    }
}