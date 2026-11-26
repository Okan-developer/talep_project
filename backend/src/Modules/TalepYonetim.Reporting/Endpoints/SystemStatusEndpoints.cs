using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using TalepYonetim.Reporting.Application;

namespace TalepYonetim.Reporting.Endpoints;

public static class SystemStatusEndpoints
{
    public static IEndpointRouteBuilder MapSystemStatusEndpoints(this IEndpointRouteBuilder app)
    {
        // Any signed-in user, no particular permission (spec 0002, R-9): the fallback policy applies.
        app.MapGet("/api/system/status", async (SystemStatusQuery query, CancellationToken cancellationToken) =>
                TypedResults.Ok(await query.ExecuteAsync(cancellationToken)))
            .WithName("GetSystemStatus")
            .WithTags("System");
        return app;
    }
}
