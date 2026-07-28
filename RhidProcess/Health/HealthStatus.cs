using RhidProcess.Monitoring;

namespace RhidProcess.Health;

public static class HealthStatuses
{
    public const string Healthy = "Healthy";
    public const string Degraded = "Degraded";
    public const string Unhealthy = "Unhealthy";
}

public sealed record HealthComponentResult(
    string Status,
    long DurationMs,
    string? Detail = null,
    int? HttpStatus = null,
    AutomationTelemetrySnapshot? Automation = null);

public sealed record ReadinessHealthResponse(
    string Status,
    DateTimeOffset CheckedAtUtc,
    long DurationMs,
    IReadOnlyDictionary<string, HealthComponentResult> Components);

public sealed record LivenessHealthResponse(
    string Status,
    DateTimeOffset CheckedAtUtc);
