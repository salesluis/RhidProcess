namespace RhidProcess.Logging;

public sealed class ErrorLoggingMiddleware(RequestDelegate next, ErrorFileLogger logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);

            if (context.Response.StatusCode >= StatusCodes.Status500InternalServerError
                && !IsHealthEndpoint(context.Request.Path))
                await logger.LogResponseAsync(context);
        }
        catch (Exception ex)
        {
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await logger.LogAsync(ex, context);

            context.Response.ContentType = "application/json";

            await context.Response.WriteAsJsonAsync(new
            {
                error = "Ocorreu um erro interno. Detalhes registrados em Logs.",
                stackTace = ex.StackTrace,
            }, context.RequestAborted);
        }
    }

    private static bool IsHealthEndpoint(PathString path)
    {
        return path.StartsWithSegments("/v2/health", StringComparison.OrdinalIgnoreCase);
    }
}
