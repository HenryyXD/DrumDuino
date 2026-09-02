namespace DrumDuino.App.Helpers;

public static class MidiNoteHelper
{
    private static readonly string[] NoteNames = ["C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B"];

    public static string GetName(byte note)
    {
        var octave = note / 12 - 1;
        var name = NoteNames[note % 12];
        return $"{name}{octave}";
    }

    public static string Format(byte note) => $"{GetName(note)} ({note})";
}
