using Microsoft.Extensions.DependencyInjection;
using TalepYonetim.Reporting.Application;

namespace TalepYonetim.Reporting;

public static class ReportingModule
{
    public static IServiceCollection AddReportingModule(this IServiceCollection services)
    {
        services.AddOptions<SystemStatusOptions>();
        services.AddScoped<SystemStatusQuery>();
        return services;
    }
}
