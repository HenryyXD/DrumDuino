using DrumDuino.Core.Protocol;

namespace DrumDuino.Core.Protocol;

public sealed class SysExFrameReader
{
    private readonly List<byte> _buffer = new();

    public IEnumerable<SysExMessage> Push(byte data)
    {
        if (data == 0xF0)
        {
            _buffer.Clear();
            _buffer.Add(data);
            return Array.Empty<SysExMessage>();
        }

        if (_buffer.Count == 0)
        {
            return Array.Empty<SysExMessage>();
        }

        _buffer.Add(data);
        if (data != 0xF7)
        {
            return Array.Empty<SysExMessage>();
        }

        if (!SysExCodec.TryParseFrame(_buffer.ToArray(), out var message))
        {
            _buffer.Clear();
            return Array.Empty<SysExMessage>();
        }

        _buffer.Clear();
        return new[] { message };
    }

    public void PushRange(byte[] data, List<SysExMessage> output)
    {
        foreach (var b in data)
        {
            foreach (var message in Push(b))
            {
                output.Add(message);
            }
        }
    }
}
