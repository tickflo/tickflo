namespace Tickflo.Core.Services.Views;

using Tickflo.Core.Services.Workspace;

public class WorkspaceUsersInviteViewData
{
    public bool CanViewUsers { get; set; }
    public bool CanCreateUsers { get; set; }
}

public interface IWorkspaceUsersInviteViewService
{
    public Task<WorkspaceUsersInviteViewData> BuildAsync(int workspaceId, int userId);
}


public class WorkspaceUsersInviteViewService(IWorkspaceAccessService workspaceAccessService) : IWorkspaceUsersInviteViewService
{
    private readonly IWorkspaceAccessService workspaceAccessService = workspaceAccessService;

    public async Task<WorkspaceUsersInviteViewData> BuildAsync(int workspaceId, int userId)
    {
        var data = new WorkspaceUsersInviteViewData();

        var isAdmin = await this.workspaceAccessService.UserIsWorkspaceAdminAsync(userId, workspaceId);
        var permissions = await this.workspaceAccessService.GetUserPermissionsAsync(workspaceId, userId);
        data.CanViewUsers = isAdmin || (permissions.TryGetValue("users", out var up) && up.CanView);
        data.CanCreateUsers = isAdmin || (permissions.TryGetValue("users", out var up2) && up2.CanCreate);

        return data;
    }
}


