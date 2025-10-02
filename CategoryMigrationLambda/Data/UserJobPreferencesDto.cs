using Amazon.DynamoDBv2.DataModel;

namespace CategoryMigrationLambda.Data;

/// <summary>
/// Simplified UserJobPreferences model for Lambda operations
/// </summary>
[DynamoDBTable("Users")]
public class UserJobPreferencesDto
{
    [DynamoDBHashKey]
    public string PK { get; set; } = string.Empty;

    [DynamoDBRangeKey]
    public string SK { get; set; } = string.Empty;

    [DynamoDBProperty]
    public string EntityId { get; set; } = string.Empty;

    [DynamoDBProperty]
    public string Type { get; set; } = string.Empty;

    [DynamoDBProperty]
    public int? CategoryId { get; set; }

    [DynamoDBProperty]
    public List<int> SubcategoryIds { get; set; } = new();
}
