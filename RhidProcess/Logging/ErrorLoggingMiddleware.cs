using RhidProcess.Diagnostics;
using RhidProcess.Models;

namespace RhidProcess.Logging;

public sealed class ErrorLoggingMiddleware(
    RequestDelegate next,
    ErrorFileLogger logger,
    ILogger<ErrorLoggingMiddleware> diagnosticLogger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var requestErrorId = ErrorContext.GetErrorId(context);
        using var scope = diagnosticLogger.BeginScope(
            new Dictionary<string, object>
            {
                ["ErrorId"] = requestErrorId
            });

        try
        {
            await next(context);

            if (context.Response.StatusCode >= StatusCodes.Status500InternalServerError)
            {
                var metadata = ErrorContext.Get(context);
                await logger.LogResponseAsync(
                    context,
                    metadata?.ErrorId ?? ErrorContext.GetErrorId(context),
                    metadata?.Code ?? AutomationErrorCodes.InternalError,
                    metadata?.Stage ?? AutomationStages.Request);
            }
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            // The client disconnected. Do not convert an expected cancellation into a 500.
        }
        catch (Exception ex)
        {
            var errorId = requestErrorId;
            var (statusCode, code, stage, message) = MapException(ex);

            if (!context.Response.HasStarted)
                context.Response.StatusCode = statusCode;

            await logger.LogAsync(ex, context, errorId, code, stage);

            if (context.Response.HasStarted)
                throw;

            context.Response.Clear();
            context.Response.StatusCode = statusCode;
            context.Response.ContentType = "application/json";

            await context.Response.WriteAsJsonAsync(
                new ApiErrorResponse(errorId, code, stage, message),
                context.RequestAborted);
        }
    }

    private static (int StatusCode, string Code, string Stage, string Message) MapException(
        Exception exception)
    {
        if (exception is RhidAutomationException automationException)
        {
            return (
                automationException.StatusCode,
                automationException.Code,
                automationException.Stage,
                automationException.PublicMessage);
        }

        return (
            StatusCodes.Status500InternalServerError,
            AutomationErrorCodes.InternalError,
            AutomationStages.Request,
            "Ocorreu um erro interno. Use o errorId para consultar os logs.");
    }
}
