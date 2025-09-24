using Amazon.Lambda.Core;
using Amazon.Lambda.SQSEvents;
using Amazon.Lambda.Serialization.SystemTextJson;
using CategoryMigrationLambda.Data;
using CategoryMigrationLambda.Extensions;
using CategoryMigrationLambda.Services;
using JetBrains.Annotations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: Amazon.Lambda.Core.LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace CategoryMigrationLambda;

[UsedImplicitly]
public class Function
{
    /// <summary>
    ///   Default timeout to use for creating cancellation tokens when testing locally.
    /// </summary>
    /// <remarks>
    ///   `context.RemainingTime` should have a valid time running in AWS. Testing locally it's always zero so use this fallback.
    /// </remarks>
    private const int DefaultRunningSeconds = 60;

    /// <summary>
    /// Helper to access the current environment.
    /// </summary>
    private readonly string _environmentName = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? string.Empty;

    /// <summary>
    /// Stores configuration from appSettings and environment vars.
    /// </summary>
    private IConfiguration _configuration;

    /// <summary>
    /// Collection of services for DI.
    /// </summary>
    private static ServiceProvider? _serviceProvider;

    public Function()
    {
        ConfigureServices();
    }

    /// <summary>
    ///   Lambda function handler for category migration operations.
    /// </summary>
    /// <param name="request">The migration request for the Lambda function handler to process.</param>
    /// <param name="context">The ILambdaContext that provides methods for logging and describing the Lambda environment.</param>
    public async Task<MigrationResultDto> FunctionHandler(MigrationRequestDto request, ILambdaContext context)
    {
        try
        {
            context.Logger.LogInformation($"FunctionHandler invoked with request: {JsonSerializer.Serialize(request)}");

            if (_serviceProvider is null)
                throw new InvalidOperationException("ServiceProvider not initialized");

            using var scope = _serviceProvider.CreateScope();
            var migrationService = scope.ServiceProvider.GetRequiredService<ICategoryMigrationService>();

            return request.Type.ToLower() switch
            {
                "all" => await migrationService.MigrateAllPreferencesAsync(request.DryRun, CreateCancellationToken(context)),
                "user" => await migrationService.MigrateUserPreferencesAsync(
                    request.SubjectId ?? throw new ArgumentException("SubjectId is required for user migration"), 
                    request.DryRun, 
                    CreateCancellationToken(context)),
                _ => throw new ArgumentException($"Unknown migration type: {request.Type}")
            };
        }
        catch (Exception ex)
        {
            context.Logger.LogError("FunctionHandler encountered an unrecoverable exception: " + ex);
            return new MigrationResultDto
            {
                StartTime = DateTime.UtcNow,
                EndTime = DateTime.UtcNow,
                Duration = TimeSpan.Zero,
                ProcessedCount = 0,
                MigratedCount = 0,
                ErrorCount = 1,
                Errors = new List<string> { ex.Message },
                DryRun = request.DryRun
            };
        }
    }

    /// <summary>
    ///   Lambda function handler for SQS events.
    /// </summary>
    /// <param name="sqsEvent">The SQS event for the Lambda function handler to process.</param>
    /// <param name="context">The ILambdaContext that provides methods for logging and describing the Lambda environment.</param>
    public async Task<SQSEvent> FunctionHandler(SQSEvent sqsEvent, ILambdaContext context)
    {
        foreach (var record in sqsEvent.Records)
        {
            try
            {
                var request = JsonSerializer.Deserialize<MigrationRequestDto>(record.Body);
                if (request != null)
                {
                    await FunctionHandler(request, context);
                }
            }
            catch (Exception ex)
            {
                context.Logger.LogError($"Error processing SQS record {record.MessageId}: {ex.Message}");
                throw;
            }
        }

        return sqsEvent;
    }

    /// <summary>
    /// Load configuration and set up services for DI.
    /// </summary>
    [MemberNotNull(nameof(_configuration))]
    private void ConfigureServices()
    {
        try
        {
            _configuration = (ConfigurationManager) new ConfigurationManager()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", true)
                .AddJsonFile($"appsettings.{_environmentName}.json", true)
                .AddEnvironmentVariables();
        }
        catch (Exception ex)
        {
            // Log the error and use default configuration or rethrow if critical
            throw new InvalidOperationException("Failed to initialize configuration", ex);
        }

        var serviceCollection = new ServiceCollection();
        serviceCollection.AddServices(_configuration);
        _serviceProvider = serviceCollection.BuildServiceProvider();
    }

    /// <summary>
    /// Creates a cancellation token from the context.
    /// </summary>
    private static CancellationToken CreateCancellationToken(ILambdaContext context)
    {
        return new CancellationTokenSource(context.RemainingTime.TotalSeconds > 0
            ? context.RemainingTime
            : TimeSpan.FromSeconds(DefaultRunningSeconds)).Token;
    }

    ~Function()
    {
        _serviceProvider?.Dispose();
    }
}
