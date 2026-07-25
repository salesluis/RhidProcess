namespace RhidProcess.Auth;

public sealed class ApiKeyMiddleware(RequestDelegate next, IConfiguration configuration)
{
    private const string HeaderName = "X-Api-Key";

    public async Task InvokeAsync(HttpContext context)
    {
        var expectedKey = configuration["ApiKey"];

        if (string.IsNullOrEmpty(expectedKey))
        {
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await context.Response.WriteAsJsonAsync(new { error = "ApiKey não configurada." });
            return;
        }

        if (!context.Request.Headers.TryGetValue(HeaderName, out var providedKey)
            || !string.Equals(expectedKey, providedKey, StringComparison.Ordinal))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new { error = "Api key inválida ou ausente." });
            return;
        }

        await next(context);
    }
}
