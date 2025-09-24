using CategoryMigrationLambda.Data;

namespace CategoryMigrationLambda.Services;

/// <summary>
/// Service for migrating category and subcategory IDs in job preferences
/// </summary>
public interface ICategoryMigrationService
{
    /// <summary>
    /// Migrates all job preferences that need migration
    /// </summary>
    /// <param name="dryRun">Whether to perform a dry run without making changes</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Migration result</returns>
    Task<MigrationResultDto> MigrateAllPreferencesAsync(bool dryRun = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Migrates job preferences for a specific user
    /// </summary>
    /// <param name="subjectId">The user's subject ID</param>
    /// <param name="dryRun">Whether to perform a dry run without making changes</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Migration result</returns>
    Task<UserMigrationResultDto> MigrateUserPreferencesAsync(string subjectId, bool dryRun = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a category/subcategory combination needs migration
    /// </summary>
    /// <param name="categoryId">The category ID</param>
    /// <param name="subcategoryIds">The subcategory IDs</param>
    /// <returns>True if migration is needed</returns>
    bool NeedsMigration(int? categoryId, List<int> subcategoryIds);
}
