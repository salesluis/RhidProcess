using Microsoft.AspNetCore.Mvc;
using RhidProcess.Diagnostics;
using RhidProcess.Health;
using RhidProcess.Logging;
using RhidProcess.Models;
using RhidProcess.Services;

namespace RhidProcess.Routes;

public static class RhidRoute
{
    public static void MapRhidRoute(this WebApplication app)
    {
        app.MapGet("/v2/rhid/unlock", async (
            [AsParameters] UnlockRequest request,
            [FromServices] RepAutomationService service,
            HttpContext context) =>
        {
            var result = await service.ExecuteAsync(request, context.RequestAborted);
            return Results.Ok(result);
        });

        app.MapGet("/v2/health", (
            [FromServices] RhidHealthService health,
            HttpContext context) =>
        {
            if (health.IsHealthy())
                return Results.Ok(new { status = "healthy" });

            const string message = "Serviço indisponível devido a uma configuração inválida.";
            var errorId = ErrorContext.GetErrorId(context);
            ErrorContext.Set(
                context,
                AutomationErrorCodes.ConfigurationInvalid,
                AutomationStages.Configuration,
                errorId);

            return Results.Json(
                new ApiErrorResponse(
                    errorId,
                    AutomationErrorCodes.ConfigurationInvalid,
                    AutomationStages.Configuration,
                    message),
                statusCode: StatusCodes.Status503ServiceUnavailable);
        });
    }
}
