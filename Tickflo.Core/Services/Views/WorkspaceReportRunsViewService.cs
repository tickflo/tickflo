namespace Tickflo.Core.Services.Views;

using Tickflo.Core.Entities;
using Tickflo.Core.Services.Reporting;
using Tickflo.Core.Services.Workspace;

public class WorkspaceReportRunsViewData
{
    public bool CanViewReports { get; set; }
    public Report? Report { get; set; }
    public List<ReportRun> Runs { get; set; } = [];
}

public interface IWorkspaceReportRunsViewService
{
    public Task<WorkspaceReportRunsViewData> BuildAsync(int workspaceId, int userId, int reportId);
}


public class WorkspaceReportRunsViewService(
    IWorkspaceAccessService workspaceAccessService,
    IReportRunService reportRunService) : IWorkspaceReportRunsViewService
{
    private readonly IWorkspaceAccessService workspaceAccessService = workspaceAccessService;
    private readonly IReportRunService reportRunService = reportRunService;

    public async Task<WorkspaceReportRunsViewData> BuildAsync(int workspaceId, int userId, int reportId)
    {
        var data = new WorkspaceReportRunsViewData();

        var isAdmin = await this.workspaceAccessService.UserIsWorkspaceAdminAsync(userId, workspaceId);
        var permissions = await this.workspaceAccessService.GetUserPermissionsAsync(workspaceId, userId);
        data.CanViewReports = isAdmin || (permissions.TryGetValue("reports", out var rp) && rp.CanView);

        if (!data.CanViewReports)
        {
            return data;
        }

        var (report, runs) = await this.reportRunService.GetReportRunsAsync(workspaceId, reportId, 100);
        if (report != null)
        {
            data.Report = report;
            data.Runs = [.. runs];
        }

        return data;
    }
}



