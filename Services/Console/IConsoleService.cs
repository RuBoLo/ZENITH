using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;

namespace ZENITH.Services.Console;

public interface IConsoleService : INotifyPropertyChanged
{
    ObservableCollection<ConsoleEntry> Entries { get; }

    string InputText { get; set; }
    bool IsVisible { get; set; }

    ICommand ExecuteCommand { get; }
    ICommand ClearCommand { get; }
    ICommand ToggleVisibilityCommand { get; }
    ICommand HideCommand { get; }

    bool HasEntries { get; }

    void Show();
    void Hide();
    void ToggleVisibility();

    void Write(string text, ConsoleMessageType type = ConsoleMessageType.Default);

    ConsoleEntry WritePersistent(string text, ConsoleMessageType type = ConsoleMessageType.Default);

    void Clear();

    void Execute();
    void Execute(string input);

    void RecallPrevious();
    void RecallNext();
}
