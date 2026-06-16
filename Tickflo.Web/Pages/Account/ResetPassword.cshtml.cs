namespace Tickflo.Web.Pages.Account;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Tickflo.Core.Config;
using Tickflo.Core.Services.Authentication;

public class ResetPasswordModel(
    IPasswordSetupService passwordSetupService,
    TickfloConfig config) : PageModel
{
    private readonly IPasswordSetupService passwordSetupService = passwordSetupService;
    private readonly TickfloConfig config = config;

    [BindProperty(SupportsGet = true)]
    public string? Token { get; set; }

    [BindProperty]
    public string Password { get; set; } = string.Empty;

    [BindProperty]
    public string ConfirmPassword { get; set; } = string.Empty;

    public bool IsTokenValid { get; private set; }

    public string? MaskedEmail { get; private set; }

    public string? ErrorMessage { get; private set; }

    public async Task<IActionResult> OnGetAsync()
    {
        if (string.IsNullOrWhiteSpace(this.Token))
        {
            this.IsTokenValid = false;
            return this.Page();
        }

        var validation = await this.passwordSetupService.ValidateResetTokenAsync(this.Token);
        this.IsTokenValid = validation.IsValid;
        if (this.IsTokenValid && !string.IsNullOrEmpty(validation.UserEmail))
        {
            this.MaskedEmail = MaskEmail(validation.UserEmail);
        }

        return this.Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (string.IsNullOrWhiteSpace(this.Token))
        {
            this.IsTokenValid = false;
            return this.Page();
        }

        if (string.IsNullOrWhiteSpace(this.Password) || this.Password.Length < 8)
        {
            this.ModelState.AddModelError(nameof(this.Password), "Password must be at least 8 characters long.");
        }

        if (!string.Equals(this.Password, this.ConfirmPassword, StringComparison.Ordinal))
        {
            this.ModelState.AddModelError(nameof(this.ConfirmPassword), "Passwords do not match.");
        }

        if (!this.ModelState.IsValid)
        {
            this.IsTokenValid = true;
            var revalidation = await this.passwordSetupService.ValidateResetTokenAsync(this.Token);
            if (revalidation.IsValid && !string.IsNullOrEmpty(revalidation.UserEmail))
            {
                this.MaskedEmail = MaskEmail(revalidation.UserEmail);
            }
            return this.Page();
        }

        var result = await this.passwordSetupService.SetPasswordWithTokenAsync(this.Token, this.Password);
        if (!result.Success || string.IsNullOrEmpty(result.LoginToken))
        {
            this.ErrorMessage = result.ErrorMessage ?? "We couldn't reset your password. Please request a new link.";
            this.IsTokenValid = false;
            return this.Page();
        }

        this.Response.Cookies.Append(this.config.SessionCookieName, result.LoginToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = this.Request.IsHttps,
            SameSite = SameSiteMode.Lax,
            Expires = DateTimeOffset.UtcNow.AddMinutes(this.config.SessionTimeoutMinutes),
        });

        if (!string.IsNullOrEmpty(result.WorkspaceSlug))
        {
            return this.Redirect($"/workspaces/{result.WorkspaceSlug}");
        }

        return this.Redirect("/");
    }

    private static string MaskEmail(string email)
    {
        var at = email.IndexOf('@');
        if (at <= 1)
        {
            return email;
        }

        var local = email[..at];
        var domain = email[at..];
        var first = local[..1];
        var last = local.Length > 1 ? local[^1..] : string.Empty;
        return $"{first}{new string('*', Math.Max(local.Length - 2, 1))}{last}{domain}";
    }
}
