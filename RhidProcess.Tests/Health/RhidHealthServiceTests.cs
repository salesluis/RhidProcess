using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using RhidProcess.Health;
using RhidProcess.Options;

namespace RhidProcess.Tests.Health;

public sealed class RhidHealthServiceTests
{
    [Fact]
    public void IsHealthy_ReturnsTrueForValidConfigurationAndExistingChrome()
    {
        var executablePath = Path.Combine(
            Path.GetTempPath(),
            $"rhid-test-chrome-{Guid.NewGuid():N}");

        try
        {
            File.WriteAllText(executablePath, string.Empty);

            var service = CreateService(ValidOptions(), executablePath);

            Assert.True(service.IsHealthy());
        }
        finally
        {
            File.Delete(executablePath);
        }
    }

    [Fact]
    public void IsHealthy_ReturnsFalseWhenChromeDoesNotExist()
    {
        var missingExecutable = Path.Combine(
            Path.GetTempPath(),
            $"rhid-missing-chrome-{Guid.NewGuid():N}");

        var service = CreateService(ValidOptions(), missingExecutable);

        Assert.False(service.IsHealthy());
    }

    [Fact]
    public void IsHealthy_ReturnsFalseWhenCredentialsAreMissing()
    {
        var options = new RhidOptions
        {
            BaseUrl = "https://example.invalid",
            LoginRoute = "/v2/#/login",
            UnlockRoute = "/v2/#/unlock",
            Email = string.Empty,
            Password = string.Empty,
            NavigationTimeoutSeconds = 30,
            ActionTimeoutSeconds = 30
        };

        var service = CreateService(options, typeof(object).Assembly.Location);

        Assert.False(service.IsHealthy());
    }

    [Theory]
    [InlineData("", "/v2/#/login", "/v2/#/unlock", 30, 30)]
    [InlineData("not-an-absolute-url", "/v2/#/login", "/v2/#/unlock", 30, 30)]
    [InlineData("ftp://example.invalid", "/v2/#/login", "/v2/#/unlock", 30, 30)]
    [InlineData("https://example.invalid", "", "/v2/#/unlock", 30, 30)]
    [InlineData("https://example.invalid", "/v2/#/login", "", 30, 30)]
    [InlineData("https://example.invalid", "/v2/#/login", "/v2/#/unlock", 0, 30)]
    [InlineData("https://example.invalid", "/v2/#/login", "/v2/#/unlock", 30, 0)]
    public void HasValidRhidConfiguration_RejectsInvalidValues(
        string baseUrl,
        string loginRoute,
        string unlockRoute,
        int navigationTimeout,
        int actionTimeout)
    {
        var options = new RhidOptions
        {
            BaseUrl = baseUrl,
            LoginRoute = loginRoute,
            UnlockRoute = unlockRoute,
            Email = "service-account@example.invalid",
            Password = "configured-at-runtime",
            NavigationTimeoutSeconds = navigationTimeout,
            ActionTimeoutSeconds = actionTimeout
        };

        Assert.False(RhidHealthService.HasValidRhidConfiguration(options));
    }

    private static RhidHealthService CreateService(
        RhidOptions options,
        string executablePath)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["PUPPETEER_EXECUTABLE_PATH"] = executablePath
            })
            .Build();

        return new RhidHealthService(
            Microsoft.Extensions.Options.Options.Create(options),
            configuration);
    }

    private static RhidOptions ValidOptions()
    {
        return new RhidOptions
        {
            BaseUrl = "https://example.invalid",
            LoginRoute = "/v2/#/login",
            UnlockRoute = "/v2/#/unlock",
            Email = "service-account@example.invalid",
            Password = "configured-at-runtime",
            NavigationTimeoutSeconds = 30,
            ActionTimeoutSeconds = 30
        };
    }
}
