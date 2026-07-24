using PuppeteerSharp;

namespace RhidProcess.Browser;

internal sealed class SafePageDiagnostics
{
    private readonly IPage _page;
    private readonly ILogger _logger;

    public SafePageDiagnostics(IPage page, ILogger logger)
    {
        _page = page;
        _logger = logger;

        _page.PageError += OnPageError;
        _page.RequestFailed += OnRequestFailed;
        _page.Response += OnResponse;
    }

    public void Detach()
    {
        _page.PageError -= OnPageError;
        _page.RequestFailed -= OnRequestFailed;
        _page.Response -= OnResponse;
    }

    public static string SanitizeUrl(string? value)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri)
            || uri.Scheme is not ("http" or "https"))
        {
            return "unavailable";
        }

        return uri.GetLeftPart(UriPartial.Authority) + uri.AbsolutePath;
    }

    private void OnPageError(object? sender, PageErrorEventArgs eventArgs)
    {
        _logger.LogWarning(
            "RHID page JavaScript error observed. Url={Url}",
            SanitizeUrl(_page.Url));
    }

    private void OnRequestFailed(object? sender, RequestEventArgs eventArgs)
    {
        if (eventArgs.Request.ResourceType is
            ResourceType.Image or
            ResourceType.Font or
            ResourceType.Media or
            ResourceType.StyleSheet)
        {
            // These resources are intentionally blocked by BrowserSession.
            return;
        }

        _logger.LogWarning(
            "RHID request failed. Method={Method} ResourceType={ResourceType} Url={Url}",
            eventArgs.Request.Method,
            eventArgs.Request.ResourceType,
            SanitizeUrl(eventArgs.Request.Url));
    }

    private void OnResponse(object? sender, ResponseCreatedEventArgs eventArgs)
    {
        if ((int)eventArgs.Response.Status < StatusCodes.Status400BadRequest)
        {
            return;
        }

        _logger.LogWarning(
            "RHID HTTP error response observed. StatusCode={StatusCode} Url={Url}",
            (int)eventArgs.Response.Status,
            SanitizeUrl(eventArgs.Response.Url));
    }
}
