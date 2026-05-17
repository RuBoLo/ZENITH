using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ZENITH.ViewModels.Shell;

namespace ZENITH.Services.Workspace;

public interface IImageWorkspaceService
{
    void AttachWorkspace(WorkspaceViewModel workspace);

    Task OpenFilesAsync(IReadOnlyList<string> paths, CancellationToken cancellationToken = default);
}