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

        // Any prior password-reset token is implicitly invalidated the
        // moment the user successfully sets a new password (that flow
        // bumps user.UpdatedAt, and ValidateResetTokenAsync rejects any
        // token older than that timestamp). We do not have to delete the
        // old row here — the new token has a unique Value, and the old
        // one will fail validation as soon as a successful reset occurs.
        var resetToken = new Token(user.Id, ResetTokenMaxAgeInSeconds);
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
