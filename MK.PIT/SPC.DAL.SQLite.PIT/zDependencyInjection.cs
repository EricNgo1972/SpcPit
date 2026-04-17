using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SPC.DAL.SQLite;

namespace SPC.DAL.SQLite.PIT;

public static class DependencyInjection
{
    private const string ConnectionStringName = "SpcPitDb";
    private const string DefaultDbFileName = "spc-pit.db";

    public static IServiceCollection AddSPCDALPIT(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(ConnectionStringName);
        if (string.IsNullOrWhiteSpace(connectionString))
            connectionString = $"Data Source={DefaultDbFileName}";

        services.AddSingleton(new SqlKataDb(connectionString));
        services.AddSingleton<DatabaseBootstrapper>();
        return services;
    }

    public static IApplicationBuilder UseSPCDALPIT(this IApplicationBuilder builder)
    {
        using var scope = builder.ApplicationServices.CreateScope();
        var bootstrapper = scope.ServiceProvider.GetRequiredService<DatabaseBootstrapper>();
        bootstrapper.InitializeAsync().GetAwaiter().GetResult();
        return builder;
    }
}
