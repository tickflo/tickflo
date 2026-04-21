namespace Tickflo.CoreTest.Services.Auth;

using Microsoft.EntityFrameworkCore;
using Moq;
using Tickflo.Core.Config;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;
using Tickflo.Core.Exceptions;
using Tickflo.Core.Services.Authentication;
using Tickflo.Core.Services.Email;
using Tickflo.Core.Services.Web;
using Tickflo.Core.Services.Workspace;
using Xunit;

public class AuthenticationServiceAuthenticationTests
{
    [Fact]
    public async Task AuthenticateAsync_WhenDemoUserHasNoPassword_ShouldAllowLogin()
    {
        await using var databaseContext = CreateDatabaseContext();
        var user = new User("Demo User", "admin@demo.com", "recovery@example.com", "password-hash")
        {
            PasswordHash = null,
        };

        databaseContext.Users.Add(user);
        await databaseContext.SaveChangesAsync();

        var authenticationService = CreateAuthenticationService(databaseContext);

        var result = await authenticationService.AuthenticateAsync(user.Email, string.Empty);

        Assert.Equal(user.Id, result.UserId);
        Assert.False(string.IsNullOrWhiteSpace(result.Token));
    }

    [Fact]
    public async Task AuthenticateAsync_WhenDemoUserPasswordIsWrong_ShouldAllowLogin()
    {
        await using var databaseContext = CreateDatabaseContext();
        var passwordHasher = new Argon2idPasswordHasher();
        var user = new User(
            "Demo User",
            "admin@demo.com",
            "recovery@example.com",
            passwordHasher.Hash("admin@demo.comcorrect-password"));

        databaseContext.Users.Add(user);
        await databaseContext.SaveChangesAsync();

        var authenticationService = CreateAuthenticationService(databaseContext);

        var result = await authenticationService.AuthenticateAsync(user.Email, "wrong-password");

        Assert.Equal(user.Id, result.UserId);
        Assert.False(string.IsNullOrWhiteSpace(result.Token));
    }

    [Fact]
    public async Task AuthenticateAsync_WhenNonDemoUserPasswordIsWrong_ShouldRejectLogin()
    {
        await using var databaseContext = CreateDatabaseContext();
        var passwordHasher = new Argon2idPasswordHasher();
        var user = new User(
            "Regular User",
            "user@example.com",
            "recovery@example.com",
            passwordHasher.Hash("user@example.comcorrect-password"));

        databaseContext.Users.Add(user);
        await databaseContext.SaveChangesAsync();

        var authenticationService = CreateAuthenticationService(databaseContext);

        await Assert.ThrowsAsync<UnauthorizedException>(() =>
            authenticationService.AuthenticateAsync(user.Email, "wrong-password"));
    }

    [Fact]
    public async Task AuthenticateAsync_WhenEmailOnlyContainsDemoDomainText_ShouldRejectInvalidPassword()
    {
        await using var databaseContext = CreateDatabaseContext();
        var passwordHasher = new Argon2idPasswordHasher();
        var user = new User(
            "Regular User",
            "attacker@notdemo.com",
            "recovery@example.com",
            passwordHasher.Hash("attacker@notdemo.comcorrect-password"));

        databaseContext.Users.Add(user);
        await databaseContext.SaveChangesAsync();

        var authenticationService = CreateAuthenticationService(databaseContext);

        await Assert.ThrowsAsync<UnauthorizedException>(() =>
            authenticationService.AuthenticateAsync(user.Email, "wrong-password"));
    }

    private static AuthenticationService CreateAuthenticationService(TickfloDbContext databaseContext)
    {
        var tickfloConfig = new TickfloConfig
        {
            BaseUrl = "https://app.tickflo.co",
            SessionTimeoutMinutes = 20,
        };

        return new AuthenticationService(
            databaseContext,
            new Argon2idPasswordHasher(),
            Mock.Of<IEmailSendService>(),
            tickfloConfig,
            Mock.Of<IWorkspaceCreationService>(),
            Mock.Of<IRequestOriginService>());
    }

    private static TickfloDbContext CreateDatabaseContext()
    {
        var options = new DbContextOptionsBuilder<TickfloDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new TickfloDbContext(options);
    }
}
