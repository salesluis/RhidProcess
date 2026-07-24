using PuppeteerSharp;
using RhidProcess.Abstractions;

namespace RhidProcess.Browser;

public class BrowserFactory(IConfiguration configuration) : IBrowserFactory
{
    public async Task<IBrowser> CreateBrowserAsync(CancellationToken cancellationToken = default)
    {
        var executablePath = configuration["PUPPETEER_EXECUTABLE_PATH"];
        if (string.IsNullOrWhiteSpace(executablePath))
        {
            executablePath = "/usr/bin/google-chrome-stable";
        }
        string[] args = ["--no-sandbox", "--disable-dev-shm-usage", "--disable-gpu"];

        var launchTask = Puppeteer.LaunchAsync(
            new LaunchOptions
            {
                Headless = true,
                ExecutablePath = executablePath,
                Args = args
            });

        try
        {
            return await launchTask.WaitAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _ = CloseBrowserWhenLaunchCompletesAsync(launchTask);
            throw;
        }
    }

    private static async Task CloseBrowserWhenLaunchCompletesAsync(Task<IBrowser> launchTask)
    {
        try
        {
            var browser = await launchTask;
            await browser.CloseAsync();
        }
        catch
        {
            // The original launch operation is already being reported by the request pipeline.
        }
    }
}
