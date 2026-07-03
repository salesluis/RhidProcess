using Microsoft.AspNetCore.Mvc;
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
    }
}