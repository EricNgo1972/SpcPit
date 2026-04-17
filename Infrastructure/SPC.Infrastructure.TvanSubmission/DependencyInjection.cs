using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace SPC.Infrastructure.TvanSubmission;

public static class DependencyInjection
{
    public const string ConfigSection = "Tvan";

    public static IServiceCollection AddTvanSubmission(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<TvanSubmissionOptions>(configuration.GetSection(ConfigSection));

        // Real providers (Viettel, VNPT, MISA, …) plug in here when their contracts are known.
        services.AddScoped<ITvanSubmissionService, StubTvanSubmissionService>();
        return services;
    }
}
