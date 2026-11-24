using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using TalepYonetim.Reporting.Application;

namespace TalepYonetim.Reporting.Endpoints;

public static class SystemStatusEndpoints
{
    public static IEndpointRouteBuilder MapSystemStatusEndpoints(this IEndpointRouteBuilder app)
    {
        // Anonymous by decision of spec 0001 (Constraints); spec 0002 revisits it with default-deny.
        app.MapGet("/api/system/status", async (SystemStatusQuery query, CancellationToken cancellationToken) =>
                TypedResults.Ok(await query.ExecuteAsync(cancellationToken)))
            .WithName("GetSystemStatus")
            .WithTags("System")
            .AllowAnonymous();
        return app;
    }
}
