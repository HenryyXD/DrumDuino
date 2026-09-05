namespace DrumDuino.App.Services;

/// <summary>
/// Dynamics trainer inspired by Beat Studio Live Play Dynamics Graph:
/// target velocity envelope vs actual hits over time (crescendo / decrescendo / swell).
/// </summary>
public enum DynamicsPattern
{
    Crescendo,
    Decrescendo,
    Swell,
    Steady
}

public sealed record DynamicsHit(double TimeSec, byte Velocity, byte Target, int Error);

public sealed class DynamicsTrainingService
{
    private readonly List<DynamicsHit> _hits = [];
    private readonly object _lock = new();
    private DateTimeOffset? _startedAt;

    public bool IsRunning { get; private set; }
    public DynamicsPattern Pattern { get; private set; } = DynamicsPattern.Crescendo;
    public double DurationSec { get; private set; } = 16;
    public int Tolerance { get; private set; } = 18;
    public byte MinVelocity { get; private set; } = 20;
    public byte MaxVelocity { get; private set; } = 110;
    public int? FocusPadIndex { get; private set; }

    public event Action? Changed;

    public IReadOnlyList<DynamicsHit> Hits
    {
        get
        {
            lock (_lock)
            {
                return _hits.ToList();
            }
        }
    }

    public double ElapsedSec
    {
        get
        {
            if (_startedAt is null || !IsRunning)
            {
                return 0;
            }

            return Math.Min(DurationSec, (DateTimeOffset.UtcNow - _startedAt.Value).TotalSeconds);
        }
    }

    public double ScorePercent
    {
        get
        {
            lock (_lock)
            {
                if (_hits.Count == 0)
                {
                    return 0;
                }

                var good = _hits.Count(h => h.Error <= Tolerance);
                return 100.0 * good / _hits.Count;
            }
        }
    }

    public int HitCount
    {
        get
        {
            lock (_lock)
            {
                return _hits.Count;
            }
        }
    }

    public int InToleranceCount
    {
        get
        {
            lock (_lock)
            {
                return _hits.Count(h => h.Error <= Tolerance);
            }
        }
    }

    public void Configure(DynamicsPattern pattern, double durationSec, int tolerance, byte minVel, byte maxVel, int? focusPad)
    {
        Pattern = pattern;
        DurationSec = Math.Clamp(durationSec, 4, 120);
        Tolerance = Math.Clamp(tolerance, 4, 40);
        MinVelocity = minVel;
        MaxVelocity = maxVel;
        FocusPadIndex = focusPad;
    }

    public void Start()
    {
        lock (_lock)
        {
            _hits.Clear();
            _startedAt = DateTimeOffset.UtcNow;
            IsRunning = true;
        }

        Changed?.Invoke();
    }

    public void Stop()
    {
        IsRunning = false;
        Changed?.Invoke();
    }

    public void Reset()
    {
        lock (_lock)
        {
            _hits.Clear();
            _startedAt = null;
            IsRunning = false;
        }

        Changed?.Invoke();
    }

    public byte GetTargetAt(double timeSec)
    {
        if (DurationSec <= 0)
        {
            return MinVelocity;
        }

        var t = Math.Clamp(timeSec / DurationSec, 0, 1);
        var min = (double)MinVelocity;
        var max = (double)MaxVelocity;
        var span = max - min;

        var y = Pattern switch
        {
            DynamicsPattern.Crescendo => min + span * t,
            DynamicsPattern.Decrescendo => max - span * t,
            DynamicsPattern.Swell => t < 0.5
                ? min + span * (t * 2)
                : max - span * ((t - 0.5) * 2),
            DynamicsPattern.Steady => min + span * 0.5,
            _ => min + span * t
        };

        return (byte)Math.Clamp(Math.Round(y), 1, 127);
    }

    public IReadOnlyList<(double TimeSec, byte Velocity)> GetTargetPolyline(int samples = 64)
    {
        var points = new List<(double, byte)>(samples + 1);
        for (var i = 0; i <= samples; i++)
        {
            var t = DurationSec * i / samples;
            points.Add((t, GetTargetAt(t)));
        }

        return points;
    }

    public bool TryRecordHit(int padIndex, byte velocity, out DynamicsHit? hit)
    {
        hit = null;
        if (!IsRunning || _startedAt is null)
        {
            return false;
        }

        if (FocusPadIndex is int focus && focus != padIndex)
        {
            return false;
        }

        var time = (DateTimeOffset.UtcNow - _startedAt.Value).TotalSeconds;
        if (time > DurationSec)
        {
            IsRunning = false;
            Changed?.Invoke();
            return false;
        }

        var target = GetTargetAt(time);
        var error = Math.Abs(velocity - target);
        hit = new DynamicsHit(time, velocity, target, error);

        lock (_lock)
        {
            _hits.Add(hit);
        }

        Changed?.Invoke();
        return true;
    }
}
