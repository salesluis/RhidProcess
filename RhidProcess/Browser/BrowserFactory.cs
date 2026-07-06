using PuppeteerSharp;
using RhidProcess.Abstractions;

namespace RhidProcess.Browser;

public class BrowserFactory : IBrowserFactory
{
    public async Task<IBrowser> CreateBrowserAsync()
    {
        var executablePath = Environment.GetEnvironmentVariable("PUPPETEER_EXECUTABLE_PATH");

        if (string.IsNullOrEmpty(executablePath))
        {
            var fetcher = new BrowserFetcher();
            await fetcher.DownloadAsync();
        }
        
        return await Puppeteer.LaunchAsync(
            new LaunchOptions
            {
                //todo: mudar para true para nao abrir janela do browser
                Headless = false,
                SlowMo = 100,
                ExecutablePath = executablePath,
                Args = 
                [
                    "--no-sandbox",
                    "--disable-dev-shm-usage",
                    "--disable-gpu"
                ]
            });
    }
    
}