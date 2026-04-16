namespace Tickflo.CoreTest.Services.Auth;

using Microsoft.EntityFrameworkCore;
using Tickflo.Core.Config;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;
using Tickflo.Core.Services.Authentication;
using Xunit;

public class PasswordSetupServiceTests
{
    [Fact]
    public async Task SetInitialPasswordAsyncShouldPersistPasswordHashAndLoginToken()
    {
        await using var databaseContext = CreateDatabaseContext();
        var user = new User("Demo Admin", "admin@demo.com", "recovery@example.com", "password-hash")
        {
            PasswordHash = null,
        };

        databaseContext.Users.Add(user);
        await databaseContext.SaveChangesAsync();

        var workspace = new Workspace
        {
            Name = "Demo Workspace",
            Slug = "demo-workspace",
            CreatedBy = user.Id,
        };

        databaseContext.Workspaces.Add(workspace);
        await databaseContext.SaveChangesAsync();

        databaseContext.UserWorkspaces.Add(new UserWorkspace
        {
            UserId = user.Id,
            WorkspaceId = workspace.Id,
            Accepted = true,
            CreatedBy = user.Id,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        });
        await databaseContext.SaveChangesAsync();

        var passwordSetupService = new PasswordSetupService(
            databaseContext,
            new TickfloConfig { SessionTimeoutMinutes = 20 },
            new Argon2idPasswordHasher());

        var result = await passwordSetupService.SetInitialPasswordAsync(user.Id, "demo-password");

        Assert.True(result.Success);
        Assert.False(string.IsNullOrWhiteSpace(result.LoginToken));
        Assert.Equal(workspace.Slug, result.WorkspaceSlug);

        var persistedUser = await databaseContext.Users.FindAsync(user.Id);
        Assert.NotNull(persistedUser?.PasswordHash);

        var persistedToken = await databaseContext.Tokens.FirstOrDefaultAsync(token => token.UserId == user.Id && token.Value == result.LoginToken);
        Assert.NotNull(persistedToken);
    }

    [Fact]
    public async Task ValidateInitialUserAsyncWhenUserIsNotDemoDomainShouldRejectInitialPasswordSetup()
    {
        await using var databaseContext = CreateDatabaseContext();
        var user = new User("Regular User", "user@example.com", "recovery@example.com", "password-hash")
        {
            PasswordHash = null,
        };

        databaseContext.Users.Add(user);
        await databaseContext.SaveChangesAsync();

        var passwordSetupService = new PasswordSetupService(
            databaseContext,
            new TickfloConfig { SessionTimeoutMinutes = 20 },
            new Argon2idPasswordHasher());

        var result = await passwordSetupService.ValidateInitialUserAsync(user.Id);

        Assert.False(result.IsValid);
        Assert.Equal("Initial password setup is only available for demo users.", result.ErrorMessage);
    }

    [Fact]
    public async Task SetInitialPasswordAsyncWhenUserIsNotDemoDomainShouldFail()
    {
        await using var databaseContext = CreateDatabaseContext();
        var user = new User("Regular User", "user@example.com", "recovery@example.com", "password-hash")
        {
            PasswordHash = null,
        };

        databaseContext.Users.Add(user);
        await databaseContext.SaveChangesAsync();

        var passwordSetupService = new PasswordSetupService(
            databaseContext,
            new TickfloConfig { SessionTimeoutMinutes = 20 },
            new Argon2idPasswordHasher());

        var result = await passwordSetupService.SetInitialPasswordAsync(user.Id, "regular-password");

        Assert.False(result.Success);
        Assert.Equal("Initial password setup is only available for demo users.", result.ErrorMessage);

        var persistedUser = await databaseContext.Users.FindAsync(user.Id);
        Assert.Null(persistedUser?.PasswordHash);
    }

    private static TickfloDbContext CreateDatabaseContext()
    {
        var options = new DbContextOptionsBuilder<TickfloDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new TickfloDbContext(options);
    }
}
