using System.Diagnostics;
using Microsoft.Extensions.Options;
using RhidProcess.Browser;
using RhidProcess.Monitoring;

namespace RhidProcess.Health;

public sealed class RhidHealthService(
    IOptions<RhidOptions> options,
    BrowserRuntimeSettings browserRuntime,
    IHostEnvironment environment,
    IHttpClientFactory httpClientFactory,
    AutomationTelemetry automationTelemetry,
    ILogger<RhidHealthService> logger)
{
    private const string LogsDirectoryName = "Logs";
    private static readonly TimeSpan RhidProbeTimeout = TimeSpan.FromSeconds(5);
    private readonly object _statusSync = new();
    private string? _lastReportedStatus;

    public async Task<ReadinessHealthResponse> GetReadinessAsync(
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();

        var checks = await Task.WhenAll(
            CheckConfigurationAsync(),
            CheckChromeAsync(),
            CheckLogsAsync(cancellationToken),
            CheckRhidAsync(cancellationToken));

        var components = new Dictionary<string, HealthComponentResult>
        {
            ["configuration"] = checks[0],
            ["chrome"] = checks[1],
            ["logs"] = checks[2],
            ["rhid"] = checks[3],
            ["automation"] = GetAutomationComponent()
        };

        var status = GetOverallStatus(components.Values);
        var result = new ReadinessHealthResponse(
            status,
            DateTimeOffset.UtcNow,
            stopwatch.ElapsedMilliseconds,
            components);

        LogStatusChange(result);
        return result;
    }

    private Task<HealthComponentResult> CheckConfigurationAsync()
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var rhid = options.Value;
            var valid = !string.IsNullOrWhiteSpace(rhid.Email)
                        && !string.IsNullOrWhiteSpace(rhid.Password)
                        && Uri.TryCreate(rhid.BaseUrl, UriKind.Absolute, out var uri)
                        && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
                        && !string.IsNullOrWhiteSpace(rhid.LoginRoute)
                        && !string.IsNullOrWhiteSpace(rhid.UnlockRoute)
                        && rhid.DefaultTimeout > 0
                        && rhid.DefaultNavigationTimeout > 0;

            return Task.FromResult(new HealthComponentResult(
                valid ? HealthStatuses.Healthy : HealthStatuses.Unhealthy,
                stopwatch.ElapsedMilliseconds,
                valid ? null : "invalid_configuration"));
        }
        catch (OptionsValidationException)
        {
            return Task.FromResult(new HealthComponentResult(
                HealthStatuses.Unhealthy,
                stopwatch.ElapsedMilliseconds,
                "invalid_configuration"));
        }
    }

    private Task<HealthComponentResult> CheckChromeAsync()
    {
        var stopwatch = Stopwatch.StartNew();
        var exists = File.Exists(browserRuntime.ExecutablePath);

        return Task.FromResult(new HealthComponentResult(
            exists ? HealthStatuses.Healthy : HealthStatuses.Unhealthy,
            stopwatch.ElapsedMilliseconds,
            exists ? null : "executable_not_found"));
    }

    private async Task<HealthComponentResult> CheckLogsAsync(
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var directory = Path.Combine(environment.ContentRootPath, LogsDirectoryName);
            Directory.CreateDirectory(directory);
            var probePath = Path.Combine(directory, $".health-{Guid.NewGuid():N}");

            await using (var stream = new FileStream(
                             probePath,
                             FileMode.CreateNew,
                             FileAccess.Write,
                             FileShare.None,
                             1,
                             FileOptions.Asynchronous | FileOptions.DeleteOnClose))
            {
                await stream.WriteAsync(new byte[] { 0 }, cancellationToken);
                await stream.FlushAsync(cancellationToken);
            }

            return new HealthComponentResult(
                HealthStatuses.Healthy,
                stopwatch.ElapsedMilliseconds);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            return new HealthComponentResult(
                HealthStatuses.Degraded,
                stopwatch.ElapsedMilliseconds,
                "storage_unavailable");
        }
    }

    private async Task<HealthComponentResult> CheckRhidAsync(
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken);
            timeout.CancelAfter(RhidProbeTimeout);

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                options.Value.BaseUrl);
            using var response = await httpClientFactory
                .CreateClient(HttpClientNames.RhidAvailability)
                .SendAsync(
                    request,
                    HttpCompletionOption.ResponseHeadersRead,
                    timeout.Token);

            return new HealthComponentResult(
                response.IsSuccessStatusCode
                    ? HealthStatuses.Healthy
                    : HealthStatuses.Unhealthy,
                stopwatch.ElapsedMilliseconds,
                response.IsSuccessStatusCode ? null : "unexpected_http_status",
                (int)response.StatusCode);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException)
        {
            return new HealthComponentResult(
                HealthStatuses.Unhealthy,
                stopwatch.ElapsedMilliseconds,
                "timeout");
        }
        catch (HttpRequestException)
        {
            return new HealthComponentResult(
                HealthStatuses.Unhealthy,
                stopwatch.ElapsedMilliseconds,
                "network_unavailable");
        }
        catch (Exception)
        {
            return new HealthComponentResult(
                HealthStatuses.Unhealthy,
                stopwatch.ElapsedMilliseconds,
                "probe_failed");
        }
    }

    private HealthComponentResult GetAutomationComponent()
    {
        var snapshot = automationTelemetry.GetSnapshot();
        var status = snapshot.LastFailureCode is null
            ? HealthStatuses.Healthy
            : HealthStatuses.Degraded;

        return new HealthComponentResult(
            status,
            0,
            snapshot.LastFailureCode is null ? null : "last_execution_failed",
            Automation: snapshot);
    }

    private static string GetOverallStatus(
        IEnumerable<HealthComponentResult> components)
    {
        var componentList = components.ToList();

        if (componentList.Any(component =>
                component.Status == HealthStatuses.Unhealthy))
        {
            return HealthStatuses.Unhealthy;
        }

        return componentList.Any(component =>
                component.Status == HealthStatuses.Degraded)
            ? HealthStatuses.Degraded
            : HealthStatuses.Healthy;
    }

    private void LogStatusChange(ReadinessHealthResponse result)
    {
        lock (_statusSync)
        {
            if (string.Equals(_lastReportedStatus, result.Status, StringComparison.Ordinal))
            {
                return;
            }

            _lastReportedStatus = result.Status;
        }

        if (result.Status == HealthStatuses.Unhealthy)
        {
            logger.LogWarning(
                "RHID readiness changed. Status={Status}",
                result.Status);
            return;
        }

        logger.LogInformation(
            "RHID readiness changed. Status={Status}",
            result.Status);
    }
}

public static class HttpClientNames
{
    public const string RhidAvailability = "rhid-availability";
}
