using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace SPC.Infrastructure.XmlSigning;

public static class DependencyInjection
{
    public const string ConfigSection = "XmlSigning";

    public static IServiceCollection AddXmlSigning(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<XmlSigningOptions>(configuration.GetSection(ConfigSection));
        var mode = configuration[$"{ConfigSection}:Mode"] ?? "Stub";

        if (string.Equals(mode, "LocalAgent", StringComparison.OrdinalIgnoreCase))
        {
            services.AddHttpClient<IXmlSigningService, LocalAgentXmlSigningService>();
        }
        else
        {
            services.AddScoped<IXmlSigningService, StubXmlSigningService>();
        }
        return services;
    }
}
