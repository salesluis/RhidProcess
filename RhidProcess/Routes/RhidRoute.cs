namespace RhidProcess.Routes;
using RhidProcess.Models;
using RhidProcess.Services;

public static class RhidRoute{
    public static void MapRhidRoutes(this WebApplication app){
        app.MapPost("/v2/rhid/unlock", async (
            [AsParameters] UnlockRequest request,
            [FromServices] RepAutomationService service,
            CancellationToken cancellationToken) => {
            var result = await service.ExecuteAsync(request, cancellationToken);
            return TypedResults.Ok(result);
        });
    }
}