namespace Tickflo.Core.Services.Reporting;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;

public interface IReportRunService
{
    public Task<ReportRun?> RunReportAsync(int workspaceId, int reportId, CancellationToken ct = default);
    public Task<(Report? report, IReadOnlyList<ReportRun> runs)> GetReportRunsAsync(int workspaceId, int reportId, int take = 100, CancellationToken ct = default);
    public Task<ReportRun?> GetRunAsync(int workspaceId, int runId, CancellationToken ct = default);
}


public class ReportRunService(TickfloDbContext dbContext, IReportingService reportingService, ILogger<ReportRunService> logger) : IReportRunService
{
    private readonly TickfloDbContext dbContext = dbContext;
    private readonly IReportingService reportingService = reportingService;
    private readonly ILogger<ReportRunService> logger = logger;

    public async Task<(Report? report, IReadOnlyList<ReportRun> runs)> GetReportRunsAsync(int workspaceId, int reportId, int take = 100, CancellationToken ct = default)
    {
        var report = await this.dbContext.Reports
            .FirstOrDefaultAsync(r => r.WorkspaceId == workspaceId && r.Id == reportId, ct);

        if (report == null)
        {
            return (null, Array.Empty<ReportRun>());
        }

        var runs = await this.dbContext.ReportRuns
            .Where(rr => rr.WorkspaceId == workspaceId && rr.ReportId == reportId)
            .OrderByDescending(rr => rr.StartedAt)
            .Take(take)
            .ToListAsync(ct);

        return (report, runs);
    }

    public async Task<ReportRun?> GetRunAsync(int workspaceId, int runId, CancellationToken ct = default)
    {
        var run = await this.dbContext.ReportRuns
            .FirstOrDefaultAsync(rr => rr.WorkspaceId == workspaceId && rr.Id == runId, ct);
        return run;
    }

    public async Task<ReportRun?> RunReportAsync(int workspaceId, int reportId, CancellationToken ct = default)
    {
        var rep = await this.dbContext.Reports
            .FirstOrDefaultAsync(r => r.WorkspaceId == workspaceId && r.Id == reportId, ct);

        if (rep == null)
        {
            return null;
        }

        var run = new ReportRun
        {
            WorkspaceId = workspaceId,
            ReportId = rep.Id,
            Status = "Pending",
            StartedAt = DateTime.UtcNow
        };

        this.dbContext.ReportRuns.Add(run);
        await this.dbContext.SaveChangesAsync(ct);

        run.Status = "Running";
        await this.dbContext.SaveChangesAsync(ct);

        try
        {
            var res = await this.reportingService.ExecuteAsync(workspaceId, rep, ct);

            run.Status = "Succeeded";
            run.RowCount = res.RowCount;
            run.FileBytes = res.Bytes;
            run.ContentType = res.ContentType;
            run.FileName = res.FileName;

            rep.LastRun = DateTime.UtcNow;

            await this.dbContext.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Report run {ReportId} failed for workspace {WorkspaceId}", reportId, workspaceId);

            run.Status = "Failed";
            await this.dbContext.SaveChangesAsync(ct);
        }

        return run;
    }
}


