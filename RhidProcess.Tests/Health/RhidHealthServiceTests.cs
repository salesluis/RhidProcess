using System.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using RhidProcess.Browser;
using RhidProcess.Health;
using RhidProcess.Monitoring;

namespace RhidProcess.Tests.Health;

public sealed class RhidHealthServiceTests : IDisposable
{
    private readonly string _contentRoot = Path.Combine(
        Path.GetTempPath(),
        $"rhid-health-{Guid.NewGuid():N}");

    [Fact]
    public async Task GetReadinessAsync_ReturnsHealthyWhenAllCriticalComponentsAreAvailable()
    {
        var chromePath = CreateChromePlaceholder();
        var service = CreateService(
            chromePath,
            HttpStatusCode.OK,
            new AutomationTelemetry());

        var result = await service.GetReadinessAsync(CancellationToken.None);

        Assert.Equal(HealthStatuses.Healthy, result.Status);
        Assert.Equal(HealthStatuses.Healthy, result.Components["configuration"].Status);
        Assert.Equal(HealthStatuses.Healthy, result.Components["chrome"].Status);
        Assert.Equal(HealthStatuses.Healthy, result.Components["logs"].Status);
        Assert.Equal(HealthStatuses.Healthy, result.Components["rhid"].Status);
    }

    [Fact]
    public async Task GetReadinessAsync_ReturnsUnhealthyWhenRhidReturnsAnError()
    {
        var service = CreateService(
            CreateChromePlaceholder(),
            HttpStatusCode.BadGateway,
            new AutomationTelemetry());

        var result = await service.GetReadinessAsync(CancellationToken.None);

        Assert.Equal(HealthStatuses.Unhealthy, result.Status);
        Assert.Equal(HealthStatuses.Unhealthy, result.Components["rhid"].Status);
        Assert.Equal(502, result.Components["rhid"].HttpStatus);
    }

    [Fact]
    public async Task GetReadinessAsync_ReturnsDegradedAfterTheLatestAutomationFailure()
    {
        var telemetry = new AutomationTelemetry();
        telemetry.RecordStarted();
        telemetry.RecordFailure("AUTOMATION_TIMEOUT", "request", 123);

        var service = CreateService(
            CreateChromePlaceholder(),
            HttpStatusCode.OK,
            telemetry);

        var result = await service.GetReadinessAsync(CancellationToken.None);

        Assert.Equal(HealthStatuses.Degraded, result.Status);
        Assert.Equal(HealthStatuses.Degraded, result.Components["automation"].Status);
        Assert.Equal(
            "AUTOMATION_TIMEOUT",
            result.Components["automation"].Automation!.LastFailureCode);
    }

    public void Dispose()
    {
        if (Directory.Exists(_contentRoot))
        {
            Directory.Delete(_contentRoot, recursive: true);
        }
    }

    private RhidHealthService CreateService(
        string chromePath,
        HttpStatusCode responseStatus,
        AutomationTelemetry telemetry)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(
            [
                new KeyValuePair<string, string?>(
                    "PUPPETEER_EXECUTABLE_PATH",
                    chromePath)
            ])
            .Build();

        return new RhidHealthService(
            Options.Create(new RhidOptions
            {
                Email = "health@example.com",
                Password = "configured-for-test"
            }),
            new BrowserRuntimeSettings(configuration),
            new TestHostEnvironment(_contentRoot),
            new TestHttpClientFactory(responseStatus),
            telemetry,
            NullLogger<RhidHealthService>.Instance);
    }

    private string CreateChromePlaceholder()
    {
        Directory.CreateDirectory(_contentRoot);
        var path = Path.Combine(_contentRoot, "chrome");
        File.WriteAllText(path, string.Empty);
        return path;
    }

    private sealed class TestHttpClientFactory(HttpStatusCode statusCode) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name)
        {
            return new HttpClient(new TestHttpMessageHandler(statusCode));
        }
    }

    private sealed class TestHttpMessageHandler(HttpStatusCode statusCode) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(new HttpResponseMessage(statusCode));
        }
    }

    private sealed class TestHostEnvironment(string contentRootPath) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Development;
        public string ApplicationName { get; set; } = "RhidProcess.Tests";
        public string ContentRootPath { get; set; } = contentRootPath;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
