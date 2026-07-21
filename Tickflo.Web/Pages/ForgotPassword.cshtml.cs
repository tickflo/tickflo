namespace Tickflo.Web.Pages;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Tickflo.Core.Config;
using Tickflo.Core.Services.Authentication;
using Tickflo.Core.Utils;

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

    public string ExpiresIn => DurationFormatter.FormatExpiresIn(this.tickfloConfig.PasswordResetTokenMaxAgeSeconds);

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

}
