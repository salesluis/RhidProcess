using PuppeteerSharp;
using RhidProcess.Abstractions;

namespace RhidProcess.Browser;

// herda de IAsyncDisposable para que eu possa usar Using
public class BrowserSession : IAsyncDisposable
{
    private readonly IBrowser _browser;
    public IPage Page { get; }
    public LoginPage Login { get; }
    public UnlockRepPage Unlock { get; }

    private BrowserSession(IBrowser browser, IPage page, RhidOptions options)
    {
        _browser = browser;
        Page = page;

        Login = new LoginPage(page, options);
        Unlock = new UnlockRepPage(page, options);
    }

    public static async Task<BrowserSession> CreateAsync(
        IBrowserFactory factory,
        RhidOptions options)
    {
        var browser = await factory.CreateBrowserAsync();
        var page = await browser.NewPageAsync();

        page.DefaultTimeout = options.DefaultTimeout;
        page.DefaultNavigationTimeout = options.DefaultNavigationTimeout;
        
        // todo: excluir ao finalizar aplicação
        await page.SetViewportAsync(new ViewPortOptions
        {
            Width = 1920,
            Height = 920
        });

        await ConfigurePerformance(page);

        return new BrowserSession(browser, page, options);
    }

    private static async Task ConfigurePerformance(IPage page)
    {
        await page.SetRequestInterceptionAsync(true);

        page.Request += async (_, e) =>
        {
            switch (e.Request.ResourceType)
            {
                case ResourceType.Image:
                case ResourceType.Font:
                case ResourceType.Media:
                case ResourceType.StyleSheet:

                    await e.Request.AbortAsync();
                    break;

                default:

                    await e.Request.ContinueAsync();
                    break;
            }
        };
    }

    public async ValueTask DisposeAsync()
    {
        await _browser.CloseAsync();
    }
}