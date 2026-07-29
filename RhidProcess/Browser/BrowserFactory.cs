using PuppeteerSharp;
using RhidProcess.Abstractions;

namespace RhidProcess.Browser;

public class BrowserFactory(BrowserRuntimeSettings browserRuntime) : IBrowserFactory
{
    public async Task<IBrowser> CreateBrowserAsync()
    {
        var headless = true;
        var args =  new[] { "--no-sandbox", "--disable-dev-shm-usage", "--disable-gpu" };

        return await Puppeteer.LaunchAsync(
            new LaunchOptions
            {
                Headless = headless,
                ExecutablePath = browserRuntime.ExecutablePath,
                Args = args
            });
    }
    
}
