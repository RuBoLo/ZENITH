using System.IO;

namespace ZENITH.Services.FilePicker;

public class FileItem(string path, bool isDirectory)
{
    public string FullPath { get; } = path;
    public bool IsDirectory { get; } = isDirectory;

    public string Name { get; } = isDirectory
        ? new DirectoryInfo(path).Name
        : Path.GetFileName(path);
}