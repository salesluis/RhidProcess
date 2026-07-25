using PuppeteerSharp;
using RhidProcess.Abstractions;

namespace RhidProcess.Browser;

public class BrowserFactory : IBrowserFactory
{
    public async Task<IBrowser> CreateBrowserAsync()
    {
        var executablePath =  "/usr/bin/google-chrome-stable";
        var headless = true;
        var args =  new[] { "--no-sandbox", "--disable-dev-shm-usage", "--disable-gpu" };

        return await Puppeteer.LaunchAsync(
            new LaunchOptions
            {
                Headless = headless,
                ExecutablePath = executablePath,
                Args = args
            });
    }
    
}