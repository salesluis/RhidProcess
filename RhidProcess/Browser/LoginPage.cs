using PuppeteerSharp;
using RhidProcess.Diagnostics;
using RhidProcess.Options;

namespace RhidProcess.Browser;

public sealed class LoginPage(
    IPage page,
    RhidOptions options,
    ILogger<LoginPage> logger)
{
    private const string EmailSelector = "#email";
    private const string PasswordSelector = "#password";
    private const string SubmitSelector = "#m_login_signin_submit";

    private readonly AutomationStepRunner _runner = new(logger);

    public async Task LoginAsync(CancellationToken cancellationToken = default)
    {
        await _runner.RunAsync(
            AutomationStages.LoginPage,
            async () =>
            {
                var response = await page
                    .GoToAsync(
                        BuildUrl(options.LoginRoute),
                        NavigationOptions())
                    .WaitAsync(cancellationToken);

                EnsureSuccessfulResponse(
                    response,
                    AutomationStages.LoginPage);

                await page
                    .Locator(EmailSelector)
                    .FillAsync(options.Email)
                    .WaitAsync(cancellationToken);

                await page
                    .Locator(PasswordSelector)
                    .FillAsync(options.Password)
                    .WaitAsync(cancellationToken);
            },
            cancellationToken);

        await _runner.RunAsync(
            AutomationStages.LoginSubmit,
            async () =>
            {
                // Register the navigation observer before clicking to avoid missing
                // a fast navigation fired by the submit action.
                try
                {
                    await NavigationCoordinator.WaitForNavigationAndActionAsync(
                        () => page.WaitForNavigationAsync(NavigationOptions()),
                        () => page.Locator(SubmitSelector).ClickAsync(),
                        cancellationToken);
                }
                catch (Exception exception)
                    when (IsNavigationTimeout(exception)
                          && IsLoginLocation(page.Url))
                {
                    throw LoginNotConfirmed(exception);
                }

                if (IsLoginLocation(page.Url))
                {
                    throw LoginNotConfirmed();
                }
            },
            cancellationToken);
    }

    private NavigationOptions NavigationOptions()
    {
        return new NavigationOptions
        {
            Timeout = ToMilliseconds(options.NavigationTimeoutSeconds),
            WaitUntil = [WaitUntilNavigation.Networkidle2]
        };
    }

    private string BuildUrl(string route)
    {
        return new Uri(new Uri(options.BaseUrl), route).AbsoluteUri;
    }

    private bool IsLoginLocation(string currentUrl)
    {
        if (!Uri.TryCreate(currentUrl, UriKind.Absolute, out var current)
            || !Uri.TryCreate(BuildUrl(options.LoginRoute), UriKind.Absolute, out var expected))
        {
            return true;
        }

        return string.Equals(
                   current.AbsolutePath.TrimEnd('/'),
                   expected.AbsolutePath.TrimEnd('/'),
                   StringComparison.OrdinalIgnoreCase)
               && string.Equals(
                   current.Fragment.TrimEnd('/'),
                   expected.Fragment.TrimEnd('/'),
                   StringComparison.OrdinalIgnoreCase);
    }

    private static RhidAutomationException LoginNotConfirmed(
        Exception? innerException = null)
    {
        return new RhidAutomationException(
            AutomationErrorCodes.LoginNotConfirmed,
            AutomationStages.LoginSubmit,
            "Não foi possível confirmar o login no RHID.",
            StatusCodes.Status502BadGateway,
            innerException);
    }

    private static bool IsNavigationTimeout(Exception exception)
    {
        return exception is WaitTaskTimeoutException or TimeoutException;
    }

    private static void EnsureSuccessfulResponse(
        IResponse? response,
        string stage)
    {
        if (response is not null
            && (int)response.Status >= StatusCodes.Status400BadRequest)
        {
            throw new RhidAutomationException(
                AutomationErrorCodes.UpstreamFailure,
                stage,
                "O RHID retornou uma resposta inesperada.",
                StatusCodes.Status502BadGateway);
        }
    }

    private static int ToMilliseconds(int seconds)
    {
        return Math.Clamp(seconds, 1, int.MaxValue / 1000) * 1000;
    }
}
