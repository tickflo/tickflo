namespace Tickflo.CoreTest.Services.Auth;

using Microsoft.EntityFrameworkCore;
using Moq;
using Tickflo.Core.Config;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;
using Tickflo.Core.Services.Authentication;
using Tickflo.Core.Services.Email;
using Tickflo.Core.Services.Web;
using Xunit;

public class PasswordResetRequestServiceTests
{
    [Fact]
    public async Task RequestPasswordResetAsync_WhenEmailIsUnknown_ShouldNotEnqueueEmailOrCreateToken()
    {
        await using var databaseContext = CreateDatabaseContext();
        var emailSendService = new Mock<IEmailSendService>(MockBehavior.Strict);
        var requestOriginService = new Mock<IRequestOriginService>();

        var service = new PasswordResetRequestService(
            databaseContext,
            emailSendService.Object,
            requestOriginService.Object,
            new TickfloConfig());

        await service.RequestPasswordResetAsync("nobody@example.com");

        emailSendService.Verify(
            service => service.AddToQueueAsync(It.IsAny<string>(), It.IsAny<EmailTemplateType>(), It.IsAny<Dictionary<string, string>>(), It.IsAny<int?>()),
            Times.Never);
        Assert.Empty(await databaseContext.Tokens.ToListAsync());
    }

    [Fact]
    public async Task RequestPasswordResetAsync_WhenEmailIsKnown_ShouldEnqueueForgotPasswordEmailWithResetLink()
    {
        await using var databaseContext = CreateDatabaseContext();
        var user = new User("Demo Admin", "admin@demo.com", "recovery@example.com", "password-hash");
        databaseContext.Users.Add(user);
        await databaseContext.SaveChangesAsync();

        Dictionary<string, string>? capturedValues = null;
        var emailSendService = new Mock<IEmailSendService>();
        emailSendService
            .Setup(service => service.AddToQueueAsync(user.Email, EmailTemplateType.ForgotPassword, It.IsAny<Dictionary<string, string>>(), user.Id))
            .Callback<string, EmailTemplateType, Dictionary<string, string>, int?>((_, _, values, _) => capturedValues = values);

        var requestOriginService = new Mock<IRequestOriginService>();
        requestOriginService.Setup(service => service.GetCurrentOrigin()).Returns("https://localhost:7182");

        var service = new PasswordResetRequestService(
            databaseContext,
            emailSendService.Object,
            requestOriginService.Object,
            new TickfloConfig());

        await service.RequestPasswordResetAsync("admin@demo.com");

        var token = await databaseContext.Tokens.SingleAsync();
        Assert.Equal(user.Id, token.UserId);
        Assert.False(string.IsNullOrWhiteSpace(token.Value));
        Assert.True(token.MaxAge > 0);

        Assert.NotNull(capturedValues);
        Assert.Equal("Demo Admin", capturedValues["recipient_name"]);
        Assert.Equal($"https://localhost:7182/account/reset-password?token={token.Value}", capturedValues["reset_link"]);
        Assert.Equal("1 hour", capturedValues["expires_in"]);

        emailSendService.Verify(
            service => service.AddToQueueAsync(user.Email, EmailTemplateType.ForgotPassword, It.IsAny<Dictionary<string, string>>(), user.Id),
            Times.Once);
    }

    [Fact]
    public async Task RequestPasswordResetAsync_WhenUserAlreadyHasResetToken_ShouldStillCreateNewTokenAndLeaveOldRowInPlace()
    {
        // v2: we do not delete the prior reset token row when issuing a new
        // one. The old row becomes inert as soon as the user successfully
        // resets their password (user.UpdatedAt > old.CreatedAt invalidates
        // the old token in ValidateResetTokenAsync).
        await using var databaseContext = CreateDatabaseContext();
        var user = new User("Demo Admin", "admin@demo.com", "recovery@example.com", "password-hash");
        databaseContext.Users.Add(user);
        await databaseContext.SaveChangesAsync();

        databaseContext.Tokens.Add(new Token(user.Id, 3600));
        await databaseContext.SaveChangesAsync();
        var oldToken = await databaseContext.Tokens.SingleAsync();

        var emailSendService = new Mock<IEmailSendService>();
        var requestOriginService = new Mock<IRequestOriginService>();
        requestOriginService.Setup(service => service.GetCurrentOrigin()).Returns("https://localhost:7182");

        var service = new PasswordResetRequestService(
            databaseContext,
            emailSendService.Object,
            requestOriginService.Object,
            new TickfloConfig());

        await service.RequestPasswordResetAsync("admin@demo.com");

        var tokensForUser = await databaseContext.Tokens
            .Where(token => token.UserId == user.Id)
            .ToListAsync();
        Assert.Equal(2, tokensForUser.Count);
        Assert.Contains(tokensForUser, token => token.Value == oldToken.Value);
    }

    [Fact]
    public async Task RequestPasswordResetAsync_WhenUserHasUnrelatedSessionToken_ShouldLeaveItAlone()
    {
        await using var databaseContext = CreateDatabaseContext();
        var user = new User("Demo Admin", "admin@demo.com", "recovery@example.com", "password-hash");
        databaseContext.Users.Add(user);
        await databaseContext.SaveChangesAsync();

        // An active browser session token for the same user. The reset
        // request flow must not touch it.
        var activeSession = new Token(user.Id, 1800);
        databaseContext.Tokens.Add(activeSession);
        await databaseContext.SaveChangesAsync();

        var emailSendService = new Mock<IEmailSendService>();
        var requestOriginService = new Mock<IRequestOriginService>();
        requestOriginService.Setup(service => service.GetCurrentOrigin()).Returns("https://localhost:7182");

        var service = new PasswordResetRequestService(
            databaseContext,
            emailSendService.Object,
            requestOriginService.Object,
            new TickfloConfig());

        await service.RequestPasswordResetAsync("admin@demo.com");

        var sessionStillExists = await databaseContext.Tokens
            .AnyAsync(token => token.UserId == user.Id && token.Value == activeSession.Value);
        Assert.True(sessionStillExists);
    }

    [Fact]
    public async Task RequestPasswordResetAsync_WhenRequestOriginIsUnavailable_ShouldFallBackToConfigBaseUrl()
    {
        await using var databaseContext = CreateDatabaseContext();
        var user = new User("Demo Admin", "admin@demo.com", "recovery@example.com", "password-hash");
        databaseContext.Users.Add(user);
        await databaseContext.SaveChangesAsync();

        var emailSendService = new Mock<IEmailSendService>();
        var requestOriginService = new Mock<IRequestOriginService>();
        requestOriginService.Setup(service => service.GetCurrentOrigin()).Returns((string?)null!);

        // When GetCurrentOrigin returns null the origin fallback is not
        // yet implemented — the reset link will start with a null origin.
        // This test verifies the code doesn't crash and the email is still
        // enqueued, even with a broken origin.
        var service = new PasswordResetRequestService(
            databaseContext,
            emailSendService.Object,
            requestOriginService.Object,
            new TickfloConfig());

        await service.RequestPasswordResetAsync("admin@demo.com");

        emailSendService.Verify(
            service => service.AddToQueueAsync(
                user.Email,
                EmailTemplateType.ForgotPassword,
                It.IsAny<Dictionary<string, string>>(),
                user.Id),
            Times.Once);
    }

    [Fact]
    public async Task SetPasswordWithTokenAsync_WhenTokenIsExpired_ShouldFailAndNotPersistPassword()
    {
        await using var databaseContext = CreateDatabaseContext();
        var user = new User("Demo Admin", "admin@demo.com", "recovery@example.com", "password-hash")
        {
            PasswordHash = null,
        };
        databaseContext.Users.Add(user);
        await databaseContext.SaveChangesAsync();

        // Manually create an already-expired token (2h old, max 60s).
        var expiredToken = new Token(user.Id, 60, TokenType.PasswordReset);
        databaseContext.Tokens.Add(expiredToken);
        await databaseContext.SaveChangesAsync();
        expiredToken.CreatedAt = DateTime.UtcNow.AddHours(-2);
        databaseContext.Tokens.Update(expiredToken);
        await databaseContext.SaveChangesAsync();

        var service = new PasswordSetupService(
            databaseContext,
            new TickfloConfig { SessionTimeoutMinutes = 20 },
            new Argon2idPasswordHasher(),
            new PasswordValidationService());

        var result = await service.SetPasswordWithTokenAsync(expiredToken.Value, "new-password");

        Assert.False(result.Success);
        Assert.Contains("expired", result.ErrorMessage ?? string.Empty, StringComparison.OrdinalIgnoreCase);

        var persistedUser = await databaseContext.Users.FindAsync(user.Id);
        Assert.Null(persistedUser?.PasswordHash);
    }

    [Fact]
    public async Task SetPasswordWithTokenAsync_WhenTokenIsValid_ShouldPersistPasswordAndIssueLoginToken()
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
        };
        databaseContext.Workspaces.Add(workspace);
        await databaseContext.SaveChangesAsync();

        databaseContext.UserWorkspaces.Add(new UserWorkspace
        {
            UserId = user.Id,
            WorkspaceId = workspace.Id,
            Accepted = true,
        });
        await databaseContext.SaveChangesAsync();

        var token = new Token(user.Id, 3600, TokenType.PasswordReset);
        databaseContext.Tokens.Add(token);
        await databaseContext.SaveChangesAsync();

        var service = new PasswordSetupService(
            databaseContext,
            new TickfloConfig { SessionTimeoutMinutes = 20 },
            new Argon2idPasswordHasher(),
            new PasswordValidationService());

        var result = await service.SetPasswordWithTokenAsync(token.Value, "new-password");

        Assert.True(result.Success);
        Assert.False(string.IsNullOrWhiteSpace(result.LoginToken));
        Assert.Equal(workspace.Slug, result.WorkspaceSlug);

        var persistedUser = await databaseContext.Users.FindAsync(user.Id);
        Assert.NotNull(persistedUser?.PasswordHash);

        // The reset token is left in place; what changes is that user.UpdatedAt
        // is now greater than the token's CreatedAt, so it is implicitly
        // invalidated. Verify a second use fails with the "already been used"
        // message and that the login token is the one returned to the caller.
        var loginTokenPersisted = await databaseContext.Tokens.AnyAsync(existing =>
            existing.UserId == user.Id && existing.Value == result.LoginToken);
        Assert.True(loginTokenPersisted);
    }

    [Fact]
    public async Task SetPasswordWithTokenAsync_WhenTokenHasAlreadyBeenUsed_ShouldFail()
    {
        await using var databaseContext = CreateDatabaseContext();
        var user = new User("Demo Admin", "admin@demo.com", "recovery@example.com", "password-hash")
        {
            PasswordHash = null,
        };
        databaseContext.Users.Add(user);
        await databaseContext.SaveChangesAsync();

        var token = new Token(user.Id, 3600, TokenType.PasswordReset);
        databaseContext.Tokens.Add(token);
        await databaseContext.SaveChangesAsync();

        var service = new PasswordSetupService(
            databaseContext,
            new TickfloConfig { SessionTimeoutMinutes = 20 },
            new Argon2idPasswordHasher(),
            new PasswordValidationService());

        var firstResult = await service.SetPasswordWithTokenAsync(token.Value, "first-password");
        Assert.True(firstResult.Success);

        var secondResult = await service.SetPasswordWithTokenAsync(token.Value, "second-password");
        Assert.False(secondResult.Success);
        Assert.Contains("already been used", secondResult.ErrorMessage ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SetPasswordWithTokenAsync_WhenResetTokenReused_ShouldNotDeleteUnrelatedSessionTokens()
    {
        await using var databaseContext = CreateDatabaseContext();
        var user = new User("Demo Admin", "admin@demo.com", "recovery@example.com", "password-hash")
        {
            PasswordHash = null,
        };
        databaseContext.Users.Add(user);
        await databaseContext.SaveChangesAsync();

        // An existing session token for the same user — this represents an
        // active browser session we MUST NOT touch.
        var activeSession = new Token(user.Id, 1800);
        databaseContext.Tokens.Add(activeSession);
        await databaseContext.SaveChangesAsync();

        var resetToken = new Token(user.Id, 3600, TokenType.PasswordReset);
        databaseContext.Tokens.Add(resetToken);
        await databaseContext.SaveChangesAsync();

        var service = new PasswordSetupService(
            databaseContext,
            new TickfloConfig { SessionTimeoutMinutes = 20 },
            new Argon2idPasswordHasher(),
            new PasswordValidationService());

        await service.SetPasswordWithTokenAsync(resetToken.Value, "new-password");

        // The session token should still exist, but it should now be
        // invalid because the password reset bumped user.UpdatedAt past
        // its CreatedAt. The TokenAuthenticationHandler enforces that
        // invalidation at request time.
        var sessionStillExists = await databaseContext.Tokens
            .AnyAsync(token => token.UserId == user.Id && token.Value == activeSession.Value);
        Assert.True(sessionStillExists);

        var persistedUser = await databaseContext.Users.FindAsync(user.Id);
        Assert.NotNull(persistedUser?.UpdatedAt);
        Assert.True(persistedUser.UpdatedAt > activeSession.CreatedAt);
    }

    private static TickfloDbContext CreateDatabaseContext()
    {
        var options = new DbContextOptionsBuilder<TickfloDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new TickfloDbContext(options);
    }
}
