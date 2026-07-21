namespace Tickflo.Core.Services.Authentication;

using Microsoft.EntityFrameworkCore;
using Tickflo.Core.Config;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;
using Tickflo.Core.Exceptions;
using Tickflo.Core.Services.Email;
using Tickflo.Core.Services.Web;
using Tickflo.Core.Utils;

public interface IPasswordResetRequestService
{
    public Task RequestPasswordResetAsync(string emailAddress);
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

    public async Task RequestPasswordResetAsync(string emailAddress)
    {
        if (string.IsNullOrWhiteSpace(emailAddress))
        {
            throw new BadRequestException("Email is required.");
        }

        var normalizedEmail = emailAddress.Trim().ToLowerInvariant();
        var user = await this.dbContext.Users
            .FirstOrDefaultAsync(u => u.Email.ToLower() == normalizedEmail);

        // Do not reveal whether the account exists. The no-user branch
        // silently returns so an attacker cannot distinguish it from a
        // happy path by side effects.
        if (user == null)
        {
            return;
        }

        var resetToken = new Token(
            user.Id,
            this.tickfloConfig.PasswordResetTokenMaxAgeSeconds,
            TokenType.PasswordReset);
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
                { "expires_in", DurationFormatter.FormatExpiresIn(this.tickfloConfig.PasswordResetTokenMaxAgeSeconds) },
            },
            user.Id);
    }
}
