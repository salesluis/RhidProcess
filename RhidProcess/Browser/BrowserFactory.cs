using PuppeteerSharp;
using RhidProcess.Abstractions;

namespace RhidProcess.Browser;

public class BrowserFactory : IBrowserFactory
{
    public async Task<IBrowser> CreateBrowserAsync() =>
        await Puppeteer.LaunchAsync(
            new LaunchOptions
            {
                Headless = false,
                Args = 
                [
                    "run",
                    "com.google.Chrome",
                    "--no-sandbox",
                    "--disable-dev-shm-usage"
                ]
            });
    
}