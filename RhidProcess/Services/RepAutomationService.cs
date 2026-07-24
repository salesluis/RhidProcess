using System.Diagnostics;
using Microsoft.Extensions.Options;
using RhidProcess.Abstractions;
using RhidProcess.Browser;
using RhidProcess.Diagnostics;
using RhidProcess.Health;
using RhidProcess.Models;
using RhidProcess.Options;

namespace RhidProcess.Services;

public sealed class RepAutomationService(
    IBrowserFactory browserFactory,
    IOptions<RhidOptions> options,
    RhidHealthService health,
    ILoggerFactory loggerFactory,
    ILogger<RepAutomationService> logger)
{
    public async Task<UnlockResponse> ExecuteAsync(UnlockRequest request, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        logger.LogInformation(
            "RHID automation request started. Stage={Stage}",
            AutomationStages.Request);

        try
        {
            if (!health.IsHealthy())
            {
                throw new RhidAutomationException(
                    AutomationErrorCodes.ConfigurationInvalid,
                    AutomationStages.Configuration,
                    "Serviço indisponível devido a uma configuração inválida.",
                    StatusCodes.Status503ServiceUnavailable);
            }

            await using var session = await BrowserSession.CreateAsync(
                browserFactory,
                options.Value,
                loggerFactory,
                cancellationToken);

            await session.Login.LoginAsync(cancellationToken);
            await session.Unlock.OpenAsync(cancellationToken);
            await session.Unlock.FillAsync(
                request.Serial,
                request.Password,
                cancellationToken);

            var contraSenha = await session.Unlock
                .GetContraSenhaAsync(cancellationToken);

            logger.LogInformation(
                "RHID automation request completed. Stage={Stage} DurationMs={DurationMs}",
                AutomationStages.Request,
                stopwatch.ElapsedMilliseconds);

            return new UnlockResponse(contraSenha);
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            logger.LogInformation(
                "RHID automation request cancelled. Stage={Stage} DurationMs={DurationMs}",
                AutomationStages.Request,
                stopwatch.ElapsedMilliseconds);
            throw;
        }
        catch (RhidAutomationException exception)
        {
            logger.LogWarning(
                "RHID automation request failed. Stage={Stage} Code={Code} ExceptionType={ExceptionType} DurationMs={DurationMs}",
                exception.Stage,
                exception.Code,
                exception.GetType().Name,
                stopwatch.ElapsedMilliseconds);
            throw;
        }
    }
}
