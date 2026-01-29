namespace Tickflo.Web.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Tickflo.Core.Data;
using Tickflo.Core.Services.Common;
using Tickflo.Core.Services.Roles;
using Tickflo.Core.Services.Workspace;

// TODO: This should NOT be using TickfloDbContext directly. The logic on this page/controller needs moved into a Tickflo.Core service

[Authorize]
[Route("workspaces/{slug}/users/roles/{id:int}")]
public class RolesController(
    TickfloDbContext dbContext,
    ICurrentUserService currentUserService,
    IWorkspaceAccessService workspaceAccessService,
    IRoleManagementService roleManagementService) : Controller
{
    private readonly TickfloDbContext dbContext = dbContext;
    private readonly ICurrentUserService currentUserService = currentUserService;
    private readonly IWorkspaceAccessService workspaceAccessService = workspaceAccessService;
    private readonly IRoleManagementService roleManagementService = roleManagementService;

    [HttpPost("delete")]
    public async Task<IActionResult> Delete(string slug, int id)
    {
        var workspace = await this.dbContext.Workspaces.FirstOrDefaultAsync(w => w.Slug.Equals(slug, StringComparison.OrdinalIgnoreCase));
        if (workspace == null)
        {
            return this.NotFound();
        }

        if (!this.currentUserService.TryGetUserId(this.User, out var uid))
        {
            return this.Unauthorized();
        }

        var isAdmin = await this.workspaceAccessService.UserIsWorkspaceAdminAsync(uid, workspace.Id);
        if (!isAdmin)
        {
            return this.Forbid();
        }

        var role = await this.dbContext.Roles.FindAsync(id);
        if (role == null || role.WorkspaceId != workspace.Id)
        {
            return this.NotFound();
        }

        // Use service to check if role can be deleted (guard against assignments)
        try
        {
            await this.roleManagementService.EnsureRoleCanBeDeletedAsync(workspace.Id, id, role.Name);
        }
        catch (InvalidOperationException ex)
        {
            this.TempData["Error"] = ex.Message;
            return this.Redirect($"/workspaces/{slug}/roles");
        }

        this.dbContext.Roles.Remove(role);
        await this.dbContext.SaveChangesAsync();
        return this.Redirect($"/workspaces/{slug}/roles");
    }
}

