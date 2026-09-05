using NAudio.Midi;

namespace DrumDuino.App.Services;

public sealed class MidiOutputService : IDisposable
{
    private MidiOut? _midiOut;

    public IReadOnlyList<string> GetDeviceNames()
    {
        var names = new List<string>();
        for (var i = 0; i < MidiOut.NumberOfDevices; i++)
        {
            names.Add(MidiOut.DeviceInfo(i).ProductName);
        }

        return names;
    }

    public void Open(int deviceIndex)
    {
        Close();
        _midiOut = new MidiOut(deviceIndex);
    }

    public void SendNoteOn(byte note, byte velocity, byte channel = 0)
    {
        if (_midiOut is null)
        {
            return;
        }

        var message = (int)(MidiCommandCode.NoteOn + channel)
                      | ((int)note << 8)
                      | ((int)Math.Clamp(velocity, (byte)1, (byte)127) << 16);
        _midiOut.Send(message);
    }

    public void Close()
    {
        _midiOut?.Dispose();
        _midiOut = null;
    }

    public void Dispose() => Close();
}
