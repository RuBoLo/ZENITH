namespace ZENITH.Services.FilePicker;

public class PathSegment(string fullPath)
{
    public string FullPath { get; } = fullPath;

    public string Label { get; } = GetLabel(fullPath);

    private static string GetLabel(string path)
    {
        string label = System.IO.Path.GetFileName(path);

        return string.IsNullOrEmpty(label)
            ? path
            : label;
    }
}