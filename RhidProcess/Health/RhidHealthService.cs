using Microsoft.Extensions.Options;
using RhidProcess.Options;

namespace RhidProcess.Health;

public sealed class RhidHealthService(
    IOptions<RhidOptions> options,
    IConfiguration configuration)
{
    private const string DefaultChromeExecutablePath = "/usr/bin/google-chrome-stable";

    public bool IsHealthy()
    {
        var rhid = options.Value;
        var executablePath = configuration["PUPPETEER_EXECUTABLE_PATH"];

        if (string.IsNullOrWhiteSpace(executablePath))
            executablePath = DefaultChromeExecutablePath;

        return HasValidRhidConfiguration(rhid)
            && File.Exists(executablePath);
    }

    public static bool HasValidRhidConfiguration(RhidOptions options)
    {
        return options.HasRequiredCredentials
            && Uri.TryCreate(options.BaseUrl, UriKind.Absolute, out var baseUri)
            && (baseUri.Scheme == Uri.UriSchemeHttp || baseUri.Scheme == Uri.UriSchemeHttps)
            && !string.IsNullOrWhiteSpace(options.LoginRoute)
            && !string.IsNullOrWhiteSpace(options.UnlockRoute)
            && options.NavigationTimeoutSeconds > 0
            && options.ActionTimeoutSeconds > 0;
    }
}
