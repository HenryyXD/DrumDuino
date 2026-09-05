using DrumDuino.Core.Models;

namespace DrumDuino.App.Services;

public sealed class KitHistoryService
{
    private readonly Stack<DrumKit> _undo = new();
    private readonly Stack<DrumKit> _redo = new();
    private const int MaxDepth = 30;

    public int UndoCount => _undo.Count;
    public int RedoCount => _redo.Count;

    public void Push(DrumKit kit)
    {
        _undo.Push(kit.Clone());
        _redo.Clear();
        TrimUndo();
    }

    public bool TryUndo(DrumKit current, out DrumKit? restored)
    {
        if (_undo.Count == 0)
        {
            restored = null;
            return false;
        }

        _redo.Push(current.Clone());
        restored = _undo.Pop();
        return true;
    }

    public bool TryRedo(DrumKit current, out DrumKit? restored)
    {
        if (_redo.Count == 0)
        {
            restored = null;
            return false;
        }

        _undo.Push(current.Clone());
        restored = _redo.Pop();
        return true;
    }

    public void Clear()
    {
        _undo.Clear();
        _redo.Clear();
    }

    private void TrimUndo()
    {
        while (_undo.Count > MaxDepth)
        {
            var items = _undo.ToArray();
            _undo.Clear();
            for (var i = items.Length - 2; i >= 0; i--)
            {
                _undo.Push(items[i]);
            }
        }
    }
}
