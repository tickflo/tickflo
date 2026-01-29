namespace Tickflo.Core.Services.Views;

using Microsoft.EntityFrameworkCore;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;
using Tickflo.Core.Services.Workspace;
using InventoryEntity = Entities.Inventory;

public class WorkspaceInventoryEditViewData
{
    public bool CanViewInventory { get; set; }
    public bool CanEditInventory { get; set; }
    public bool CanCreateInventory { get; set; }
    public InventoryEntity? ExistingItem { get; set; }
    public List<Location> LocationOptions { get; set; } = [];
}

public interface IWorkspaceInventoryEditViewService
{
    public Task<WorkspaceInventoryEditViewData> BuildAsync(int workspaceId, int userId, int inventoryId = 0);
}


public class WorkspaceInventoryEditViewService(
    TickfloDbContext dbContext,
    IWorkspaceAccessService workspaceAccessService) : IWorkspaceInventoryEditViewService
{
    private readonly TickfloDbContext dbContext = dbContext;
    private readonly IWorkspaceAccessService workspaceAccessService = workspaceAccessService;

    public async Task<WorkspaceInventoryEditViewData> BuildAsync(int workspaceId, int userId, int inventoryId = 0)
    {
        var data = new WorkspaceInventoryEditViewData();

        var isAdmin = await this.workspaceAccessService.UserIsWorkspaceAdminAsync(userId, workspaceId);
        var permissions = await this.workspaceAccessService.GetUserPermissionsAsync(workspaceId, userId);

        if (isAdmin)
        {
            data.CanViewInventory = data.CanEditInventory = data.CanCreateInventory = true;
        }
        else if (permissions.TryGetValue("inventory", out var ip))
        {
            data.CanViewInventory = ip.CanView;
            data.CanEditInventory = ip.CanEdit;
            data.CanCreateInventory = ip.CanCreate;
        }

        var locations = await this.dbContext.Locations
            .AsNoTracking()
            .Where(l => l.WorkspaceId == workspaceId)
            .ToListAsync();
        data.LocationOptions = [.. locations];

        if (inventoryId > 0)
        {
            data.ExistingItem = await this.dbContext.Inventory
                .AsNoTracking()
                .FirstOrDefaultAsync(i => i.WorkspaceId == workspaceId && i.Id == inventoryId);
        }
        else
        {
            data.ExistingItem = new InventoryEntity { WorkspaceId = workspaceId, Status = "active" };
        }

        return data;
    }
}



