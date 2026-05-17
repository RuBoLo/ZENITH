using Avalonia.Controls;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ZENITH.Services.FilePicker;

public interface IFilePickerService
{
    Task<IReadOnlyList<string>> PickFilesAsync(
        Window owner,
        string title,
        bool allowMultiple,
        Func<string, bool>? filter = null);
}