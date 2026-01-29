namespace Tickflo.Core.Services.Views;

using Microsoft.EntityFrameworkCore;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;
using Tickflo.Core.Services.Workspace;

public class WorkspaceContactsEditViewData
{
    public bool CanViewContacts { get; set; }
    public bool CanEditContacts { get; set; }
    public bool CanCreateContacts { get; set; }
    public Contact? ExistingContact { get; set; }
    public List<TicketPriority> Priorities { get; set; } = [];
}

public interface IWorkspaceContactsEditViewService
{
    public Task<WorkspaceContactsEditViewData> BuildAsync(int workspaceId, int userId, int contactId = 0);
}


public class WorkspaceContactsEditViewService(
    TickfloDbContext dbContext,
    IWorkspaceAccessService workspaceAccessService) : IWorkspaceContactsEditViewService
{
    private readonly TickfloDbContext dbContext = dbContext;
    private readonly IWorkspaceAccessService workspaceAccessService = workspaceAccessService;

    public async Task<WorkspaceContactsEditViewData> BuildAsync(int workspaceId, int userId, int contactId = 0)
    {
        var data = new WorkspaceContactsEditViewData();

        var isAdmin = await this.workspaceAccessService.UserIsWorkspaceAdminAsync(userId, workspaceId);
        var permissions = await this.workspaceAccessService.GetUserPermissionsAsync(workspaceId, userId);

        if (isAdmin)
        {
            data.CanViewContacts = data.CanEditContacts = data.CanCreateContacts = true;
        }
        else if (permissions.TryGetValue("contacts", out var cp))
        {
            data.CanViewContacts = cp.CanView;
            data.CanEditContacts = cp.CanEdit;
            data.CanCreateContacts = cp.CanCreate;
        }

        var priorities = await this.dbContext.TicketPriorities
            .AsNoTracking()
            .Where(p => p.WorkspaceId == workspaceId)
            .ToListAsync();
        data.Priorities = [.. priorities];

        if (contactId > 0)
        {
            data.ExistingContact = await this.dbContext.Contacts
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.WorkspaceId == workspaceId && c.Id == contactId);
        }

        return data;
    }
}


