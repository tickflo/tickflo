namespace Tickflo.Core.Services.Authentication;

using Microsoft.EntityFrameworkCore;
using Tickflo.Core.Config;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;
using Tickflo.Core.Exceptions;

public record PasswordSetResult(string LoginToken, string? WorkspaceSlug, int UserId, string? UserEmail);

public interface IPasswordSetupService
{
    public Task<(int UserId, string UserEmail)> ValidateResetTokenAsync(string tokenValue);
    public Task<(int UserId, string UserEmail)> ValidateInitialUserAsync(int userId);
    public Task<PasswordSetResult> SetPasswordWithTokenAsync(string tokenValue, string newPassword);
    public Task<PasswordSetResult> SetInitialPasswordAsync(int userId, string newPassword);
}

public class PasswordSetupService(
    TickfloDbContext dbContext,
    TickfloConfig tickfloConfig,
    IPasswordHasher passwordHasher,
    IPasswordValidationService passwordValidationService)
    : IPasswordSetupService
{
    private readonly TickfloDbContext dbContext = dbContext;
    private readonly TickfloConfig tickfloConfig = tickfloConfig;
    private readonly IPasswordHasher passwordHasher = passwordHasher;
    private readonly IPasswordValidationService passwordValidationService = passwordValidationService;

    public async Task<(int UserId, string UserEmail)> ValidateResetTokenAsync(string tokenValue)
    {
        if (string.IsNullOrWhiteSpace(tokenValue))
        {
            throw new BadRequestException("Missing token.");
        }

        var token = await this.dbContext.Tokens
            .FirstOrDefaultAsync(t => t.Value == tokenValue && t.TypeId == (int)TokenType.PasswordReset);
        if (token == null)
        {
            throw new BadRequestException("Invalid or expired token.");
        }

        if (DateTime.UtcNow > token.CreatedAt.AddSeconds(token.MaxAge))
        {
            throw new BadRequestException("Reset link has expired.");
        }

        var user = await this.dbContext.Users.FindAsync(token.UserId);
        if (user == null)
        {
            throw new BadRequestException("User not found.");
        }

        // A successful password reset bumps user.UpdatedAt (see
        // SetPasswordWithTokenAsync). If this token is older than that
        // bump, the user has already reset their password using some
        // other token, and this one is stale.
        if (user.UpdatedAt.HasValue && token.CreatedAt <= user.UpdatedAt.Value)
        {
            throw new BadRequestException("Reset link has already been used.");
        }

        return (user.Id, user.Email);
    }

    public async Task<(int UserId, string UserEmail)> ValidateInitialUserAsync(int userId)
    {
        if (userId <= 0)
        {
            throw new BadRequestException("Missing user id.");
        }

        var user = await this.dbContext.Users.FindAsync(userId);
        if (user == null)
        {
            throw new BadRequestException("User not found.");
        }

        if (user.PasswordHash != null)
        {
            return (user.Id, user.Email);
        }

        return (user.Id, user.Email);
    }

    public async Task<PasswordSetResult> SetPasswordWithTokenAsync(string tokenValue, string newPassword)
    {
        var (userId, userEmail) = await this.ValidateResetTokenAsync(tokenValue);

        // passwordValidationService.Validate throws BadRequestException on failure
        this.passwordValidationService.Validate(newPassword);

        var user = await this.dbContext.Users.FindAsync(userId);
        if (user == null)
        {
            throw new BadRequestException("User not found.");
        }

        // Re-fetch the token to avoid a stale read between validation and use.
        var token = await this.dbContext.Tokens
            .FirstOrDefaultAsync(t => t.Value == tokenValue && t.TypeId == (int)TokenType.PasswordReset);
        if (token == null)
        {
            throw new BadRequestException("Invalid or expired token.");
        }

        if (DateTime.UtcNow > token.CreatedAt.AddSeconds(token.MaxAge))
        {
            throw new BadRequestException("Reset link has expired.");
        }

        if (user.UpdatedAt.HasValue && token.CreatedAt <= user.UpdatedAt.Value)
        {
            throw new BadRequestException("Reset link has already been used.");
        }

        var passwordHash = this.passwordHasher.Hash($"{user.Email}{newPassword}");
        user.PasswordHash = passwordHash;
        user.UpdatedAt = DateTime.UtcNow;
        this.dbContext.Users.Update(user);

        // Issue a fresh session token so the caller can log the user in.
        // The reset token itself is left in place: it is now stale because
        // user.UpdatedAt > token.CreatedAt, and the unique index on Value
        // means we never issue a colliding one.
        var sessionToken = new Token(
            user.Id,
            this.tickfloConfig.SessionTimeoutMinutes * 60,
            TokenType.Session);
        await this.dbContext.Tokens.AddAsync(sessionToken);

        string? workspaceSlug = null;
        var userWorkspace = await this.dbContext.UserWorkspaces.Include(w => w.Workspace).FirstOrDefaultAsync(w => w.UserId == user.Id && w.Accepted);
        if (userWorkspace != null)
        {
            workspaceSlug = userWorkspace.Workspace.Slug;
        }

        await this.dbContext.SaveChangesAsync();

        return new PasswordSetResult(sessionToken.Value, workspaceSlug, user.Id, user.Email);
    }

    public async Task<PasswordSetResult> SetInitialPasswordAsync(int userId, string newPassword)
    {
        var user = await this.dbContext.Users.FindAsync(userId);
        if (user == null)
        {
            throw new BadRequestException("User not found.");
        }

        if (user.PasswordHash != null)
        {
            throw new BadRequestException("Password already set.");
        }

        // passwordValidationService.Validate throws BadRequestException on failure
        this.passwordValidationService.Validate(newPassword);

        var passwordHash = this.passwordHasher.Hash($"{user.Email}{newPassword}");
        user.PasswordHash = passwordHash;
        user.UpdatedAt = DateTime.UtcNow;
        this.dbContext.Users.Update(user);

        var token = new Token(
            user.Id,
            this.tickfloConfig.SessionTimeoutMinutes * 60,
            TokenType.Session);
        await this.dbContext.Tokens.AddAsync(token);

        string? workspaceSlug = null;
        var userWorkspace = await this.dbContext.UserWorkspaces.Include(w => w.Workspace).FirstOrDefaultAsync(w => w.UserId == user.Id && w.Accepted);
        if (userWorkspace != null)
        {
            workspaceSlug = userWorkspace.Workspace.Slug;
        }

        await this.dbContext.SaveChangesAsync();

        return new PasswordSetResult(token.Value, workspaceSlug, user.Id, user.Email);
    }
}
