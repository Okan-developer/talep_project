using TalepYonetim.SharedKernel;

namespace TalepYonetim.Api.Errors;

/// <summary>
/// Every error response is a ProblemDetails with a Turkish title and detail and no internal detail
/// (docs/conventions.md, Error handling; spec 0002, R-12). A business error written by an endpoint
/// marks itself with the <see cref="ProblemExtensions.Code"/> extension and keeps its own text;
/// everything else (framework 400/401/403/404/429/500) gets the catalogue text.
/// </summary>
public static class ProblemDetailsSetup
{
    public static IServiceCollection AddTurkishProblemDetails(this IServiceCollection services) =>
        services.AddProblemDetails(options => options.CustomizeProblemDetails = context =>
        {
            var problem = context.ProblemDetails;
            var status = problem.Status ?? context.HttpContext.Response.StatusCode;
            problem.Status = status;

            // Never echo exception data, whatever produced the problem.
            problem.Extensions.Remove("exception");

            if (problem.Extensions.ContainsKey(ProblemExtensions.Code))
            {
                return;
            }

            var text = ErrorCatalog.For(status);
            problem.Title = text.Title;
            problem.Detail = text.Detail;
            if (problem.Extensions.TryGetValue("errors", out var errors) && errors is IDictionary<string, string[]> fieldErrors)
            {
                // Framework field messages are English and may name internal types: keep the keys only.
                foreach (var key in fieldErrors.Keys.ToArray())
                {
                    fieldErrors[key] = [text.Detail];
                }
            }
        });
}
