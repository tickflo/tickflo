namespace Tickflo.Core.Services.Views;

using Tickflo.Core.Entities;
using Tickflo.Core.Services.Reporting;
using Tickflo.Core.Services.Workspace;

public class WorkspaceReportRunDownloadViewData
{
    public bool CanViewReports { get; set; }
    public ReportRun? Run { get; set; }
}

public interface IWorkspaceReportRunDownloadViewService
{
    public Task<WorkspaceReportRunDownloadViewData> BuildAsync(int workspaceId, int userId, int reportId, int runId);
}


public class WorkspaceReportRunDownloadViewService(
    IWorkspaceAccessService workspaceAccessService,
    IReportRunService reportRunService) : IWorkspaceReportRunDownloadViewService
{
    private readonly IWorkspaceAccessService workspaceAccessService = workspaceAccessService;
    private readonly IReportRunService reportRunService = reportRunService;

    public async Task<WorkspaceReportRunDownloadViewData> BuildAsync(int workspaceId, int userId, int reportId, int runId)
    {
        var data = new WorkspaceReportRunDownloadViewData();
        var isAdmin = await this.workspaceAccessService.UserIsWorkspaceAdminAsync(userId, workspaceId);
        var permissions = await this.workspaceAccessService.GetUserPermissionsAsync(workspaceId, userId);
        data.CanViewReports = isAdmin || (permissions.TryGetValue("reports", out var rp) && rp.CanView);
        if (!data.CanViewReports)
        {
            return data;
        }

        var run = await this.reportRunService.GetRunAsync(workspaceId, runId);
        if (run == null || run.ReportId != reportId)
        {
            return data;
        }

        data.Run = run;
        return data;
    }
}



