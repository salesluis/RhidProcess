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
                //todo: mudar para true para nao abrir janela do browser
                Headless = false,
                SlowMo = 100,
                Args = 
                [
                    "--no-sandbox",
                    "--disable-dev-shm-usage",
                    "--disable-gpu"
                ]
            });
    }
    
}