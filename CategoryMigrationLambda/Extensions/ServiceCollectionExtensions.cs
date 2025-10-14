using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
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
            .AddSingleton<IAmazonDynamoDB, AmazonDynamoDBClient>()
            .AddSingleton<IDynamoDBContext>(sp =>
            {
                var dynamoDbClient = sp.GetRequiredService<IAmazonDynamoDB>();
                return new DynamoDBContext(dynamoDbClient);
            })
            .AddScoped<ICategoryMigrationService, CategoryMigrationService>();

        return services;
    }
}
