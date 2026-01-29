namespace Tickflo.Core.Services.Views;

using Tickflo.Core.Services.Workspace;

public class WorkspaceReportDeleteViewData
{
    public bool CanEditReports { get; set; }
}

public interface IWorkspaceReportDeleteViewService
{
    public Task<WorkspaceReportDeleteViewData> BuildAsync(int workspaceId, int userId);
}


public class WorkspaceReportDeleteViewService(IWorkspaceAccessService workspaceAccessService) : IWorkspaceReportDeleteViewService
{
    private readonly IWorkspaceAccessService workspaceAccessService = workspaceAccessService;

    public async Task<WorkspaceReportDeleteViewData> BuildAsync(int workspaceId, int userId)
    {
        var data = new WorkspaceReportDeleteViewData();
        var isAdmin = await this.workspaceAccessService.UserIsWorkspaceAdminAsync(userId, workspaceId);
        var permissions = await this.workspaceAccessService.GetUserPermissionsAsync(workspaceId, userId);
        data.CanEditReports = isAdmin || (permissions.TryGetValue("reports", out var rp) && rp.CanEdit);
        return data;
    }
}


