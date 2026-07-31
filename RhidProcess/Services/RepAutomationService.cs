using System.Diagnostics;
using Microsoft.Extensions.Options;
using PuppeteerSharp;
using RhidProcess.Abstractions;
using RhidProcess.Browser;
using RhidProcess.Models;
using RhidProcess.Monitoring;

namespace RhidProcess.Services;

public sealed class RepAutomationService(
    IBrowserFactory browserFactory,
    IOptions<RhidOptions> options,
    AutomationTelemetry automationTelemetry)
{
    public async Task<UnlockResponse> ExecuteAsync(UnlockRequest request, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        automationTelemetry.RecordStarted();

        try
        {
            if(string.IsNullOrWhiteSpace(request.Serial) || string.IsNullOrWhiteSpace(request.Password))
            {
                return new UnlockResponse("Os campos 'serial' e 'senha' são obrigatórios.");
            }

            await using var session =
                await BrowserSession.CreateAsync(
                    browserFactory,
                    options.Value);

            await session.Login.LoginAsync();
            await session.Unlock.OpenAsync();
            await session.Unlock.FillAsync(request.Serial, request.Password);

            var contraSenha =
                await session.Unlock.GetContraSenhaAsync();

            automationTelemetry.RecordSuccess(stopwatch.ElapsedMilliseconds);
            return new UnlockResponse(contraSenha);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            automationTelemetry.RecordCancelled(stopwatch.ElapsedMilliseconds);
            throw;
        }
        catch (Exception exception)
        {
            automationTelemetry.RecordFailure(
                GetFailureCode(exception),
                "request",
                stopwatch.ElapsedMilliseconds);
            throw;
        }
    }

    private static string GetFailureCode(Exception exception)
    {
        return exception switch
        {
            WaitTaskTimeoutException or TimeoutException => "AUTOMATION_TIMEOUT",
            PuppeteerException => "AUTOMATION_BROWSER_ERROR",
            _ => "AUTOMATION_ERROR"
        };
    }
    //todo: add method for validate request and return UnlockResponse with error message if invalid
}
