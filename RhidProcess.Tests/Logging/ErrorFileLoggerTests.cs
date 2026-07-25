using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using RhidProcess.Logging;

namespace RhidProcess.Tests.Logging;

public sealed class ErrorFileLoggerTests
{
    [Theory]
    [InlineData(null, "")]
    [InlineData("", "")]
    [InlineData("https://example.invalid/path", "https://example.invalid/path")]
    [InlineData(
        "https://example.invalid/path?serial=123&password=secret",
        "https://example.invalid/path")]
    [InlineData(
        "https://example.invalid/path#fragment?password=secret",
        "https://example.invalid/path")]
    public void SanitizeUrl_RemovesQueryAndFragment(string? input, string expected)
    {
        Assert.Equal(expected, ErrorFileLogger.SanitizeUrl(input));
    }

    [Fact]
    public async Task LogAsync_WritesCorrelatedEntryWithoutRequestSecrets()
    {
        var contentRoot = Path.Combine(
            Path.GetTempPath(),
            $"rhid-logger-tests-{Guid.NewGuid():N}");

        try
        {
            var logger = new ErrorFileLogger(
                new TestHostEnvironment(contentRoot),
                NullLogger<ErrorFileLogger>.Instance);
            var context = new DefaultHttpContext();
            context.Request.Method = HttpMethods.Get;
            context.Request.Path = "/v2/rhid/unlock";
            context.Request.QueryString =
                new QueryString("?serial=123456789&password=device-secret");
            context.Request.Headers["X-Api-Key"] = "api-key-secret";
            context.Response.StatusCode = StatusCodes.Status504GatewayTimeout;

            await logger.LogAsync(
                new InvalidOperationException(
                    "email@example.invalid portal-password device-secret"),
                context,
                "error-123",
                "RHID_UPSTREAM_TIMEOUT",
                "login_submit");

            var logPath = Assert.Single(
                Directory.EnumerateFiles(
                    Path.Combine(contentRoot, "Logs"),
                    "*.json"));
            var content = await File.ReadAllTextAsync(logPath);

            Assert.DoesNotContain("123456789", content, StringComparison.Ordinal);
            Assert.DoesNotContain("device-secret", content, StringComparison.Ordinal);
            Assert.DoesNotContain("api-key-secret", content, StringComparison.Ordinal);
            Assert.DoesNotContain("email@example.invalid", content, StringComparison.Ordinal);
            Assert.DoesNotContain("portal-password", content, StringComparison.Ordinal);

            using var document = JsonDocument.Parse(content);
            var root = document.RootElement;
            Assert.Equal("error-123", root.GetProperty("errorId").GetString());
            Assert.Equal("RHID_UPSTREAM_TIMEOUT", root.GetProperty("code").GetString());
            Assert.Equal("login_submit", root.GetProperty("stage").GetString());
            Assert.Equal("/v2/rhid/unlock", root.GetProperty("path").GetString());
            Assert.Equal(504, root.GetProperty("responseStatusCode").GetInt32());
        }
        finally
        {
            if (Directory.Exists(contentRoot))
                Directory.Delete(contentRoot, recursive: true);
        }
    }

    private sealed class TestHostEnvironment(string contentRootPath) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Development;
        public string ApplicationName { get; set; } = "RhidProcess.Tests";
        public string ContentRootPath { get; set; } = contentRootPath;
        public IFileProvider ContentRootFileProvider { get; set; } =
            new NullFileProvider();
    }
}
