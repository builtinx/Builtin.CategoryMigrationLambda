namespace CategoryMigrationLambda.Data;

/// <summary>
/// Simplified UserJobPreferences model for Lambda operations
/// </summary>
public class UserJobPreferencesDto
{
    public string PK { get; set; } = string.Empty;
    public string SK { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public int? CategoryId { get; set; }
    public List<int> SubcategoryIds { get; set; } = new();
}
