namespace DrumDuino.Core.Models;

public readonly record struct PadHitEvent(byte PadIndex, byte Value, DateTime Timestamp);
