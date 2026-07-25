using RhidProcess.Diagnostics;
using RhidProcess.Logging;
using RhidProcess.Models;

namespace RhidProcess.Auth;

public sealed class ApiKeyMiddleware(RequestDelegate next, IConfiguration configuration)
{
    private const string HeaderName = "X-Api-Key";

    public async Task InvokeAsync(HttpContext context)
    {
        var expectedKey = configuration["ApiKey"];

        if (string.IsNullOrEmpty(expectedKey))
        {
            var errorId = ErrorContext.GetErrorId(context);
            ErrorContext.Set(
                context,
                "API_KEY_CONFIGURATION_INVALID",
                AutomationStages.Configuration,
                errorId);
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await context.Response.WriteAsJsonAsync(
                new ApiErrorResponse(
                    errorId,
                    "API_KEY_CONFIGURATION_INVALID",
                    AutomationStages.Configuration,
                    "A autenticação da API não está configurada."),
                context.RequestAborted);
            return;
        }

        if (!context.Request.Headers.TryGetValue(HeaderName, out var providedKey)
            || !string.Equals(expectedKey, providedKey, StringComparison.Ordinal))
        {
            var errorId = ErrorContext.GetErrorId(context);
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(
                new ApiErrorResponse(
                    errorId,
                    "API_KEY_INVALID",
                    AutomationStages.Request,
                    "Api key inválida ou ausente."),
                context.RequestAborted);
            return;
        }

        await next(context);
    }
}
