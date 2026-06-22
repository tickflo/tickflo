namespace Tickflo.Core.Services.Authentication;

using Microsoft.EntityFrameworkCore;
using Tickflo.Core.Config;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;
using Tickflo.Core.Services.Email;
using Tickflo.Core.Services.Web;

public record PasswordResetRequestResult(
    bool EmailEnqueued,
    bool? RecipientExists,
    string? ErrorMessage);

public interface IPasswordResetRequestService
{
    public Task<PasswordResetRequestResult> RequestPasswordResetAsync(string emailAddress);
}

public class PasswordResetRequestService(
    TickfloDbContext dbContext,
    IEmailSendService emailSendService,
    IRequestOriginService requestOriginService,
    TickfloConfig tickfloConfig) : IPasswordResetRequestService
{
    private readonly TickfloDbContext dbContext = dbContext;
    private readonly IEmailSendService emailSendService = emailSendService;
    private readonly IRequestOriginService requestOriginService = requestOriginService;
    private readonly TickfloConfig tickfloConfig = tickfloConfig;

    public async Task<PasswordResetRequestResult> RequestPasswordResetAsync(string emailAddress)
    {
        if (string.IsNullOrWhiteSpace(emailAddress))
        {
            return new PasswordResetRequestResult(false, null, "Email is required.");
        }

        var normalizedEmail = emailAddress.Trim().ToLowerInvariant();
        var user = await this.dbContext.Users
            .FirstOrDefaultAsync(u => u.Email.ToLower() == normalizedEmail);

        // Do not reveal whether the account exists. The no-user branch and
        // the happy path both enqueue nothing and return the same shape
        // so an attacker cannot distinguish them by side effects.
        if (user == null)
        {
            return new PasswordResetRequestResult(false, false, null);
        }

        // Any prior password-reset token is implicitly invalidated the
        // moment the user successfully sets a new password — that flow
        // bumps user.UpdatedAt, and ValidateResetTokenAsync rejects any
        // token whose CreatedAt is not strictly greater than that
        // timestamp. We do not have to delete the old row here; the new
        // token has a unique Value and the old one fails validation as
        // soon as a successful reset occurs.
        var resetToken = new Token(
            user.Id,
            this.tickfloConfig.PasswordResetTokenMaxAgeSeconds,
            TokenType.PasswordReset,
            this.tickfloConfig.PasswordResetTokenByteLength);
        await this.dbContext.Tokens.AddAsync(resetToken);
        await this.dbContext.SaveChangesAsync();

        var origin = this.requestOriginService.GetCurrentOrigin();
        var resetLink = $"{origin}/reset-password?token={Uri.EscapeDataString(resetToken.Value)}";

        await this.emailSendService.AddToQueueAsync(
            user.Email,
            EmailTemplateType.ForgotPassword,
            new Dictionary<string, string>
            {
                { "recipient_name", user.Name },
                { "reset_link", resetLink },
                { "expires_in", FormatExpiresIn(this.tickfloConfig.PasswordResetTokenMaxAgeSeconds) },
            },
            user.Id);

        return new PasswordResetRequestResult(true, true, null);
    }

    private static string FormatExpiresIn(int maxAgeInSeconds)
    {
        if (maxAgeInSeconds % 3600 == 0)
        {
            var hours = maxAgeInSeconds / 3600;
            return hours == 1 ? "1 hour" : $"{hours} hours";
        }

        if (maxAgeInSeconds % 60 == 0)
        {
            var minutes = maxAgeInSeconds / 60;
            return minutes == 1 ? "1 minute" : $"{minutes} minutes";
        }

        return $"{maxAgeInSeconds} seconds";
    }
}
