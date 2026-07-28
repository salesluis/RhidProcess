namespace RhidProcess.Monitoring;

public sealed record AutomationTelemetrySnapshot(
    long Total,
    long Succeeded,
    long Failed,
    long Cancelled,
    DateTimeOffset? LastStartedAtUtc,
    DateTimeOffset? LastCompletedAtUtc,
    long? LastDurationMs,
    string? LastFailureCode,
    string? LastFailureStage);

public sealed class AutomationTelemetry
{
    private readonly object _sync = new();
    private long _total;
    private long _succeeded;
    private long _failed;
    private long _cancelled;
    private DateTimeOffset? _lastStartedAtUtc;
    private DateTimeOffset? _lastCompletedAtUtc;
    private long? _lastDurationMs;
    private string? _lastFailureCode;
    private string? _lastFailureStage;

    public void RecordStarted()
    {
        lock (_sync)
        {
            _total++;
            _lastStartedAtUtc = DateTimeOffset.UtcNow;
        }
    }

    public void RecordSuccess(long durationMs)
    {
        lock (_sync)
        {
            _succeeded++;
            _lastCompletedAtUtc = DateTimeOffset.UtcNow;
            _lastDurationMs = durationMs;
            _lastFailureCode = null;
            _lastFailureStage = null;
        }
    }

    public void RecordFailure(string code, string stage, long durationMs)
    {
        lock (_sync)
        {
            _failed++;
            _lastCompletedAtUtc = DateTimeOffset.UtcNow;
            _lastDurationMs = durationMs;
            _lastFailureCode = code;
            _lastFailureStage = stage;
        }
    }

    public void RecordCancelled(long durationMs)
    {
        lock (_sync)
        {
            _cancelled++;
            _lastCompletedAtUtc = DateTimeOffset.UtcNow;
            _lastDurationMs = durationMs;
        }
    }

    public AutomationTelemetrySnapshot GetSnapshot()
    {
        lock (_sync)
        {
            return new AutomationTelemetrySnapshot(
                _total,
                _succeeded,
                _failed,
                _cancelled,
                _lastStartedAtUtc,
                _lastCompletedAtUtc,
                _lastDurationMs,
                _lastFailureCode,
                _lastFailureStage);
        }
    }
}
