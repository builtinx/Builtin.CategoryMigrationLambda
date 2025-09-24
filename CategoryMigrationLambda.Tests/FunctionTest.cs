using Amazon.Lambda.TestUtilities;
using CategoryMigrationLambda;
using CategoryMigrationLambda.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CategoryMigrationLambda.Tests;

public class FunctionTest
{
    [Fact]
    public async Task FunctionHandler_MigrateAll_ReturnsSuccessResult()
    {
        // Arrange
        var request = new MigrationRequestDto
        {
            Type = "all",
            DryRun = true
        };

        var context = new TestLambdaContext();
        var function = new Function();

        // Act
        var result = await function.FunctionHandler(request, context);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.DryRun);
        Assert.True(result.Duration >= TimeSpan.Zero);
    }

    [Fact]
    public async Task FunctionHandler_MigrateUser_ReturnsSuccessResult()
    {
        // Arrange
        var request = new MigrationRequestDto
        {
            Type = "user",
            SubjectId = "test-user-123",
            DryRun = true
        };

        var context = new TestLambdaContext();
        var function = new Function();

        // Act
        var result = await function.FunctionHandler(request, context);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.DryRun);
        Assert.True(result.Duration >= TimeSpan.Zero);
    }

    [Fact]
    public async Task FunctionHandler_InvalidType_ReturnsErrorResult()
    {
        // Arrange
        var request = new MigrationRequestDto
        {
            Type = "invalid",
            DryRun = true
        };

        var context = new TestLambdaContext();
        var function = new Function();

        // Act
        var result = await function.FunctionHandler(request, context);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.ErrorCount > 0);
        Assert.NotEmpty(result.Errors);
    }

    [Fact]
    public async Task FunctionHandler_UserTypeWithoutSubjectId_ReturnsErrorResult()
    {
        // Arrange
        var request = new MigrationRequestDto
        {
            Type = "user",
            SubjectId = null,
            DryRun = true
        };

        var context = new TestLambdaContext();
        var function = new Function();

        // Act
        var result = await function.FunctionHandler(request, context);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.ErrorCount > 0);
        Assert.NotEmpty(result.Errors);
    }
}
