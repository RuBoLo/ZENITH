using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using System;
using System.Collections.Generic;
using ZENITH.Services.FilePicker;
using ZENITH.ViewModels.Dialogs;

namespace ZENITH.Views.Dialogs;

public partial class FilePickerView : Window
{
    public string TitleText { get; }

    public IReadOnlyList<string> Results { get; private set; } = [];

    private FilePickerViewModel Vm => (FilePickerViewModel)DataContext!;

    // Parameterless constructor for Avalonia tooling/XAML; defaults to single selection
    public FilePickerView() : this("Select Files", false)
    {
    }

    public FilePickerView(string title, bool allowMultiple, Func<string, bool>? filter = null)
    {
        TitleText = title;

        InitializeComponent();

        DataContext = new FilePickerViewModel(allowMultiple)
        {
            FileFilter = filter
        };

        // Initial navigation
        string start = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Desktop);

        _ = Vm.NavigateToAsync(start);
    }

    private async void Crumb_Click(object? sender, RoutedEventArgs e)
    {
        // Breadcrumb buttons store their target folder path in Tag
        if (sender is Button { Tag: string path }) await Vm.NavigateToAsync(path);
    }

    private async void List_DoubleTapped(object? sender, TappedEventArgs e)
    {
        if (FileList.SelectedItem is FileItem item)
        {
            if (item.IsDirectory) await Vm.OpenFolderAsync(item);

            // Double-clicking a file accepts it immediately
            else
            {
                Vm.UpdateSelection(item, true);
                Results = Vm.SelectedPaths;
                Close();
            }
        }
    }

    // Stop any background folder scan when the dialog is closed
    protected override void OnClosed(EventArgs e)
    {
        if (DataContext is FilePickerViewModel vm) vm.CancelNavigation();

        base.OnClosed(e);
    }

    // Mirror Avalonia's ListBox selection changes into the view model's selected files
    private void FileList_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (DataContext is not FilePickerViewModel vm) return;

        foreach (FileItem item in e.AddedItems) vm.UpdateSelection(item, true);

        foreach (FileItem item in e.RemovedItems) vm.UpdateSelection(item, false);
    }

    private void DragBar_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed) BeginMoveDrag(e);
    }

    private void Open_Click(object? sender, RoutedEventArgs e)
    {
        Results = Vm.SelectedPaths;
        Close();
    }

    private void Cancel_Click(object? sender, RoutedEventArgs e)
    {
        Results = [];
        Close();
    }
}