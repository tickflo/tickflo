namespace Tickflo.Core.Services.Views;

using Tickflo.Core.Services.Workspace;

public class WorkspaceUsersManageViewData
{
    public bool CanEditUsers { get; set; }
}

public interface IWorkspaceUsersManageViewService
{
    public Task<WorkspaceUsersManageViewData> BuildAsync(int workspaceId, int userId);
}


public class WorkspaceUsersManageViewService(IWorkspaceAccessService workspaceAccessService) : IWorkspaceUsersManageViewService
{
    private readonly IWorkspaceAccessService workspaceAccessService = workspaceAccessService;

    public async Task<WorkspaceUsersManageViewData> BuildAsync(int workspaceId, int userId)
    {
        var data = new WorkspaceUsersManageViewData();

        var isAdmin = await this.workspaceAccessService.UserIsWorkspaceAdminAsync(userId, workspaceId);
        if (isAdmin)
        {
            data.CanEditUsers = true;
        }
        else
        {
            var permissions = await this.workspaceAccessService.GetUserPermissionsAsync(workspaceId, userId);
            data.CanEditUsers = permissions.TryGetValue("users", out var up) && up.CanEdit;
        }

        return data;
    }
}


