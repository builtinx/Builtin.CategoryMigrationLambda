namespace CategoryMigrationLambda.Data;

/// <summary>
/// Result of a migration operation
/// </summary>
public record MigrationResultDto
{
    /// <summary>
    /// When the migration started
    /// </summary>
    public DateTime StartTime { get; set; }

    /// <summary>
    /// When the migration completed
    /// </summary>
    public DateTime EndTime { get; set; }

    /// <summary>
    /// Total duration of the migration
    /// </summary>
    public TimeSpan Duration { get; set; }

    /// <summary>
    /// Number of preferences processed
    /// </summary>
    public int ProcessedCount { get; set; }

    /// <summary>
    /// Number of preferences successfully migrated
    /// </summary>
    public int MigratedCount { get; set; }

    /// <summary>
    /// Number of errors encountered
    /// </summary>
    public int ErrorCount { get; set; }

    /// <summary>
    /// List of error messages
    /// </summary>
    public List<string> Errors { get; set; } = new();

    /// <summary>
    /// Whether this was a dry run
    /// </summary>
    public bool DryRun { get; set; }
}

/// <summary>
/// Result of a user-specific migration operation
/// </summary>
public record UserMigrationResultDto : MigrationResultDto
{
    /// <summary>
    /// The subject ID that was migrated
    /// </summary>
    public string SubjectId { get; set; } = string.Empty;
}
