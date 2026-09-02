namespace DrumDuino.Core;

public static class RepoPaths
{
    public static string? FindRepoRoot(string? startDirectory = null)
    {
        var current = new DirectoryInfo(startDirectory ?? AppContext.BaseDirectory);
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "AGENTS.md"))
                && Directory.Exists(Path.Combine(current.FullName, "firmware"))
                && Directory.Exists(Path.Combine(current.FullName, "presets")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        return null;
    }

    public static string? FindPresetPath(string fileName)
    {
        var root = FindRepoRoot();
        if (root is null)
        {
            return null;
        }

        var path = Path.Combine(root, "presets", fileName);
        return File.Exists(path) ? path : null;
    }
}
