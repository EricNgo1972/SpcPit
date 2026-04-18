using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SPC.BO.PIT.Pdf;
using SPC.BO.PIT.Xml;

namespace SPC.BO.PIT;

public static class DependencyInjection
{
    public static IServiceCollection AddSPCBOPIT(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSingleton<MessageIdFactory>();
        services.AddSingleton<PitPdfBuilder>();
        services.AddScoped<PitExcelImporter>();
        return services;
    }
}
