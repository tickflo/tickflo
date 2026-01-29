namespace Tickflo.Core.Services.Views;

using Tickflo.Core.Services.Workspace;

public class WorkspaceReportRunsBackfillViewData
{
    public bool CanEditReports { get; set; }
}

public interface IWorkspaceReportRunsBackfillViewService
{
    public Task<WorkspaceReportRunsBackfillViewData> BuildAsync(int workspaceId, int userId);
}


public class WorkspaceReportRunsBackfillViewService(IWorkspaceAccessService workspaceAccessService) : IWorkspaceReportRunsBackfillViewService
{
    private readonly IWorkspaceAccessService workspaceAccessService = workspaceAccessService;

    public async Task<WorkspaceReportRunsBackfillViewData> BuildAsync(int workspaceId, int userId)
    {
        var data = new WorkspaceReportRunsBackfillViewData();
        var isAdmin = await this.workspaceAccessService.UserIsWorkspaceAdminAsync(userId, workspaceId);
        var permissions = await this.workspaceAccessService.GetUserPermissionsAsync(workspaceId, userId);
        data.CanEditReports = isAdmin || (permissions.TryGetValue("reports", out var rp) && rp.CanEdit);
        return data;
    }
}


