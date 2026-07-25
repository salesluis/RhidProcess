using System.Diagnostics;
using PuppeteerSharp;
using RhidProcess.Diagnostics;

namespace RhidProcess.Browser;

internal sealed class AutomationStepRunner(ILogger logger)
{
    public Task RunAsync(
        string stage,
        Func<Task> operation,
        CancellationToken cancellationToken)
    {
        return RunAsync(
            stage,
            async () =>
            {
                await operation();
                return true;
            },
            cancellationToken);
    }

    public async Task<T> RunAsync<T>(
        string stage,
        Func<Task<T>> operation,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        logger.LogInformation(
            "RHID automation stage started. Stage={Stage}",
            stage);

        try
        {
            var result = await operation().WaitAsync(cancellationToken);

            logger.LogInformation(
                "RHID automation stage completed. Stage={Stage} DurationMs={DurationMs}",
                stage,
                stopwatch.ElapsedMilliseconds);

            return result;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            logger.LogInformation(
                "RHID automation stage cancelled. Stage={Stage} DurationMs={DurationMs}",
                stage,
                stopwatch.ElapsedMilliseconds);
            throw;
        }
        catch (RhidAutomationException exception)
        {
            logger.LogWarning(
                "RHID automation stage failed. Stage={Stage} Code={Code} ExceptionType={ExceptionType} DurationMs={DurationMs}",
                stage,
                exception.Code,
                exception.GetType().Name,
                stopwatch.ElapsedMilliseconds);
            throw;
        }
        catch (Exception exception) when (IsTimeout(exception))
        {
            logger.LogWarning(
                "RHID automation stage timed out. Stage={Stage} ExceptionType={ExceptionType} DurationMs={DurationMs}",
                stage,
                exception.GetType().Name,
                stopwatch.ElapsedMilliseconds);

            throw new RhidAutomationException(
                AutomationErrorCodes.UpstreamTimeout,
                stage,
                "O RHID não respondeu dentro do tempo esperado.",
                StatusCodes.Status504GatewayTimeout,
                exception);
        }
        catch (Exception exception)
        {
            logger.LogError(
                "RHID automation stage failed. Stage={Stage} ExceptionType={ExceptionType} DurationMs={DurationMs}",
                stage,
                exception.GetType().Name,
                stopwatch.ElapsedMilliseconds);

            throw new RhidAutomationException(
                AutomationErrorCodes.UpstreamFailure,
                stage,
                "Não foi possível concluir a comunicação com o RHID.",
                StatusCodes.Status502BadGateway,
                exception);
        }
    }

    private static bool IsTimeout(Exception exception)
    {
        return exception is WaitTaskTimeoutException or TimeoutException;
    }
}
