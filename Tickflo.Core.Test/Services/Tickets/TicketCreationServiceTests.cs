namespace Tickflo.CoreTest.Services.Tickets;

using Microsoft.EntityFrameworkCore;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;
using Tickflo.Core.Services.Tickets;
using Xunit;

public class TicketCreationServiceTests
{
    [Fact]
    public async Task CreateTicketAsyncWhenNamesAreOmittedShouldUseWorkspaceReferenceDefaults()
    {
        await using var databaseContext = CreateDatabaseContext();
        var workspace = new Workspace { Name = "Operations", Slug = "operations" };
        databaseContext.Workspaces.Add(workspace);
        await databaseContext.SaveChangesAsync();

        var defaultType = new TicketType { WorkspaceId = workspace.Id, Name = "Inspection", SortOrder = 1 };
        var laterType = new TicketType { WorkspaceId = workspace.Id, Name = "Repair", SortOrder = 2 };
        var defaultPriority = new TicketPriority { WorkspaceId = workspace.Id, Name = "Normal", SortOrder = 1 };
        var laterPriority = new TicketPriority { WorkspaceId = workspace.Id, Name = "Urgent", SortOrder = 2 };
        var defaultStatus = new TicketStatus { WorkspaceId = workspace.Id, Name = "Queued", SortOrder = 1, IsClosedState = false };
        var closedStatus = new TicketStatus { WorkspaceId = workspace.Id, Name = "Closed", SortOrder = 2, IsClosedState = true };
        databaseContext.TicketTypes.AddRange(defaultType, laterType);
        databaseContext.TicketPriorities.AddRange(defaultPriority, laterPriority);
        databaseContext.TicketStatuses.AddRange(defaultStatus, closedStatus);
        await databaseContext.SaveChangesAsync();

        var ticketCreationService = new TicketCreationService(databaseContext);

        var createdTicket = await ticketCreationService.CreateTicketAsync(
            workspace.Id,
            new TicketCreationRequest
            {
                Subject = "Inspect rooftop unit",
                Description = "Original details."
            },
            17);

        Assert.Equal(defaultType.Id, createdTicket.TicketTypeId);
        Assert.Equal(defaultPriority.Id, createdTicket.PriorityId);
        Assert.Equal(defaultStatus.Id, createdTicket.StatusId);
    }

    private static TickfloDbContext CreateDatabaseContext()
    {
        var options = new DbContextOptionsBuilder<TickfloDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new TickfloDbContext(options);
    }
}
