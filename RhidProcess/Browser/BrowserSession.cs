using PuppeteerSharp;
using RhidProcess.Abstractions;
using RhidProcess.Diagnostics;
using RhidProcess.Options;

namespace RhidProcess.Browser;

public sealed class BrowserSession : IAsyncDisposable
{
    private readonly IBrowser _browser;
    private readonly SafePageDiagnostics _diagnostics;
    private readonly ILogger<BrowserSession> _logger;

    public IPage Page { get; }
    public LoginPage Login { get; }
    public UnlockRepPage Unlock { get; }

    private BrowserSession(
        IBrowser browser,
        IPage page,
        RhidOptions options,
        ILoggerFactory loggerFactory,
        SafePageDiagnostics diagnostics)
    {
        _browser = browser;
        _diagnostics = diagnostics;
        _logger = loggerFactory.CreateLogger<BrowserSession>();
        Page = page;

        Login = new LoginPage(
            page,
            options,
            loggerFactory.CreateLogger<LoginPage>());
        Unlock = new UnlockRepPage(
            page,
            options,
            loggerFactory.CreateLogger<UnlockRepPage>());
    }

    public static async Task<BrowserSession> CreateAsync(
        IBrowserFactory factory,
        RhidOptions options,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken = default)
    {
        var logger = loggerFactory.CreateLogger<BrowserSession>();
        var runner = new AutomationStepRunner(logger);
        IBrowser? browser = null;

        try
        {
            var page = await runner.RunAsync(
                AutomationStages.BrowserStartup,
                async () =>
                {
                    browser = await factory.CreateBrowserAsync(cancellationToken);
                    var createdPage = await browser
                        .NewPageAsync()
                        .WaitAsync(cancellationToken);

                    createdPage.DefaultTimeout =
                        ToMilliseconds(options.ActionTimeoutSeconds);
                    createdPage.DefaultNavigationTimeout =
                        ToMilliseconds(options.NavigationTimeoutSeconds);

                    await createdPage
                        .SetViewportAsync(new ViewPortOptions
                        {
                            Width = 1920,
                            Height = 920
                        })
                        .WaitAsync(cancellationToken);

                    await ConfigurePerformance(
                        createdPage,
                        logger,
                        cancellationToken);

                    return createdPage;
                },
                cancellationToken);

            var diagnostics = new SafePageDiagnostics(page, logger);
            return new BrowserSession(
                browser!,
                page,
                options,
                loggerFactory,
                diagnostics);
        }
        catch
        {
            if (browser is not null)
            {
                await CloseBrowserSafelyAsync(browser, logger);
            }

            throw;
        }
    }

    private static async Task ConfigurePerformance(
        IPage page,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        await page
            .SetRequestInterceptionAsync(true)
            .WaitAsync(cancellationToken);

        page.Request += async (_, e) =>
        {
            try
            {
                if (IsBlockedResource(e.Request.ResourceType))
                {
                    await e.Request.AbortAsync();
                }
                else
                {
                    await e.Request.ContinueAsync();
                }
            }
            catch (Exception exception)
            {
                logger.LogDebug(
                    "RHID request interception failed. ExceptionType={ExceptionType} Url={Url}",
                    exception.GetType().Name,
                    SafePageDiagnostics.SanitizeUrl(e.Request.Url));
            }
        };
    }

    private static bool IsBlockedResource(ResourceType resourceType)
    {
        return resourceType is
            ResourceType.Image or
            ResourceType.Font or
            ResourceType.Media or
            ResourceType.StyleSheet;
    }

    private static int ToMilliseconds(int seconds)
    {
        return Math.Clamp(seconds, 1, int.MaxValue / 1000) * 1000;
    }

    public async ValueTask DisposeAsync()
    {
        _diagnostics.Detach();
        await CloseBrowserSafelyAsync(_browser, _logger);
    }

    private static async Task CloseBrowserSafelyAsync(
        IBrowser browser,
        ILogger logger)
    {
        try
        {
            await browser.CloseAsync();
        }
        catch (Exception exception)
        {
            logger.LogWarning(
                "RHID browser cleanup failed. ExceptionType={ExceptionType}",
                exception.GetType().Name);
        }
    }
}
