using System.Reflection;

namespace ZENITH.Services;

public static class ApplicationInfo
{
    public const string Name = "ZENITH";

    public static string Version { get; } = GetVersion();
    public static string VersionText => $"Version {Version}";
    public static string NameAndVersion => $"{Name} {Version}";

    private static string GetVersion()
    {
        Assembly assembly = typeof(ApplicationInfo).Assembly;

        return assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
            ?? assembly.GetName().Version?.ToString()
            ?? "unknown";
    }
}