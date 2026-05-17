using Avalonia.Controls;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ZENITH.Services.FilePicker;

namespace ZENITH.ViewModels.Dialogs;

public class FilePickerViewModel(bool allowMultiple) : ObservableObject
{
    private CancellationTokenSource? _navigationCts;
    private IReadOnlyList<FileItem> _items = [];
    private IReadOnlyList<PathSegment> _breadcrumbs = [];

    public IReadOnlyList<FileItem> Items
    {
        get => _items;
        private set => SetProperty(ref _items, value);
    }

    public ObservableCollection<FileItem> SelectedItems { get; } = [];

    public IReadOnlyList<PathSegment> Breadcrumbs
    {
        get => _breadcrumbs;
        private set => SetProperty(ref _breadcrumbs, value);
    }

    public bool AllowMultiple { get; } = allowMultiple;

    public SelectionMode PickerSelectionMode { get; } = allowMultiple ? SelectionMode.Multiple : SelectionMode.Single;

    public Func<string, bool>? FileFilter { get; set; }

    public IReadOnlyList<string> SelectedPaths => [.. SelectedItems.Where(i => !i.IsDirectory).Select(i => i.FullPath)];

    private sealed record DirectorySnapshot(IReadOnlyList<FileItem> Items, IReadOnlyList<PathSegment> Breadcrumbs);

    public async Task NavigateToAsync(string path)
    {
        // Cancels previous folder scan before starting a new one
        _navigationCts?.Cancel();

        using CancellationTokenSource cts = new();
        _navigationCts = cts;

        try
        {
            // Enumerate and sort the directory off the UI thread
            DirectorySnapshot? snapshot = await Task.Run(
                () => LoadDirectory(path, cts.Token),
                cts.Token);

            if (snapshot is null) return;

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                // Ignore results from an older navigation
                if (!IsCurrentNavigation(cts)) return;

                Items = snapshot.Items;
                Breadcrumbs = snapshot.Breadcrumbs;
                SelectedItems.Clear();
            });
        }
        catch (OperationCanceledException) when (cts.IsCancellationRequested)
        {
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
        }
        finally
        {
            if (ReferenceEquals(_navigationCts, cts)) _navigationCts = null;
        }
    }

    private bool IsCurrentNavigation(CancellationTokenSource cts) =>
        ReferenceEquals(_navigationCts, cts) && !cts.IsCancellationRequested;

    private DirectorySnapshot? LoadDirectory(string path, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!Directory.Exists(path)) return null;

        List<string> directories = GetSortedPaths(Directory.EnumerateDirectories(path), cancellationToken);

        List<string> files = GetSortedPaths(Directory.EnumerateFiles(path), cancellationToken, ShouldIncludeFile);

        IReadOnlyList<FileItem> items =
        [
            .. directories.Select(static dir => new FileItem(dir, true)),
            .. files.Select(static file => new FileItem(file, false))
        ];

        return new DirectorySnapshot(items, BuildBreadcrumbs(path));
    }

    private static List<string> GetSortedPaths(
        IEnumerable<string> paths,
        CancellationToken cancellationToken,
        Func<string, bool>? filter = null)
    {
        List<string> result = [];

        foreach (string path in paths)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (filter?.Invoke(path) ?? true) result.Add(path);
        }

        result.Sort(StringComparer.OrdinalIgnoreCase);
        return result;
    }

    private static List<PathSegment> BuildBreadcrumbs(string path)
    {
        List<PathSegment> breadcrumbs = [];

        for (string? current = path; current is not null; current = Path.GetDirectoryName(current))
            breadcrumbs.Add(new PathSegment(current));

        breadcrumbs.Reverse();
        return breadcrumbs;
    }

    public void UpdateSelection(FileItem item, bool isSelected)
    {
        if (item.IsDirectory) return;

        if (!AllowMultiple) SelectedItems.Clear();

        if (isSelected)
        {
            if (!SelectedItems.Contains(item)) SelectedItems.Add(item);
        }
        else SelectedItems.Remove(item);
    }

    public Task OpenFolderAsync(FileItem item) =>
        item.IsDirectory ? NavigateToAsync(item.FullPath) : Task.CompletedTask;

    public void CancelNavigation() =>
        _navigationCts?.Cancel();

    private bool ShouldIncludeFile(string path) =>
        FileFilter?.Invoke(path) ?? true;
}