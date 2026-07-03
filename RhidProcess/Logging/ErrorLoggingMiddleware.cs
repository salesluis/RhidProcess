namespace RhidProcess.Logging;

public sealed class ErrorLoggingMiddleware(RequestDelegate next, ErrorFileLogger logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            await logger.LogAsync(ex, context, context.RequestAborted);

            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/json";

            await context.Response.WriteAsJsonAsync(new
            {
                error = "Ocorreu um erro interno. Detalhes registrados em Logs."
            }, context.RequestAborted);
        }
    }
}
