namespace Tickflo.Web.Pages.Account;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Tickflo.Core.Services.Authentication;

public class ForgotPasswordModel(IPasswordResetRequestService passwordResetRequestService) : PageModel
{
    private readonly IPasswordResetRequestService passwordResetRequestService = passwordResetRequestService;

    [BindProperty]
    public string Email { get; set; } = string.Empty;

    public bool Submitted { get; private set; }

    public string? ErrorMessage { get; private set; }

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
        catch (System.Exception ex)
        {
            this.ErrorMessage = ex.Message;
            return this.Page();
        }
    }
}
