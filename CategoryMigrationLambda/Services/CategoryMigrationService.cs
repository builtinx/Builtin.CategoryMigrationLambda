using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;
using CategoryMigrationLambda.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CategoryMigrationLambda.Services;

/// <summary>
/// Service for migrating category and subcategory IDs in job preferences
/// </summary>
public class CategoryMigrationService : ICategoryMigrationService
{
    private readonly AmazonDynamoDBClient _dynamoDbClient;
    private readonly DynamoDBContext _dynamoDbContext;
    private readonly ILogger<CategoryMigrationService> _logger;
    private readonly IConfiguration _configuration;
    private readonly string _tableName;
    private readonly int _batchSize;
    private readonly int _legacyCategoryMin;
    private readonly int _legacyCategoryMax;

    public CategoryMigrationService(
        ILogger<CategoryMigrationService> logger,
        IConfiguration configuration)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        
        _dynamoDbClient = new AmazonDynamoDBClient();
        _dynamoDbContext = new DynamoDBContext(_dynamoDbClient);
        
        _tableName = _configuration["DynamoDB:TableName"] ?? "Users";
        _batchSize = _configuration.GetValue<int>("Migration:BatchSize", 25);
        _legacyCategoryMin = _configuration.GetValue<int>("Migration:LegacyCategoryIdRange:Min", 1000);
        _legacyCategoryMax = _configuration.GetValue<int>("Migration:LegacyCategoryIdRange:Max", 1019);
    }

    public async Task<MigrationResultDto> MigrateAllPreferencesAsync(bool dryRun = false, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting bulk category migration for all job preferences (DryRun: {DryRun})", dryRun);
        
        var result = new MigrationResultDto
        {
            StartTime = DateTime.UtcNow,
            ProcessedCount = 0,
            MigratedCount = 0,
            ErrorCount = 0,
            Errors = new List<string>(),
            DryRun = dryRun
        };

        try
        {
            await ScanAndMigrateAllPreferences(result, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fatal error during migration");
            result.Errors.Add($"Fatal error: {ex.Message}");
        }
        finally
        {
            result.EndTime = DateTime.UtcNow;
            result.Duration = result.EndTime - result.StartTime;
        }

        _logger.LogInformation("Migration completed: {MigratedCount}/{ProcessedCount} preferences migrated, {ErrorCount} errors", 
            result.MigratedCount, result.ProcessedCount, result.ErrorCount);
        
        return result;
    }

    public async Task<UserMigrationResultDto> MigrateUserPreferencesAsync(string subjectId, bool dryRun = false, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting category migration for user {SubjectId} (DryRun: {DryRun})", subjectId, dryRun);
        
        var result = new UserMigrationResultDto
        {
            SubjectId = subjectId,
            StartTime = DateTime.UtcNow,
            ProcessedCount = 0,
            MigratedCount = 0,
            ErrorCount = 0,
            Errors = new List<string>(),
            DryRun = dryRun
        };

        try
        {
            await ScanAndMigrateUserPreferences(subjectId, result, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error migrating user {SubjectId}", subjectId);
            result.Errors.Add($"Fatal error: {ex.Message}");
        }
        finally
        {
            result.EndTime = DateTime.UtcNow;
            result.Duration = result.EndTime - result.StartTime;
        }

        _logger.LogInformation("User migration completed for {SubjectId}: {MigratedCount}/{ProcessedCount} preferences migrated", 
            subjectId, result.MigratedCount, result.ProcessedCount);
        
        return result;
    }

    public bool NeedsMigration(int? categoryId, List<int> subcategoryIds)
    {
        if (!categoryId.HasValue)
            return false;

        // Check if this is a legacy category ID (not in the new range 1-19)
        if (categoryId.Value >= _legacyCategoryMin || categoryId.Value < 1)
        {
            return true;
        }

        // Check if any subcategory IDs are legacy (typically >= 1000)
        if (subcategoryIds?.Any(id => id >= _legacyCategoryMin) == true)
        {
            return true;
        }

        return false;
    }

    private async Task ScanAndMigrateAllPreferences(MigrationResultDto result, CancellationToken cancellationToken)
    {
        var table = Table.LoadTable(_dynamoDbClient, _tableName);
        var scanFilter = new ScanFilter();

        // Filter for UserJobPreferences items
        scanFilter.AddCondition("Type", ScanOperator.Equal, "UserJobPreferences");

        _logger.LogInformation("Starting scan of table: {TableName}", _tableName);
        _logger.LogInformation("Scan filter: Type = UserJobPreferences");
        _logger.LogInformation("AWS Region: {Region}", _dynamoDbClient.Config.RegionEndpoint?.SystemName ?? "Not set");

        // Scan all UserJobPreferences items - we'll filter by legacy category IDs in the processing loop
        // This is more efficient than complex scan conditions

        var search = table.Scan(scanFilter);
        var batch = new List<Document>();

        var pageCount = 0;
        do
        {
            pageCount++;
            var documents = await search.GetNextSetAsync();
            _logger.LogInformation("Page {PageNumber}: Retrieved {DocumentCount} documents", pageCount, documents.Count);
            
            foreach (var document in documents)
            {
                try
                {
                    var preference = _dynamoDbContext.FromDocument<UserJobPreferencesDto>(document);
                    result.ProcessedCount++;
                    
                    if (NeedsMigration(preference.CategoryId, preference.SubcategoryIds))
                    {
                        var (newCategoryId, newSubcategoryIds) = MigrateCategoryAndSubcategories(
                            preference.CategoryId, preference.SubcategoryIds);
                        
                        preference.CategoryId = newCategoryId;
                        preference.SubcategoryIds = newSubcategoryIds;
                        
                        if (!result.DryRun)
                        {
                            batch.Add(_dynamoDbContext.ToDocument(preference));
                        }
                        
                        result.MigratedCount++;
                        
                        _logger.LogInformation("Migrated preference {EntityId}: CategoryId {OldCategoryId} -> {NewCategoryId}", 
                            preference.EntityId, preference.CategoryId, newCategoryId);
                        
                        if (batch.Count >= _batchSize)
                        {
                            await WriteBatch(batch, result);
                            batch.Clear();
                        }
                    }
                }
                catch (Exception ex)
                {
                    result.ErrorCount++;
                    var error = $"Error processing preference {document["EntityId"]}: {ex.Message}";
                    result.Errors.Add(error);
                    _logger.LogError(ex, error);
                }
            }
        } while (!search.IsDone);

        _logger.LogInformation("Scan completed. Total pages scanned: {PageCount}", pageCount);
        _logger.LogInformation("Total documents processed: {ProcessedCount}, Migrations needed: {MigratedCount}",
            result.ProcessedCount, result.MigratedCount);

        // Write remaining items in batch
        if (batch.Any())
        {
            await WriteBatch(batch, result);
        }
    }

    private async Task ScanAndMigrateUserPreferences(string subjectId, UserMigrationResultDto result, CancellationToken cancellationToken)
    {
        var table = Table.LoadTable(_dynamoDbClient, _tableName);
        var queryFilter = new QueryFilter("PK", QueryOperator.Equal, $"SUBJECTID#{subjectId}");
        queryFilter.AddCondition("Type", QueryOperator.Equal, "UserJobPreferences");

        _logger.LogInformation("Starting query for user {SubjectId} on table: {TableName}", subjectId, _tableName);
        _logger.LogInformation("Query filter: PK = SUBJECTID#{SubjectId}, Type = UserJobPreferences", subjectId);

        var search = table.Query(queryFilter);

        var pageCount = 0;
        do
        {
            pageCount++;
            var documents = await search.GetNextSetAsync();
            _logger.LogInformation("Page {PageNumber}: Retrieved {DocumentCount} documents for user {SubjectId}",
                pageCount, documents.Count, subjectId);
            
            foreach (var document in documents)
            {
                try
                {
                    var preference = _dynamoDbContext.FromDocument<UserJobPreferencesDto>(document);
                    result.ProcessedCount++;
                    
                    if (NeedsMigration(preference.CategoryId, preference.SubcategoryIds))
                    {
                        var (newCategoryId, newSubcategoryIds) = MigrateCategoryAndSubcategories(
                            preference.CategoryId, preference.SubcategoryIds);
                        
                        preference.CategoryId = newCategoryId;
                        preference.SubcategoryIds = newSubcategoryIds;
                        
                        if (!result.DryRun)
                        {
                            await _dynamoDbContext.SaveAsync(preference, cancellationToken);
                        }
                        
                        result.MigratedCount++;
                        
                        _logger.LogInformation("Migrated preference {EntityId} for user {SubjectId}", 
                            preference.EntityId, subjectId);
                    }
                }
                catch (Exception ex)
                {
                    result.ErrorCount++;
                    var error = $"Error processing preference {document["EntityId"]} for user {subjectId}: {ex.Message}";
                    result.Errors.Add(error);
                    _logger.LogError(ex, error);
                }
            }
        } while (!search.IsDone);

        _logger.LogInformation("Query completed for user {SubjectId}. Total pages: {PageCount}", subjectId, pageCount);
        _logger.LogInformation("Total documents processed: {ProcessedCount}, Migrations needed: {MigratedCount}",
            result.ProcessedCount, result.MigratedCount);
    }

    private async Task WriteBatch(List<Document> batch, MigrationResultDto result)
    {
        try
        {
            var table = Table.LoadTable(_dynamoDbClient, _tableName);
            var batchWrite = table.CreateBatchWrite();
            
            foreach (var document in batch)
            {
                batchWrite.AddDocumentToPut(document);
            }
            
            await batchWrite.ExecuteAsync();
            _logger.LogInformation("Successfully wrote batch of {Count} items", batch.Count);
        }
        catch (Exception ex)
        {
            result.ErrorCount += batch.Count;
            var error = $"Error writing batch of {batch.Count} items: {ex.Message}";
            result.Errors.Add(error);
            _logger.LogError(ex, error);
        }
    }

    private (int? NewCategoryId, List<int> NewSubcategoryIds) MigrateCategoryAndSubcategories(
        int? legacyCategoryId, 
        List<int> legacySubcategoryIds)
    {
        var newSubcategoryIds = new List<int>();
        int? newCategoryId = null;

        // Handle null or empty subcategory list
        if (legacySubcategoryIds == null || !legacySubcategoryIds.Any())
        {
            // Try category-only mapping
            if (legacyCategoryId.HasValue)
            {
                var categoryOnlyKey = (legacyCategoryId.Value, (int?)null);
                if (CategoryMappings.CategoryMappingRulesByIds.TryGetValue(categoryOnlyKey, out var categoryOnlyMapping))
                {
                    newCategoryId = categoryOnlyMapping.NewCategoryId;
                }
            }
        }
        else
        {
            // Process each subcategory
            foreach (var legacySubcategoryId in legacySubcategoryIds)
            {
                var mappingKey = (legacyCategoryId, legacySubcategoryId);
                
                if (CategoryMappings.CategoryMappingRulesByIds.TryGetValue(mappingKey, out var mapping))
                {
                    // Set the new category ID (should be consistent across all subcategories)
                    if (newCategoryId == null)
                    {
                        newCategoryId = mapping.NewCategoryId;
                    }

                    // Add the new subcategory ID if it exists
                    if (mapping.NewSubcategoryId.HasValue)
                    {
                        newSubcategoryIds.Add(mapping.NewSubcategoryId.Value);
                    }
                }
            }
        }

        // Remove duplicates and sort
        newSubcategoryIds = newSubcategoryIds.Distinct().OrderBy(x => x).ToList();

        return (newCategoryId, newSubcategoryIds);
    }
}
