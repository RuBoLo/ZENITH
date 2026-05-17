using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;
using System.Collections.Specialized;
using System.ComponentModel;
using ZENITH.Services.Console;

namespace ZENITH.Views.Shell;

public partial class ConsoleView : UserControl
{
    private IConsoleService? _console;

    public ConsoleView()
    {
        InitializeComponent();

        DataContextChanged += (_, _) => AttachConsole(DataContext as IConsoleService);
        DetachedFromVisualTree += (_, _) => AttachConsole(null);
    }

    private void AttachConsole(IConsoleService? console)
    {
        if (_console is not null)
        {
            _console.Entries.CollectionChanged -= Entries_CollectionChanged;
            _console.PropertyChanged -= Console_PropertyChanged;
        }

        _console = console;

        if (_console is null)
            return;

        _console.Entries.CollectionChanged += Entries_CollectionChanged;
        _console.PropertyChanged += Console_PropertyChanged;

        if (_console.IsVisible)
            FocusInput();

        ScrollToBottom();
    }

    private void Entries_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        ScrollToBottom();
    }

    private void Console_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(IConsoleService.IsVisible) && _console?.IsVisible == true)
            FocusInput();
    }

    private void InputTextBox_KeyDown(object? sender, KeyEventArgs e)
    {
        if (_console is null) return;

        switch (e.Key)
        {
            case Key.Enter:
                _console.Execute();
                e.Handled = true;
                break;

            case Key.Up:
                _console.RecallPrevious();
                MoveCaretToEnd();
                e.Handled = true;
                break;

            case Key.Down:
                _console.RecallNext();
                MoveCaretToEnd();
                e.Handled = true;
                break;

            case Key.Escape:
                _console.Hide();
                e.Handled = true;
                break;
        }
    }

    private void FocusInput()
    {
        Dispatcher.UIThread.Post(() =>
        {
            InputTextBox.Focus();
            MoveCaretToEnd();
        });
    }

    private void MoveCaretToEnd()
    {
        Dispatcher.UIThread.Post(() =>
        {
            InputTextBox.CaretIndex = InputTextBox.Text?.Length ?? 0;
        });
    }

    private void ScrollToBottom()
    {
        Dispatcher.UIThread.Post(() => EntriesScrollViewer.ScrollToEnd());
    }
}
