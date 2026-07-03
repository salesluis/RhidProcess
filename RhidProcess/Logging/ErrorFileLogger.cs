using System.Text;

namespace RhidProcess.Logging;

public sealed class ErrorFileLogger(IHostEnvironment environment)
{
    private readonly string _logsDirectory = Path.Combine(environment.ContentRootPath, "Logs");

    public async Task LogAsync(Exception exception, HttpContext? context = null, CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(_logsDirectory);

        var fileName = $"{DateTime.Now:yyyy-MM-dd_HH-mm-ss-fff}_{Guid.NewGuid().Substring(0, 12)}:N}.log";
        var filePath = Path.Combine(_logsDirectory, fileName);
        var content = BuildLogContent(exception, context);

        await File.WriteAllTextAsync(filePath, content, Encoding.UTF8, cancellationToken);
    }

    private static string BuildLogContent(Exception exception, HttpContext? context)
    {
        var builder = new StringBuilder();

        builder.AppendLine("================================================================================");
        builder.AppendLine("INFORMAÇÕES DO ERRO");
        builder.AppendLine("================================================================================");
        builder.AppendLine($"Data/Hora: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        builder.AppendLine($"Tipo: {exception.GetType().FullName}");
        builder.AppendLine($"Mensagem: {exception.Message}");

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

        var inner = exception.InnerException;
        while (inner is not null)
        {
            builder.AppendLine($"Exceção Interna ({inner.GetType().FullName}): {inner.Message}");
            inner = inner.InnerException;
        }

        builder.AppendLine();
        builder.AppendLine("================================================================================");
        builder.AppendLine("STACKTRACE");
        builder.AppendLine("================================================================================");
        builder.AppendLine(exception.ToString());

        return builder.ToString();
    }
}
