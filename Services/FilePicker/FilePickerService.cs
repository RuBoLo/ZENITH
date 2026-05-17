using Avalonia.Controls;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ZENITH.Views.Dialogs;

namespace ZENITH.Services.FilePicker;

public sealed class FilePickerService : IFilePickerService
{
    public async Task<IReadOnlyList<string>> PickFilesAsync(
        Window owner,
        string title,
        bool allowMultiple,
        Func<string, bool>? filter = null)
    {
        FilePickerView picker = new(title, allowMultiple, filter);

        await picker.ShowDialog(owner);

        return picker.Results;
    }
}