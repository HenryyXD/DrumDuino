using DrumDuino.Core.Models;
using DrumDuino.Core.Presets;

namespace DrumDuino.App.Services;

public sealed record PadProfileInfo(string Name, string FilePath, DateTime Modified);

public sealed class PadProfileService
{
    private static string ProfilesDirectory
    {
        get
        {
            var dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "DrumDuino",
                "profiles");
            Directory.CreateDirectory(dir);
            return dir;
        }
    }

    public IReadOnlyList<PadProfileInfo> ListProfiles()
    {
        return Directory.EnumerateFiles(ProfilesDirectory, "*.json")
            .Select(path => new PadProfileInfo(
                Path.GetFileNameWithoutExtension(path),
                path,
                File.GetLastWriteTime(path)))
            .OrderBy(p => p.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public void SaveProfile(string name, PadConfig pad)
    {
        var safe = SanitizeFileName(name);
        var path = Path.Combine(ProfilesDirectory, $"{safe}.json");
        var kit = new DrumKit { Name = safe, Pads = [pad.Clone()] };
        KitPresetSerializer.Save(kit, path);
    }

    public PadConfig LoadProfile(string filePath)
    {
        var kit = KitPresetSerializer.Load(filePath);
        var pad = kit.Pads.FirstOrDefault() ?? PadConfig.CreateDefault(0);
        return pad.Clone();
    }

    public void DeleteProfile(string filePath)
    {
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }
    }

    private static string SanitizeFileName(string name)
    {
        foreach (var c in Path.GetInvalidFileNameChars())
        {
            name = name.Replace(c, '_');
        }

        return string.IsNullOrWhiteSpace(name) ? "profile" : name.Trim();
    }
}
