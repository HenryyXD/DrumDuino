using System.Text.Json;
using System.Text.Json.Serialization;
using DrumDuino.Core.Models;

namespace DrumDuino.Core.Presets;

public static class KitPresetSerializer
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    };

    public static void Save(DrumKit kit, string path)
    {
        var dto = DrumKitDto.FromModel(kit.Normalize());
        var json = JsonSerializer.Serialize(dto, Options);
        File.WriteAllText(path, json);
    }

    public static DrumKit Load(string path)
    {
        var json = File.ReadAllText(path);
        var dto = JsonSerializer.Deserialize<DrumKitDto>(json, Options)
            ?? throw new InvalidDataException("Preset file is empty or invalid.");
        return dto.ToModel().Normalize();
    }

    private sealed class DrumKitDto
    {
        public string Name { get; set; } = "Default";
        public List<PadConfigDto> Pads { get; set; } = [];

        public static DrumKitDto FromModel(DrumKit kit) => new()
        {
            Name = kit.Name,
            Pads = kit.Pads.Select(PadConfigDto.FromModel).ToList()
        };

        public DrumKit ToModel() => new()
        {
            Name = Name,
            Pads = Pads.Select(p => p.ToModel()).ToList()
        };
    }

    private sealed class PadConfigDto
    {
        public int Index { get; set; }
        public string Name { get; set; } = string.Empty;
        public PadType Type { get; set; }
        public byte Note { get; set; }
        public byte Threshold { get; set; }
        public byte ScanTime { get; set; }
        public byte MaskTime { get; set; }
        public byte Retrigger { get; set; }
        public VelocityCurve Curve { get; set; }
        public byte CurveForm { get; set; }
        public byte XTalk { get; set; }
        public byte XTalkGroup { get; set; }
        public byte Gain { get; set; }
        public byte Channel { get; set; }

        public static PadConfigDto FromModel(PadConfig pad) => new()
        {
            Index = pad.Index,
            Name = pad.Name,
            Type = pad.Type,
            Note = pad.Note,
            Threshold = pad.Threshold,
            ScanTime = pad.ScanTime,
            MaskTime = pad.MaskTime,
            Retrigger = pad.Retrigger,
            Curve = pad.Curve,
            CurveForm = pad.CurveForm,
            XTalk = pad.XTalk,
            XTalkGroup = pad.XTalkGroup,
            Gain = pad.Gain,
            Channel = pad.Channel
        };

        public PadConfig ToModel() => new()
        {
            Index = Index,
            Name = Name,
            Type = Type,
            Note = Note,
            Threshold = Threshold,
            ScanTime = ScanTime,
            MaskTime = MaskTime,
            Retrigger = Retrigger,
            Curve = Curve,
            CurveForm = CurveForm,
            XTalk = XTalk,
            XTalkGroup = XTalkGroup,
            Gain = Gain,
            Channel = Channel
        };
    }
}
