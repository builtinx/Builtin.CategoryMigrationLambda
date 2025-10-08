using Amazon.DynamoDBv2.DataModel;

namespace CategoryMigrationLambda.Data;

/// <summary>
/// UserJobPreferences model for Lambda operations
/// This is a comprehensive model that preserves all fields during migration
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

    // Additional common fields that might exist in UserJobPreferences
    [DynamoDBProperty]
    public string? SubjectId { get; set; }

    [DynamoDBProperty]
    public DateTime? CreatedAt { get; set; }

    [DynamoDBProperty]
    public DateTime? UpdatedAt { get; set; }

    [DynamoDBProperty]
    public bool? IsActive { get; set; }

    [DynamoDBProperty]
    public string? Status { get; set; }

    [DynamoDBProperty]
    public Dictionary<string, object>? AdditionalProperties { get; set; }

    // Allow for any additional properties that might exist in the DynamoDB document
    // This ensures we don't lose any data during migration
    [DynamoDBIgnore]
    public Dictionary<string, object> ExtraProperties { get; set; } = new();
}
