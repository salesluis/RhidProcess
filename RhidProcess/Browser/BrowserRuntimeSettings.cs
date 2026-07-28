namespace RhidProcess.Browser;

public sealed class BrowserRuntimeSettings(IConfiguration configuration)
{
    public const string DefaultExecutablePath = "/usr/bin/google-chrome-stable";

    public string ExecutablePath =>
        configuration["PUPPETEER_EXECUTABLE_PATH"]
        ?? DefaultExecutablePath;
}
