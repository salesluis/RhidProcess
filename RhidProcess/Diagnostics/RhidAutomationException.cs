namespace RhidProcess.Diagnostics;

public sealed class RhidAutomationException(
    string code,
    string stage,
    string publicMessage,
    int statusCode,
    Exception? innerException = null)
    : Exception(publicMessage, innerException)
{
    public string Code { get; } = code;
    public string Stage { get; } = stage;
    public string PublicMessage { get; } = publicMessage;
    public int StatusCode { get; } = statusCode;
}
