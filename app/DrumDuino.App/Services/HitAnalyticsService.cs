using DrumDuino.Core;

namespace DrumDuino.App.Services;

public sealed record HitRecord(DateTimeOffset Time, int PadIndex, byte Velocity);

public sealed record CrosstalkEvent(DateTimeOffset Time, int PadA, int PadB, byte VelocityA, byte VelocityB);

public sealed class HitAnalyticsService
{
    private const int MaxHistory = 200;
    private const int HeatmapSeconds = 8;
    private const double CrosstalkWindowMs = 35;

    private readonly LinkedList<HitRecord> _history = new();
    private readonly object _lock = new();
    private CrosstalkEvent? _lastCrosstalk;

    public event Action? HistoryChanged;

    public IReadOnlyList<HitRecord> History
    {
        get
        {
            lock (_lock)
            {
                return _history.ToList();
            }
        }
    }

    public CrosstalkEvent? LastCrosstalk
    {
        get
        {
            lock (_lock)
            {
                return _lastCrosstalk;
            }
        }
    }

    public void RecordHit(int padIndex, byte velocity)
    {
        if (padIndex < 0 || padIndex >= MicroDrumConstants.PadCount)
        {
            return;
        }

        var now = DateTimeOffset.UtcNow;
        lock (_lock)
        {
            var recent = _history.LastOrDefault();
            if (recent is not null
                && recent.PadIndex != padIndex
                && (now - recent.Time).TotalMilliseconds <= CrosstalkWindowMs)
            {
                _lastCrosstalk = new CrosstalkEvent(now, recent.PadIndex, padIndex, recent.Velocity, velocity);
            }

            _history.AddLast(new HitRecord(now, padIndex, velocity));
            while (_history.Count > MaxHistory)
            {
                _history.RemoveFirst();
            }
        }

        HistoryChanged?.Invoke();
    }

    public void Clear()
    {
        lock (_lock)
        {
            _history.Clear();
            _lastCrosstalk = null;
        }

        HistoryChanged?.Invoke();
    }

    /// <summary>
    /// Heatmap[padIndex, bucket] = intensity 0..1 for last N seconds.
    /// </summary>
    public double[,] GetHeatmap(int bucketCount = HeatmapSeconds)
    {
        var result = new double[MicroDrumConstants.PadCount, bucketCount];
        var now = DateTimeOffset.UtcNow;
        lock (_lock)
        {
            foreach (var hit in _history)
            {
                var ageSec = (now - hit.Time).TotalSeconds;
                if (ageSec < 0 || ageSec >= bucketCount)
                {
                    continue;
                }

                var bucket = bucketCount - 1 - (int)ageSec;
                var intensity = hit.Velocity / 127.0;
                result[hit.PadIndex, bucket] = Math.Min(1, result[hit.PadIndex, bucket] + intensity * 0.5);
            }
        }

        return result;
    }

    /// <summary>
    /// Average velocity per pad per second bucket (0..127).
    /// </summary>
    public byte[,] GetIntensityGrid(int seconds = 8)
    {
        var sums = new int[MicroDrumConstants.PadCount, seconds];
        var counts = new int[MicroDrumConstants.PadCount, seconds];
        var now = DateTimeOffset.UtcNow;

        lock (_lock)
        {
            foreach (var hit in _history)
            {
                var ageSec = (int)(now - hit.Time).TotalSeconds;
                if (ageSec < 0 || ageSec >= seconds)
                {
                    continue;
                }

                var bucket = seconds - 1 - ageSec;
                sums[hit.PadIndex, bucket] += hit.Velocity;
                counts[hit.PadIndex, bucket]++;
            }
        }

        var result = new byte[MicroDrumConstants.PadCount, seconds];
        for (var p = 0; p < MicroDrumConstants.PadCount; p++)
        {
            for (var s = 0; s < seconds; s++)
            {
                result[p, s] = counts[p, s] == 0
                    ? (byte)0
                    : (byte)Math.Clamp(sums[p, s] / counts[p, s], 0, 127);
            }
        }

        return result;
    }
}
