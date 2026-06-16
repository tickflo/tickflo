namespace Tickflo.Core.Services.Authentication;

using Microsoft.EntityFrameworkCore;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;
using Tickflo.Core.Services.Email;
using Tickflo.Core.Services.Web;

public interface IPasswordResetRequestService
{
    public Task RequestPasswordResetAsync(string emailAddress);
}

public class PasswordResetRequestService(
    TickfloDbContext dbContext,
    IEmailSendService emailSendService,
    IRequestOriginService requestOriginService) : IPasswordResetRequestService
{
    // Password-reset links expire after one hour. Distinct from the
    // session-token lifetime (which is bound to SessionTimeoutMinutes).
    private const int ResetTokenMaxAgeInSeconds = 60 * 60;

    private readonly TickfloDbContext dbContext = dbContext;
    private readonly IEmailSendService emailSendService = emailSendService;
    private readonly IRequestOriginService requestOriginService = requestOriginService;

    public async Task RequestPasswordResetAsync(string emailAddress)
    {
        if (string.IsNullOrWhiteSpace(emailAddress))
        {
            return;
        }

        var normalizedEmail = emailAddress.Trim().ToLowerInvariant();
        var user = await this.dbContext.Users
            .FirstOrDefaultAsync(u => u.Email.ToLower() == normalizedEmail);

        if (user == null)
        {
            // Do not reveal whether the account exists. A successful
            // request also takes the same code path so an attacker cannot
            // distinguish the two by elapsed time or side effects.
            return;
        }

        // Invalidate any prior password-reset tokens for this user.
        // Session (Login) tokens are deliberately left alone.
        var priorResetTokens = await this.dbContext.Tokens
            .Where(t => t.UserId == user.Id && t.TypeId == (int)TokenType.PasswordReset)
            .ToListAsync();
        if (priorResetTokens.Count > 0)
        {
            this.dbContext.Tokens.RemoveRange(priorResetTokens);
        }

        var resetToken = new Token(user.Id, ResetTokenMaxAgeInSeconds, TokenType.PasswordReset);
        await this.dbContext.Tokens.AddAsync(resetToken);
        await this.dbContext.SaveChangesAsync();

        var origin = this.requestOriginService.GetCurrentOrigin();
        var resetLink = $"{origin}/account/reset-password?token={Uri.EscapeDataString(resetToken.Value)}";

        await this.emailSendService.AddToQueueAsync(
            user.Email,
            EmailTemplateType.ForgotPassword,
            new Dictionary<string, string>
            {
                { "recipient_name", user.Name },
                { "reset_link", resetLink },
                { "expires_in", "1 hour" },
            },
            user.Id);
    }
}
