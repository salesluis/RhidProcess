using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using RhidProcess.Diagnostics;
using RhidProcess.Logging;

namespace RhidProcess.Tests.Logging;

public sealed class ErrorLoggingMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_ReturnsSafeCorrelatedAutomationError()
    {
        var contentRoot = Path.Combine(
            Path.GetTempPath(),
            $"rhid-middleware-tests-{Guid.NewGuid():N}");

        try
        {
            var context = new DefaultHttpContext
            {
                TraceIdentifier = "error-456"
            };
            context.Response.Body = new MemoryStream();

            var fileLogger = new ErrorFileLogger(
                new TestHostEnvironment(contentRoot),
                NullLogger<ErrorFileLogger>.Instance);
            var middleware = new ErrorLoggingMiddleware(
                _ => throw new RhidAutomationException(
                    AutomationErrorCodes.UpstreamTimeout,
                    AutomationStages.LoginSubmit,
                    "O RHID não respondeu dentro do tempo esperado.",
                    StatusCodes.Status504GatewayTimeout,
                    new TimeoutException("sensitive upstream detail")),
                fileLogger,
                NullLogger<ErrorLoggingMiddleware>.Instance);

            await middleware.InvokeAsync(context);

            Assert.Equal(StatusCodes.Status504GatewayTimeout, context.Response.StatusCode);
            context.Response.Body.Position = 0;
            using var response = await JsonDocument.ParseAsync(context.Response.Body);
            var root = response.RootElement;
            Assert.Equal("error-456", root.GetProperty("errorId").GetString());
            Assert.Equal(
                AutomationErrorCodes.UpstreamTimeout,
                root.GetProperty("code").GetString());
            Assert.Equal(
                AutomationStages.LoginSubmit,
                root.GetProperty("stage").GetString());
            Assert.False(root.TryGetProperty("stackTrace", out _));

            var logPath = Assert.Single(
                Directory.EnumerateFiles(
                    Path.Combine(contentRoot, "Logs"),
                    "*.json"));
            using var log = JsonDocument.Parse(await File.ReadAllTextAsync(logPath));
            Assert.Equal(
                StatusCodes.Status504GatewayTimeout,
                log.RootElement.GetProperty("responseStatusCode").GetInt32());
            Assert.Equal(
                typeof(TimeoutException).FullName,
                log.RootElement.GetProperty("innerExceptionType").GetString());
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
