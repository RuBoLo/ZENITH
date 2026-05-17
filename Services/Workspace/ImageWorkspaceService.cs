using Avalonia.Threading;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ZENITH.Rendering.BitmapDecoding;
using ZENITH.Services.Console;
using ZENITH.ViewModels.Documents;
using ZENITH.ViewModels.Shell;

namespace ZENITH.Services.Workspace;

public sealed class ImageWorkspaceService : IImageWorkspaceService
{
    private WorkspaceViewModel? _workspace;

    public void AttachWorkspace(WorkspaceViewModel workspace)
    {
        _workspace = workspace;
    }

    public async Task OpenFilesAsync(
        IReadOnlyList<string> paths,
        CancellationToken cancellationToken = default)
    {
        IConsoleService console = App.Services.GetRequiredService<IConsoleService>();

        WorkspaceViewModel? workspace = _workspace;
        if (workspace is null)
            return;

        foreach (string path in paths.Where(BitmapFactory.IsSupported))
        {
            cancellationToken.ThrowIfCancellationRequested();
            ImageDocumentViewModel? document = null;

            try
            {
                var (X, Y) = workspace.GetNextWindowPosition(480, 360);

                document = new ImageDocumentViewModel(
                    Path.GetFileName(path),
                    new ImageDocumentSource(BitmapFactory.Create(path)),
                    X,
                    Y);

                document.ApplyInitialLayout(
                    workspace.ViewportWidth,
                    workspace.ViewportHeight,
                    workspace.RenderScaling);
                KeepInsideWorkspace(document, workspace);

                await document.LoadPreviewAsync(cancellationToken);

                ImageDocumentViewModel loadedDocument = document;
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    workspace.AddDocument(loadedDocument);
                });

                console.Write($"Opened {path}.");
                document = null;
            }
            catch (OperationCanceledException)
            {
                document?.Dispose();
                throw;
            }
            catch (Exception ex)
            {
                document?.Dispose();
                Debug.WriteLine($"Failed to open image '{Path.GetFileName(path)}': {ex.Message}");
                console.Write($"Failed to open {path}: {ex.Message}");
            }
        }
    }

    private static void KeepInsideWorkspace(
        ImageDocumentViewModel document,
        WorkspaceViewModel workspace)
    {
        document.X = Math.Min(document.X, Math.Max(0, workspace.ViewportWidth - document.Width));
        document.Y = Math.Min(document.Y, Math.Max(0, workspace.ViewportHeight - document.Height));
    }

}
