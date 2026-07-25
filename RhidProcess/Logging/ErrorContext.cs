namespace RhidProcess.Logging;

public static class ErrorContext
{
    private static readonly object MetadataKey = new();

    public static string GetErrorId(HttpContext context)
    {
        if (!string.IsNullOrWhiteSpace(context.TraceIdentifier))
            return context.TraceIdentifier;

        context.TraceIdentifier = Guid.NewGuid().ToString("N");
        return context.TraceIdentifier;
    }

    public static void Set(
        HttpContext context,
        string code,
        string stage,
        string? errorId = null)
    {
        context.Items[MetadataKey] = new ErrorMetadata(
            errorId ?? GetErrorId(context),
            code,
            stage);
    }

    public static ErrorMetadata? Get(HttpContext context)
    {
        return context.Items.TryGetValue(MetadataKey, out var value)
            ? value as ErrorMetadata
            : null;
    }
}

public sealed record ErrorMetadata(string ErrorId, string Code, string Stage);
