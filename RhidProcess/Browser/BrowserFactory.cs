using PuppeteerSharp;
using RhidProcess.Abstractions;

namespace RhidProcess.Browser;

public class BrowserFactory : IBrowserFactory
{
    public async Task<IBrowser> CreateBrowserAsync()
    {
        var browserFetcher = new BrowserFetcher();
        await browserFetcher.DownloadAsync();
        
        return await Puppeteer.LaunchAsync(
            new LaunchOptions
            {
                Headless = false,
                Args = 
                [
                    "--no-sandbox",
                    "--disable-dev-shm-usage",
                    "--disable-gpu"
                ]
            });
    }
    
}