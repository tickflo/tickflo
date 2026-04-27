namespace Tickflo.Web.Pages.Admin;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Tickflo.Core.Services.Admin;

[Authorize]
public class ResetDemoDataModel(IDemoDataSeeder demoDataSeeder) : PageModel
{
    private readonly IDemoDataSeeder demoDataSeeder = demoDataSeeder;

    public IActionResult OnGet([FromServices] IAppContext appContext)
    {
        var user = appContext.CurrentUser;
        if (user == null || !user.SystemAdmin)
        {
            return this.Forbid();
        }

        return this.Page();
    }

    public async Task<IActionResult> OnPostAsync([FromServices] IAppContext appContext)
    {
        var user = appContext.CurrentUser;
        if (user == null || !user.SystemAdmin)
        {
            return this.Forbid();
        }

        await this.demoDataSeeder.ResetDemoDataAsync();

        this.TempData["SuccessMessage"] = "Demo data has been reset successfully.";
        return this.RedirectToPage();
    }
}
