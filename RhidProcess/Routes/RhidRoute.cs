using Microsoft.AspNetCore.Mvc;
using RhidProcess.Health;
using RhidProcess.Models;
using RhidProcess.Services;

namespace RhidProcess.Routes;

public static class RhidRoute
{
    public static void MapRhidRoute(this WebApplication app)
    {
        app.MapGet("/v2/rhid/unlock", async (
            [AsParameters] UnlockRequest request,
            [FromServices] RepAutomationService service) =>
        {
            var result = await service.ExecuteAsync(request);
            return Results.Ok(result);
        });

        app.MapGet("/v2/health/live", (HttpContext context) =>
        {
            SetNoStore(context);
            return Results.Ok(new LivenessHealthResponse(
                HealthStatuses.Healthy,
                DateTimeOffset.UtcNow));
        });

        app.MapGet("/v2/health/ready", GetReadinessAsync);
        app.MapGet("/v2/health", GetReadinessAsync);
    }

    private static async Task<IResult> GetReadinessAsync(
        HttpContext context,
        [FromServices] RhidHealthService health,
        CancellationToken cancellationToken)
    {
        SetNoStore(context);
        var result = await health.GetReadinessAsync(cancellationToken);
        var statusCode = result.Status == HealthStatuses.Unhealthy
            ? StatusCodes.Status503ServiceUnavailable
            : StatusCodes.Status200OK;

        return Results.Json(result, statusCode: statusCode);
    }

    private static void SetNoStore(HttpContext context)
    {
        context.Response.Headers.CacheControl = "no-store";
    }
}
