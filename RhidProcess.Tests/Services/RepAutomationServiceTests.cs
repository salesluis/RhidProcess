using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using PuppeteerSharp;
using RhidProcess.Abstractions;
using RhidProcess.Diagnostics;
using RhidProcess.Health;
using RhidProcess.Models;
using RhidProcess.Options;
using RhidProcess.Services;

namespace RhidProcess.Tests.Services;

public sealed class RepAutomationServiceTests
{
    [Fact]
    public async Task ExecuteAsync_BlocksAutomationWhenLocalHealthIsInvalid()
    {
        var options = Microsoft.Extensions.Options.Options.Create(
            new RhidOptions
            {
                Email = string.Empty,
                Password = string.Empty
            });
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["PUPPETEER_EXECUTABLE_PATH"] =
                    Path.Combine(Path.GetTempPath(), $"missing-{Guid.NewGuid():N}")
            })
            .Build();
        var factory = new TrackingBrowserFactory();
        var health = new RhidHealthService(options, configuration);
        var service = new RepAutomationService(
            factory,
            options,
            health,
            NullLoggerFactory.Instance,
            NullLogger<RepAutomationService>.Instance);

        var exception = await Assert.ThrowsAsync<RhidAutomationException>(
            () => service.ExecuteAsync(
                new UnlockRequest("serial", "password"),
                CancellationToken.None));

        Assert.Equal(AutomationErrorCodes.ConfigurationInvalid, exception.Code);
        Assert.Equal(AutomationStages.Configuration, exception.Stage);
        Assert.Equal(503, exception.StatusCode);
        Assert.False(factory.WasCalled);
    }

    private sealed class TrackingBrowserFactory : IBrowserFactory
    {
        public bool WasCalled { get; private set; }

        public Task<IBrowser> CreateBrowserAsync(
            CancellationToken cancellationToken = default)
        {
            WasCalled = true;
            throw new InvalidOperationException("Browser must not be created.");
        }
    }
}
