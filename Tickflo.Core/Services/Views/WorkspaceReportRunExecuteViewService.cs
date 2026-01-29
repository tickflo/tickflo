namespace Tickflo.Core.Services.Views;

using Tickflo.Core.Services.Workspace;

public class WorkspaceReportRunExecuteData
{
    public bool CanEditReports { get; set; }
}

public interface IWorkspaceReportRunExecuteViewService
{
    public Task<WorkspaceReportRunExecuteData> BuildAsync(int workspaceId, int userId);
}


public class WorkspaceReportRunExecuteViewService(IWorkspaceAccessService workspaceAccessService) : IWorkspaceReportRunExecuteViewService
{
    private readonly IWorkspaceAccessService workspaceAccessService = workspaceAccessService;

    public async Task<WorkspaceReportRunExecuteData> BuildAsync(int workspaceId, int userId)
    {
        var data = new WorkspaceReportRunExecuteData();
        var isAdmin = await this.workspaceAccessService.UserIsWorkspaceAdminAsync(userId, workspaceId);
        var permissions = await this.workspaceAccessService.GetUserPermissionsAsync(workspaceId, userId);
        data.CanEditReports = isAdmin || (permissions.TryGetValue("reports", out var rp) && rp.CanEdit);
        return data;
    }
}


