using PuppeteerSharp;
using RhidProcess.Diagnostics;
using RhidProcess.Options;

namespace RhidProcess.Browser;

public sealed class UnlockRepPage(
    IPage page,
    RhidOptions options,
    ILogger<UnlockRepPage> logger)
{
    private const string SerialSelector = "input[placeholder='Serial']";
    private const string PasswordSelector = "input[placeholder='Senha']";
    private const string ButtonSelector = "#btnSave";
    private const string ResultSelector = ".form-control.ng-binding.ng-scope";

    private readonly AutomationStepRunner _runner = new(logger);

    public Task OpenAsync(CancellationToken cancellationToken = default)
    {
        return _runner.RunAsync(
            AutomationStages.UnlockPage,
            async () =>
            {
                var response = await page
                    .GoToAsync(
                        BuildUrl(options.UnlockRoute),
                        NavigationOptions())
                    .WaitAsync(cancellationToken);

                EnsureSuccessfulResponse(response);

                await page
                    .WaitForSelectorAsync("[id^=\"n_\"]")
                    .WaitAsync(cancellationToken);
            },
            cancellationToken);
    }

    public Task FillAsync(
        string serial,
        string password,
        CancellationToken cancellationToken = default)
    {
        return _runner.RunAsync(
            AutomationStages.UnlockSubmit,
            async () =>
            {
                try
                {
                    await page.Locator(SerialSelector)
                        .FillAsync(serial)
                        .WaitAsync(cancellationToken);

                    await page.Locator(PasswordSelector)
                        .FillAsync(password)
                        .WaitAsync(cancellationToken);

                    await page.Locator(ButtonSelector)
                        .ClickAsync()
                        .WaitAsync(cancellationToken);
                }
                catch (OperationCanceledException)
                    when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch
                {
                    await FillWithAngularFallbackAsync(
                        serial,
                        password,
                        cancellationToken);
                }
            },
            cancellationToken);
    }

    public Task<string> GetContraSenhaAsync(
        CancellationToken cancellationToken = default)
    {
        return _runner.RunAsync(
            AutomationStages.ResultRead,
            async () =>
            {
                await page
                    .WaitForSelectorAsync(ResultSelector)
                    .WaitAsync(cancellationToken);

                var result = await page
                    .EvaluateExpressionAsync<string>(
                        $"document.querySelector('{ResultSelector}').innerText")
                    .WaitAsync(cancellationToken);

                if (string.IsNullOrWhiteSpace(result))
                {
                    throw new InvalidOperationException(
                        "The RHID result was empty.");
                }

                return result;
            },
            cancellationToken);
    }

    private async Task FillWithAngularFallbackAsync(
        string serial,
        string password,
        CancellationToken cancellationToken)
    {
        await page.EvaluateFunctionAsync(
                @"(serialValue, senhaValue) => {
                    const inputSerial =
                        document.querySelector('input[placeholder=""Serial""]');
                    const inputSenha =
                        document.querySelector('input[placeholder=""Senha""]');
                    const btn = document.getElementById('btnSave');

                    if (!inputSerial || !inputSenha || !btn)
                        throw new Error('Required fields were not found.');

                    inputSerial.value = serialValue;
                    inputSerial.dispatchEvent(
                        new Event('input', { bubbles: true })
                    );

                    inputSenha.value = senhaValue;
                    inputSenha.dispatchEvent(
                        new Event('input', { bubbles: true })
                    );

                    btn.click();
                }",
                serial,
                password)
            .WaitAsync(cancellationToken);
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

    private static void EnsureSuccessfulResponse(IResponse? response)
    {
        if (response is not null
            && (int)response.Status >= StatusCodes.Status400BadRequest)
        {
            throw new RhidAutomationException(
                AutomationErrorCodes.UpstreamFailure,
                AutomationStages.UnlockPage,
                "O RHID retornou uma resposta inesperada.",
                StatusCodes.Status502BadGateway);
        }
    }

    private static int ToMilliseconds(int seconds)
    {
        return Math.Clamp(seconds, 1, int.MaxValue / 1000) * 1000;
    }
}
