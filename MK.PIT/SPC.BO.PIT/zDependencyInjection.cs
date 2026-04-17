using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using QuestPDF.Infrastructure;
using SPC.BO.PIT.Pdf;
using SPC.BO.PIT.Xml;

namespace SPC.BO.PIT;

public static class DependencyInjection
{
    public static IServiceCollection AddSPCBOPIT(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // QuestPDF community licence — pure-managed PDF generator, no native deps.
        QuestPDF.Settings.License = LicenseType.Community;

        services.AddSingleton<MessageIdFactory>();
        services.AddSingleton<PitPdfBuilder>();
        services.AddScoped<PitExcelImporter>();
        return services;
    }
}
