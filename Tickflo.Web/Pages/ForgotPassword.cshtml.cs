namespace Tickflo.Web.Pages;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Tickflo.Core.Config;
using Tickflo.Core.Services.Authentication;

public class ForgotPasswordModel(
    IPasswordResetRequestService passwordResetRequestService,
    TickfloConfig tickfloConfig) : PageModel
{
    private readonly IPasswordResetRequestService passwordResetRequestService = passwordResetRequestService;
    private readonly TickfloConfig tickfloConfig = tickfloConfig;

    [BindProperty]
    public string Email { get; set; } = string.Empty;

    public bool Submitted { get; private set; }

    public string? ErrorMessage { get; private set; }

    public string ExpiresIn => FormatExpiresIn(this.tickfloConfig.PasswordResetTokenMaxAgeSeconds);

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (string.IsNullOrWhiteSpace(this.Email))
        {
            this.ModelState.AddModelError(nameof(this.Email), "Email is required.");
            return this.Page();
        }

        try
        {
            await this.passwordResetRequestService.RequestPasswordResetAsync(this.Email);
            this.Submitted = true;
            return this.Page();
        }
        catch (Exception ex)
        {
            this.ErrorMessage = ex.Message;
            return this.Page();
        }
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
