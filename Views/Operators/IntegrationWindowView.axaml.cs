using Avalonia.Controls;
using Avalonia.Interactivity;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using ZENITH.Rendering.BitmapDecoding;
using ZENITH.Services.FilePicker;
using ZENITH.ViewModels.Operators;

namespace ZENITH.Views.Operators;

public partial class IntegrationWindowView : UserControl
{
    public IntegrationWindowView()
    {
        InitializeComponent();
    }

    private async void AddFrames_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not IntegrationWindowViewModel viewModel)
            return;

        if (TopLevel.GetTopLevel(this) is not Window owner)
            return;

        IFilePickerService filePicker = App.Services.GetRequiredService<IFilePickerService>();
        IReadOnlyList<string> paths = await filePicker.PickFilesAsync(
            owner,
            "Select Image Frames",
            allowMultiple: true,
            filter: BitmapFactory.IsSupported);

        viewModel.AddFramePaths(paths);
    }
}