namespace CategoryMigrationLambda.Data;

/// <summary>
/// Request data for category migration operations
/// </summary>
public record MigrationRequestDto
{
    /// <summary>
    /// Type of migration to perform: "all" or "user"
    /// </summary>
    public string Type { get; init; } = string.Empty;

    /// <summary>
    /// Subject ID for user-specific migration (required when Type is "user")
    /// </summary>
    public string? SubjectId { get; init; }

    /// <summary>
    /// Whether to perform a dry run without making changes
    /// </summary>
    public bool DryRun { get; init; } = false;
}
