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
    private readonly IAmazonDynamoDB _dynamoDbClient;
    private readonly IDynamoDBContext _dynamoDbContext;
    private readonly ILogger<CategoryMigrationService> _logger;
    private readonly IConfiguration _configuration;
    private readonly string _tableName;
    private readonly int _batchSize;

    public CategoryMigrationService(
        IAmazonDynamoDB dynamoDbClient,
        IDynamoDBContext dynamoDbContext,
        ILogger<CategoryMigrationService> logger,
        IConfiguration configuration)
    {
        _dynamoDbClient = dynamoDbClient ?? throw new ArgumentNullException(nameof(dynamoDbClient));
        _dynamoDbContext = dynamoDbContext ?? throw new ArgumentNullException(nameof(dynamoDbContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));

        _tableName = _configuration["DynamoDB:TableName"] ?? "Users";
        _batchSize = _configuration.GetValue("Migration:BatchSize", 25);
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

    public bool NeedsMigration(int? categoryId, List<int>? subcategoryIds)
    {
        if (!categoryId.HasValue)
            return false;

        // New category IDs are in the range 1-19
        // Anything outside this range is a legacy ID that needs migration
        // This includes: 146-157, 390-391, 1001-1018, etc.
        const int newCategoryMin = 1;
        const int newCategoryMax = 19;

        if (categoryId.Value < newCategoryMin || categoryId.Value > newCategoryMax)
        {
            return true;
        }

        // Check if any subcategory IDs are legacy
        // New subcategory IDs are typically < 200, legacy are >= 200
        return (subcategoryIds?.Any(id => id >= 200)).GetValueOrDefault();
    }

    private async Task ScanAndMigrateAllPreferences(MigrationResultDto result, CancellationToken cancellationToken)
    {
        var table = Table.LoadTable(_dynamoDbClient, _tableName);

        _logger.LogInformation("Starting scan of table: {TableName}", _tableName);
        _logger.LogInformation("AWS Region: {Region}", _dynamoDbClient.Config.RegionEndpoint?.SystemName ?? "Not set");

        // Note: The Type attribute is not reliably present in DynamoDB (getter-only property issue)
        // Instead, we identify UserJobPreferences by the presence of CategoryId attribute
        var scanFilter = new ScanFilter();
        scanFilter.AddCondition("CategoryId", ScanOperator.IsNotNull);

        _logger.LogInformation("Starting scan with filter: CategoryId IS NOT NULL (identifies UserJobPreferences)");

        var search = table.Scan(scanFilter);
        await ProcessDocumentsInBatches(table, search, result, null, cancellationToken);

        _logger.LogInformation("Scan completed");
        _logger.LogInformation("Total documents processed: {ProcessedCount}, Migrations needed: {MigratedCount}",
            result.ProcessedCount, result.MigratedCount);
    }

    private async Task ScanAndMigrateUserPreferences(string subjectId, UserMigrationResultDto result, CancellationToken cancellationToken)
    {
        var table = Table.LoadTable(_dynamoDbClient, _tableName);
        var queryFilter = new QueryFilter("PK", QueryOperator.Equal, $"SUBJECTID#{subjectId}");

        _logger.LogInformation("Starting query for user {SubjectId} on table: {TableName}", subjectId, _tableName);
        _logger.LogInformation("Query filter: PK = SUBJECTID#{SubjectId} (will filter by CategoryId presence in-memory)", subjectId);

        var search = table.Query(queryFilter);
        await ProcessDocumentsInBatches(table, search, result, subjectId, cancellationToken);

        _logger.LogInformation("Query completed for user {SubjectId}", subjectId);
        _logger.LogInformation("Total documents processed: {ProcessedCount}, Migrations needed: {MigratedCount}",
            result.ProcessedCount, result.MigratedCount);
    }

    private async Task ProcessDocumentsInBatches(
        Table table,
        Search search,
        MigrationResultDto result,
        string? userContext,
        CancellationToken cancellationToken)
    {
        var batch = new List<Document>();
        var pageCount = 0;

        do
        {
            pageCount++;
            var documents = await search.GetNextSetAsync(cancellationToken);

            if (userContext != null)
            {
                _logger.LogInformation("Page {PageNumber}: Retrieved {DocumentCount} documents for user {UserContext}",
                    pageCount, documents.Count, userContext);
            }
            else
            {
                _logger.LogInformation("Page {PageNumber}: Retrieved {DocumentCount} documents", pageCount, documents.Count);
            }

            foreach (var document in documents)
            {
                try
                {
                    // Skip documents that don't have CategoryId (not UserJobPreferences)
                    if (!document.ContainsKey("CategoryId"))
                    {
                        continue;
                    }

                    var preference = _dynamoDbContext.FromDocument<UserJobPreferencesDto>(document);
                    result.ProcessedCount++;

                    if (NeedsMigration(preference.CategoryId, preference.SubcategoryIds))
                    {
                        var oldCategoryId = preference.CategoryId;
                        var oldSubcategoryIds = preference.SubcategoryIds?.ToList() ?? new List<int>();

                        var groupedMigrations = MigrateCategoryAndSubcategoriesGrouped(
                            preference.CategoryId, preference.SubcategoryIds ?? new List<int>());

                        if (groupedMigrations.Count == 0)
                        {
                            _logger.LogWarning("No valid migration found for preference {EntityId} with CategoryId {CategoryId}",
                                preference.EntityId, preference.CategoryId);
                            continue;
                        }

                        // Get the first (primary) category - this will update the existing preference
                        var primaryCategory = groupedMigrations.First();
                        var primaryCategoryId = primaryCategory.Key;
                        var primarySubcategoryIds = primaryCategory.Value;

                        if (!result.DryRun)
                        {
                            // Update the existing document with the primary category
                            UpdateDocumentWithMigratedCategories(document, primaryCategoryId, primarySubcategoryIds);
                            batch.Add(document);
                        }

                        result.MigratedCount++;

                        // Log the primary migration
                        if (userContext != null)
                        {
                            _logger.LogInformation("Migrated preference {EntityId} for user {UserContext}: CategoryId {OldCategoryId} -> {NewCategoryId}, SubcategoryIds [{OldSubcategoryIds}] -> [{NewSubcategoryIds}]",
                                preference.EntityId, userContext, oldCategoryId, primaryCategoryId,
                                string.Join(",", oldSubcategoryIds), string.Join(",", primarySubcategoryIds));
                        }
                        else
                        {
                            _logger.LogInformation("Migrated preference {EntityId}: CategoryId {OldCategoryId} -> {NewCategoryId}, SubcategoryIds [{OldSubcategoryIds}] -> [{NewSubcategoryIds}]",
                                preference.EntityId, oldCategoryId, primaryCategoryId,
                                string.Join(",", oldSubcategoryIds), string.Join(",", primarySubcategoryIds));
                        }

                        // Handle additional categories (create new preferences)
                        if (groupedMigrations.Count > 1)
                        {
                            var additionalCategories = groupedMigrations.Skip(1);

                            foreach (var additionalCategory in additionalCategories)
                            {
                                var newCategoryId = additionalCategory.Key;
                                var newSubcategoryIds = additionalCategory.Value;

                                if (!result.DryRun)
                                {
                                    // Create a new document (preference) for this additional category
                                    var newDocument = CreateNewPreferenceDocument(document, newCategoryId, newSubcategoryIds);
                                    batch.Add(newDocument);
                                }

                                result.MigratedCount++;

                                // Log the new preference creation
                                var newEntityId = !result.DryRun ? document["EntityId"].AsString() + "_split_" + newCategoryId : "(dry-run)";
                                if (userContext != null)
                                {
                                    _logger.LogInformation("Created NEW preference {NewEntityId} for user {UserContext} from {OriginalEntityId}: CategoryId -> {NewCategoryId}, SubcategoryIds -> [{NewSubcategoryIds}]",
                                        newEntityId, userContext, preference.EntityId, newCategoryId,
                                        string.Join(",", newSubcategoryIds));
                                }
                                else
                                {
                                    _logger.LogInformation("Created NEW preference {NewEntityId} from {OriginalEntityId}: CategoryId -> {NewCategoryId}, SubcategoryIds -> [{NewSubcategoryIds}]",
                                        newEntityId, preference.EntityId, newCategoryId,
                                        string.Join(",", newSubcategoryIds));
                                }
                            }
                        }

                        if (batch.Count >= _batchSize)
                        {
                            await WriteBatch(table, batch, result, cancellationToken);
                            batch.Clear();
                        }
                    }
                }
                catch (Exception ex)
                {
                    result.ErrorCount++;
                    var errorContext = userContext != null ? $" for user {userContext}" : "";
                    var error = $"Error processing preference {document["EntityId"]}{errorContext}: {ex.Message}";
                    result.Errors.Add(error);
                    _logger.LogError(ex, error);
                }
            }
        } while (!search.IsDone && !cancellationToken.IsCancellationRequested);

        _logger.LogInformation("Scan/Query completed. Total pages: {PageCount}", pageCount);

        // Write remaining items in batch
        if (batch.Any())
        {
            await WriteBatch(table, batch, result, cancellationToken);
        }
    }

    private async Task WriteBatch(Table table, List<Document> batch, MigrationResultDto result, CancellationToken cancellationToken)
    {
        try
        {
            var batchWrite = table.CreateBatchWrite();

            foreach (var document in batch)
            {
                batchWrite.AddDocumentToPut(document);
            }

            await batchWrite.ExecuteAsync(cancellationToken);
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

    private Document CreateNewPreferenceDocument(Document originalDocument, int? newCategoryId, List<int> newSubcategoryIds)
    {
        // Clone the original document to preserve all fields
        var newDocument = new Document();

        foreach (var key in originalDocument.Keys)
        {
            newDocument[key] = originalDocument[key];
        }

        // Generate a new unique EntityId
        var originalEntityId = originalDocument["EntityId"].AsString();
        var newEntityId = Guid.NewGuid().ToString();
        newDocument["EntityId"] = newEntityId;

        // Update SK to reflect the new EntityId
        // SK format: ENTITYID#{EntityId}
        if (originalDocument.ContainsKey("SK"))
        {
            var originalSK = originalDocument["SK"].AsString();
            // Replace the old EntityId in SK with the new one
            newDocument["SK"] = originalSK.Replace(originalEntityId, newEntityId);
        }

        // Update timestamps
        var now = DateTime.UtcNow;
        var timestamp = now.ToString("o");

        // Update Gsi1SK to reflect the new EntityId and timestamp
        // Gsi1SK format: TIMESTAMP#{timestamp}#ENTITYID#{EntityId}
        if (originalDocument.ContainsKey("Gsi1SK"))
        {
            newDocument["Gsi1SK"] = $"TIMESTAMP#{timestamp}#ENTITYID#{newEntityId}";
        }

        // Update the migrated category and subcategories
        newDocument["CategoryId"] = newCategoryId;

        var subcategoryIdsList = new PrimitiveList(DynamoDBEntryType.Numeric);
        foreach (var id in newSubcategoryIds)
        {
            subcategoryIdsList.Add(new Primitive(id.ToString(), true));
        }
        newDocument["SubcategoryIds"] = subcategoryIdsList;

        // New split preferences should not be primary
        if (originalDocument.ContainsKey("IsPrimary"))
        {
            newDocument["IsPrimary"] = false;
        }

        newDocument["CreatedAt"] = timestamp;
        newDocument["UpdatedAt"] = timestamp;

        return newDocument;
    }

    private void UpdateDocumentWithMigratedCategories(Document document, int? newCategoryId, List<int> newSubcategoryIds)
    {
        // Update the original document directly to preserve all fields
        document["CategoryId"] = newCategoryId;

        var subcategoryIdsList = new PrimitiveList(DynamoDBEntryType.Numeric);
        foreach (var id in newSubcategoryIds)
        {
            subcategoryIdsList.Add(new Primitive(id.ToString(), true));
        }
        document["SubcategoryIds"] = subcategoryIdsList;

        // Update the timestamp
        document["UpdatedAt"] = DateTime.UtcNow.ToString("o");
    }

    private Dictionary<int?, List<int>> MigrateCategoryAndSubcategoriesGrouped(
        int? legacyCategoryId,
        List<int>? legacySubcategoryIds)
    {
        var result = new Dictionary<int?, List<int>>();

        // Handle null or empty subcategory list
        if (legacySubcategoryIds == null || !legacySubcategoryIds.Any())
        {
            // Try category-only mapping
            if (legacyCategoryId.HasValue)
            {
                var categoryOnlyKey = (legacyCategoryId.Value, (int?)null);
                if (CategoryMappings.CategoryMappingRulesByIds.TryGetValue(categoryOnlyKey, out var categoryOnlyMapping))
                {
                    if (!result.ContainsKey(categoryOnlyMapping.NewCategoryId))
                    {
                        result[categoryOnlyMapping.NewCategoryId] = new List<int>();
                    }
                }
            }
        }
        else
        {
            // Process each subcategory and group by new category ID
            foreach (var legacySubcategoryId in legacySubcategoryIds)
            {
                var mappingKey = (legacyCategoryId, legacySubcategoryId);

                if (CategoryMappings.CategoryMappingRulesByIds.TryGetValue(mappingKey, out var mapping))
                {
                    // Skip null mappings
                    if (!mapping.NewCategoryId.HasValue)
                    {
                        continue;
                    }

                    // Initialize list for this category if not exists
                    if (!result.ContainsKey(mapping.NewCategoryId))
                    {
                        result[mapping.NewCategoryId] = new List<int>();
                    }

                    // Add the new subcategory ID if it exists
                    if (mapping.NewSubcategoryId.HasValue)
                    {
                        result[mapping.NewCategoryId].Add(mapping.NewSubcategoryId.Value);
                    }
                }
            }

            // If no subcategory mappings were found, fall back to category-only mapping
            if (!result.Any() && legacyCategoryId.HasValue)
            {
                var categoryOnlyKey = (legacyCategoryId.Value, (int?)null);
                if (CategoryMappings.CategoryMappingRulesByIds.TryGetValue(categoryOnlyKey, out var categoryOnlyMapping))
                {
                    if (categoryOnlyMapping.NewCategoryId.HasValue)
                    {
                        result[categoryOnlyMapping.NewCategoryId] = new List<int>();
                    }
                }
            }
        }

        // Remove duplicates and sort subcategories for each category
        foreach (var categoryId in result.Keys.ToList())
        {
            result[categoryId] = result[categoryId].Distinct().OrderBy(x => x).ToList();
        }

        return result;
    }

    private (int? NewCategoryId, List<int> NewSubcategoryIds) MigrateCategoryAndSubcategories(
        int? legacyCategoryId,
        List<int>? legacySubcategoryIds)
    {
        var grouped = MigrateCategoryAndSubcategoriesGrouped(legacyCategoryId, legacySubcategoryIds);

        // Return the first (primary) category mapping for backward compatibility
        var firstEntry = grouped.FirstOrDefault();
        return (firstEntry.Key, firstEntry.Value ?? new List<int>());
    }
}
