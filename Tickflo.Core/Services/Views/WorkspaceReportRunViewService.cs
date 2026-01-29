namespace Tickflo.Core.Services.Views;

using Microsoft.EntityFrameworkCore;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;
using Tickflo.Core.Services.Reporting;
using Tickflo.Core.Services.Workspace;

public class ReportRunPageData
{
    public int Page { get; set; }
    public int Take { get; set; }
    public int TotalRows { get; set; }
    public int TotalPages { get; set; }
    public int FromRow { get; set; }
    public int ToRow { get; set; }
    public bool HasContent { get; set; }
    public List<string> Headers { get; set; } = [];
    public List<List<string>> Rows { get; set; } = [];
}

public class WorkspaceReportRunViewData
{
    public bool CanViewReports { get; set; }
    public Report? Report { get; set; }
    public ReportRun? Run { get; set; }
    public ReportRunPageData? PageData { get; set; }
}

public interface IWorkspaceReportRunViewService
{
    public Task<WorkspaceReportRunViewData> BuildAsync(int workspaceId, int userId, int reportId, int runId, int page, int take);
}


public class WorkspaceReportRunViewService(
    TickfloDbContext dbContext,
    IWorkspaceAccessService workspaceAccessService,
    IReportingService reportingService) : IWorkspaceReportRunViewService
{
    private readonly TickfloDbContext dbContext = dbContext;
    private readonly IWorkspaceAccessService workspaceAccessService = workspaceAccessService;
    private readonly IReportingService reportingService = reportingService;

    public async Task<WorkspaceReportRunViewData> BuildAsync(int workspaceId, int userId, int reportId, int runId, int page, int take)
    {
        var data = new WorkspaceReportRunViewData();

        var isAdmin = await this.workspaceAccessService.UserIsWorkspaceAdminAsync(userId, workspaceId);
        var permissions = await this.workspaceAccessService.GetUserPermissionsAsync(workspaceId, userId);
        data.CanViewReports = isAdmin || (permissions.TryGetValue("reports", out var rp) && rp.CanView);
        if (!data.CanViewReports)
        {
            return data;
        }

        var rep = await this.dbContext.Reports
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.WorkspaceId == workspaceId && r.Id == reportId);
        if (rep == null)
        {
            return data;
        }

        data.Report = rep;

        var run = await this.dbContext.ReportRuns
            .AsNoTracking()
            .FirstOrDefaultAsync(rr => rr.WorkspaceId == workspaceId && rr.Id == runId);
        if (run == null || run.ReportId != reportId)
        {
            return data;
        }

        data.Run = run;

        var pageResult = await this.reportingService.GetRunPageAsync(run, page, take);
        data.PageData = new ReportRunPageData
        {
            Page = pageResult.Page,
            Take = pageResult.Take,
            TotalRows = pageResult.TotalRows,
            TotalPages = pageResult.TotalPages,
            FromRow = pageResult.FromRow,
            ToRow = pageResult.ToRow,
            HasContent = pageResult.HasContent,
            Headers = [.. pageResult.Headers],
            Rows = [.. pageResult.Rows.Select(r => r.ToList())]
        };

        return data;
    }
}



