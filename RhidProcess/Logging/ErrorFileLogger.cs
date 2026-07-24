using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace RhidProcess.Logging;

public sealed class ErrorFileLogger(
    IHostEnvironment environment,
    ILogger<ErrorFileLogger> fallbackLogger)
{
    private readonly string _logsDirectory = Path.Combine(environment.ContentRootPath, "Logs");

    public Task LogAsync(
        Exception exception,
        HttpContext context,
        string errorId,
        string code,
        string stage)
    {
        return WriteAsync(exception, context, errorId, code, stage);
    }

    public Task LogResponseAsync(
        HttpContext context,
        string errorId,
        string code,
        string stage)
    {
        return WriteAsync(null, context, errorId, code, stage);
    }

    public static string SanitizeUrl(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        var queryIndex = value.IndexOf('?', StringComparison.Ordinal);
        var fragmentIndex = value.IndexOf('#', StringComparison.Ordinal);
        var endIndex = new[] { queryIndex, fragmentIndex }
            .Where(index => index >= 0)
            .DefaultIfEmpty(value.Length)
            .Min();

        return value[..endIndex];
    }

    public static string? SanitizeStackTrace(Exception? exception)
    {
        if (string.IsNullOrWhiteSpace(exception?.StackTrace))
            return null;

        return Regex.Replace(
            exception.StackTrace,
            @"(?i)(https?://[^\s?#]+)(?:[?#][^\s]*)?",
            "$1",
            RegexOptions.CultureInvariant);
    }

    private async Task WriteAsync(
        Exception? exception,
        HttpContext context,
        string errorId,
        string code,
        string stage)
    {
        try
        {
            Directory.CreateDirectory(_logsDirectory);

            var fileName = $"{DateTime.UtcNow:yyyy-MM-dd_HH-mm-ss-fff}_{Guid.NewGuid():N}.json";
            var filePath = Path.Combine(_logsDirectory, fileName);
            var content = BuildLogContent(exception, context, errorId, code, stage);

            await File.WriteAllTextAsync(filePath, content, Encoding.UTF8, CancellationToken.None);
        }
        catch (Exception loggingException)
        {
            fallbackLogger.LogError(
                "Não foi possível gravar o log de erro. ErrorId: {ErrorId}; " +
                "LoggingExceptionType: {LoggingExceptionType}",
                errorId,
                loggingException.GetType().FullName);
        }
    }

    private static string BuildLogContent(
        Exception? exception,
        HttpContext context,
        string errorId,
        string code,
        string stage)
    {
        var upstreamStatus = FindHttpStatusCode(exception);
        var entry = new
        {
            timestampUtc = DateTimeOffset.UtcNow,
            errorId,
            code,
            stage,
            exceptionType = exception?.GetType().FullName,
            innerExceptionType = FindInnermostExceptionType(exception),
            upstreamStatusCode = upstreamStatus,
            method = context.Request.Method,
            path = SanitizeUrl(context.Request.Path.Value),
            responseStatusCode = context.Response.StatusCode,
            stackTrace = SanitizeStackTrace(exception)
        };

        return JsonSerializer.Serialize(
            entry,
            new JsonSerializerOptions { WriteIndented = true });
    }

    private static int? FindHttpStatusCode(Exception? exception)
    {
        while (exception is not null)
        {
            if (exception is HttpRequestException { StatusCode: not null } httpException)
                return (int)httpException.StatusCode.Value;

            exception = exception.InnerException;
        }

        return null;
    }

    private static string? FindInnermostExceptionType(Exception? exception)
    {
        if (exception is null)
            return null;

        while (exception.InnerException is not null)
            exception = exception.InnerException;

        return exception.GetType().FullName;
    }
}
