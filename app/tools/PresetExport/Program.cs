using DrumDuino.Core;
using DrumDuino.Core.Presets;

var repoRoot = RepoPaths.FindRepoRoot()
    ?? throw new InvalidOperationException("Could not find DrumDuino repo root.");

var iniPath = Path.Combine(repoRoot, "presets", "kit-atual.ini");
var jsonPath = Path.Combine(repoRoot, "presets", "kit-atual.json");

var kit = PinsIniImporter.Import(iniPath);
KitPresetSerializer.Save(kit, jsonPath);
Console.WriteLine($"Exported {jsonPath}");
