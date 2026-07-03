using RhidProcess.Models;
using RhidProcess.Services;

namespace RhidProcess.Routes;

public static class RhidRoute
{
    public static void MapRhidRoute(this WebApplication app, RepAutomationService service)
    {
        app.MapGet("/rhid", async ([AsParameters] UnlockRequest request) =>
        {
            var result = await service.ExecuteAsync(request);
            return Results.Ok(result);
        });
    }
}