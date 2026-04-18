using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SPC.Infrastructure.TvanSubmission.Viettel;

namespace SPC.Infrastructure.TvanSubmission;

public static class DependencyInjection
{
    /// <summary>
    /// Registers every known T-VAN adapter plus a runtime dispatcher. The active provider is
    /// chosen per-call from <c>PitSettings.TvanProviderName</c> — no app restart needed to
    /// switch between Stub, Viettel, and (future) VNPT/MISA/etc.
    /// </summary>
    public static IServiceCollection AddTvanSubmission(this IServiceCollection services, IConfiguration configuration)
    {
        // Concrete adapters — available for the dispatcher to resolve.
        services.AddScoped<StubTvanSubmissionService>();
        services.AddScoped<IViettelOptionsProvider, ViettelOptionsFromBoProvider>();
        services.AddHttpClient<ViettelTvanSubmissionService>();

        // Public API: always the dispatcher.
        services.AddScoped<ITvanSubmissionService, TvanSubmissionDispatcher>();

        return services;
    }
}
