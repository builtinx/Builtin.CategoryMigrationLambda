using CategoryMigrationLambda.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CategoryMigrationLambda.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddServices(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddLogging(x => x.AddLambdaLogger(new LambdaLoggerOptions(configuration)))
            .AddSingleton<IConfiguration>(_ => configuration)
            .AddScoped<ICategoryMigrationService, CategoryMigrationService>();

        return services;
    }
}
