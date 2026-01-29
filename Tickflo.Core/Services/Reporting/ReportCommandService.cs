namespace Tickflo.Core.Services.Reporting;

using Microsoft.EntityFrameworkCore;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;

public interface IReportCommandService
{
    public Task<Report?> FindAsync(int workspaceId, int reportId, CancellationToken ct = default);
    public Task<Report> CreateAsync(Report report, CancellationToken ct = default);
    public Task<Report?> UpdateAsync(Report report, CancellationToken ct = default);
}


public class ReportCommandService(TickfloDbContext dbContext) : IReportCommandService
{
    private readonly TickfloDbContext dbContext = dbContext;

    public Task<Report?> FindAsync(int workspaceId, int reportId, CancellationToken ct = default) =>
        this.dbContext.Reports.FirstOrDefaultAsync(r => r.WorkspaceId == workspaceId && r.Id == reportId, ct);

    public async Task<Report> CreateAsync(Report report, CancellationToken ct = default)
    {
        this.dbContext.Reports.Add(report);
        await this.dbContext.SaveChangesAsync(ct);
        return report;
    }

    public async Task<Report?> UpdateAsync(Report report, CancellationToken ct = default)
    {
        this.dbContext.Reports.Update(report);
        await this.dbContext.SaveChangesAsync(ct);
        return report;
    }
}


