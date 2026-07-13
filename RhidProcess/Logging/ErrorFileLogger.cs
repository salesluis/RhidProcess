using System.Text;

namespace RhidProcess.Logging;

public sealed class ErrorFileLogger(
    IHostEnvironment environment,
    ILogger<ErrorFileLogger> fallbackLogger)
{
    private readonly string _logsDirectory = Path.Combine(environment.ContentRootPath, "Logs");

    public Task LogAsync(Exception exception, HttpContext? context = null)
    {
        return WriteAsync(exception, context);
    }

    public Task LogResponseAsync(HttpContext context)
    {
        return WriteAsync(null, context);
    }

    private async Task WriteAsync(Exception? exception, HttpContext? context)
    {
        try
        {
            Directory.CreateDirectory(_logsDirectory);

            var fileName = $"{DateTime.Now:yyyy-MM-dd_HH-mm-ss-fff}_{Guid.NewGuid():N}.log";
            var filePath = Path.Combine(_logsDirectory, fileName);
            var content = BuildLogContent(exception, context);

            await File.WriteAllTextAsync(filePath, content, Encoding.UTF8, CancellationToken.None);
        }
        catch (Exception loggingException)
        {
            fallbackLogger.LogError(loggingException, "Não foi possível gravar o log de erro em {LogsDirectory}", _logsDirectory);
        }
    }

    private static string BuildLogContent(Exception? exception, HttpContext? context)
    {
        var builder = new StringBuilder();

        builder.AppendLine("================================================================================");
        builder.AppendLine("INFORMAÇÕES DO ERRO");
        builder.AppendLine("================================================================================");
        builder.AppendLine($"Data/Hora: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        if (exception is not null)
        {
            builder.AppendLine($"Tipo: {exception.GetType().FullName}");
            builder.AppendLine($"Mensagem: {exception.Message}");
        }
        else
        {
            builder.AppendLine("Tipo: Resposta HTTP com erro");
            builder.AppendLine("Mensagem: A requisição terminou com status HTTP 5xx sem lançar uma exceção.");
        }

        if (exception is HttpRequestException httpException && httpException.StatusCode is not null)
            builder.AppendLine($"Status HTTP: {(int)httpException.StatusCode.Value} ({httpException.StatusCode})");

        if (context is not null)
        {
            builder.AppendLine($"Método HTTP: {context.Request.Method}");
            builder.AppendLine($"Caminho: {context.Request.Path}{context.Request.QueryString}");
            builder.AppendLine($"Scheme: {context.Request.Scheme}");
            builder.AppendLine($"Host: {context.Request.Host}");
            builder.AppendLine($"Status Code: {context.Response.StatusCode}");
            builder.AppendLine($"Remote IP: {context.Connection.RemoteIpAddress}");
        }

        var inner = exception?.InnerException;
        while (inner is not null)
        {
            builder.AppendLine($"Exceção Interna ({inner.GetType().FullName}): {inner.Message}");
            inner = inner.InnerException;
        }

        builder.AppendLine();
        builder.AppendLine("================================================================================");
        builder.AppendLine("STACKTRACE");
        builder.AppendLine("================================================================================");
        builder.AppendLine(exception?.ToString() ?? "Não há stack trace porque nenhuma exceção foi lançada.");

        return builder.ToString();
    }
}
