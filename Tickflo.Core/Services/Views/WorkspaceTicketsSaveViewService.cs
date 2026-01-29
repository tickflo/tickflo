namespace Tickflo.Core.Services.Views;

using Tickflo.Core.Entities;
using Tickflo.Core.Services.Tickets;
using Tickflo.Core.Services.Workspace;

public class WorkspaceTicketsSaveViewData
{
    public bool CanCreateTickets { get; set; }
    public bool CanEditTickets { get; set; }
    public bool CanAccessTicket { get; set; }
}

public interface IWorkspaceTicketsSaveViewService
{
    public Task<WorkspaceTicketsSaveViewData> BuildAsync(int workspaceId, int userId, bool isNew, Ticket? existing = null);
}


public class WorkspaceTicketsSaveViewService(
    IWorkspaceAccessService workspaceAccessService,
    ITicketManagementService ticketManagementService) : IWorkspaceTicketsSaveViewService
{
    private readonly IWorkspaceAccessService workspaceAccessService = workspaceAccessService;
    private readonly ITicketManagementService ticketManagementService = ticketManagementService;

    public async Task<WorkspaceTicketsSaveViewData> BuildAsync(int workspaceId, int userId, bool isNew, Ticket? existing = null)
    {
        var data = new WorkspaceTicketsSaveViewData();

        var isAdmin = await this.workspaceAccessService.UserIsWorkspaceAdminAsync(userId, workspaceId);
        var permissions = await this.workspaceAccessService.GetUserPermissionsAsync(workspaceId, userId);

        if (isAdmin)
        {
            data.CanCreateTickets = true;
            data.CanEditTickets = true;
            data.CanAccessTicket = true;
        }
        else
        {
            if (permissions.TryGetValue("tickets", out var tp))
            {
                data.CanCreateTickets = tp.CanCreate;
                data.CanEditTickets = tp.CanEdit;
            }

            // For existing tickets, also check scope access
            if (!isNew && existing != null)
            {
                data.CanAccessTicket = await this.ticketManagementService.CanUserAccessTicketAsync(existing, userId, workspaceId, isAdmin);
            }
            else
            {
                data.CanAccessTicket = true;
            }
        }

        return data;
    }
}



