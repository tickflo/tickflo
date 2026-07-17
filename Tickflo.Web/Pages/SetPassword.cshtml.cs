namespace Tickflo.Web.Pages;

using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Tickflo.Core.Config;
using Tickflo.Core.Exceptions;
using Tickflo.Core.Services.Authentication;

[AllowAnonymous]
public class SetPasswordModel(
    IPasswordSetupService passwordSetupService,
    TickfloConfig config
    ) : PageModel
{
    private readonly IPasswordSetupService passwordSetupService = passwordSetupService;
    private readonly TickfloConfig config = config;

    [BindProperty]
    public SetPasswordInput Input { get; set; } = new();

    [FromQuery]
    public int UserId { get; set; }

    public string? ErrorMessage { get; set; }
    public string? UserEmail { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        try
        {
            var (_, userEmail) = await this.passwordSetupService.ValidateInitialUserAsync(this.UserId);
            this.UserEmail = userEmail;
            return this.Page();
        }
        catch (Exception)
        {
            return this.Redirect("/login");
        }
    }

    public async Task<IActionResult> OnPostAsync()
    {
        try
        {
            var (userId, userEmail) = await this.passwordSetupService.ValidateInitialUserAsync(this.UserId);
            this.UserEmail = userEmail;

            if (!this.ModelState.IsValid)
            {
                return this.Page();
            }

            var result = await this.passwordSetupService.SetInitialPasswordAsync(userId, this.Input.Password);

            this.Response.Cookies.Append(this.config.SessionCookieName, result.LoginToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = this.Request.IsHttps,
                SameSite = SameSiteMode.Lax,
                Expires = DateTimeOffset.UtcNow.AddMinutes(this.config.SessionTimeoutMinutes)
            });

            if (!string.IsNullOrWhiteSpace(result.WorkspaceSlug))
            {
                return this.Redirect($"/workspaces/{result.WorkspaceSlug}");
            }

            return this.Redirect("/");
        }
        catch (BadRequestException ex)
        {
            this.ErrorMessage = ex.Message;
            return this.Page();
        }
    }
}

public class SetPasswordInput
{
    [Required]
    [DataType(DataType.Password)]
    [Display(Name = "New Password")]
    [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 8)]
    public string Password { get; set; } = "";

    [Required]
    [DataType(DataType.Password)]
    [Display(Name = "Confirm Password")]
    [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
    public string ConfirmPassword { get; set; } = "";
}
