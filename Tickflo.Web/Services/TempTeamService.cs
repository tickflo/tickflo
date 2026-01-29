namespace Tickflo.Web.Services;

using Microsoft.EntityFrameworkCore;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;

// TODO: This is a TEMPORARY service that should NOT exist. This logic belongs in Tickflo.Core
// This is only here as a stopgap while migrating away from repositories

public interface ITempTeamService
{
    public Task<Team?> FindByIdAsync(int teamId);
    public Task<List<User>> ListMembersAsync(int teamId);
}

public class TempTeamService(TickfloDbContext dbContext) : ITempTeamService
{
    private readonly TickfloDbContext dbContext = dbContext;

    public async Task<Team?> FindByIdAsync(int teamId) => await this.dbContext.Teams.FindAsync(teamId);

    public async Task<List<User>> ListMembersAsync(int teamId)
    {
        var userIds = await this.dbContext.TeamMembers
            .Where(tm => tm.TeamId == teamId)
            .Select(tm => tm.UserId)
            .ToListAsync();

        return await this.dbContext.Users
            .Where(u => userIds.Contains(u.Id))
            .ToListAsync();
    }
}
