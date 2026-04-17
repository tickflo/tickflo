namespace Tickflo.CoreTest.Services.Tickets;

using Microsoft.EntityFrameworkCore;
using Moq;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;
using Tickflo.Core.Services.Notifications;
using Tickflo.Core.Services.Tickets;
using Xunit;

public class TicketClosingServiceTests
{
    [Fact]
    public async Task CloseTicketAsync_WhenTicketIsClosed_ShouldNotifyTicketUpdateRecipients()
    {
        await using var databaseContext = CreateDatabaseContext();
        var workspace = await SeedWorkspaceWithStatusesAsync(databaseContext);
        var ticket = await SeedTicketAsync(databaseContext, workspace.Id, "Investigate pump alarm", "Open");
        var notificationTriggerService = new Mock<INotificationTriggerService>();
        var ticketClosingService = new TicketClosingService(databaseContext, notificationTriggerService.Object);

        await ticketClosingService.CloseTicketAsync(workspace.Id, ticket.Id, "Resolved on site.", 25);

        notificationTriggerService.Verify(service => service.NotifyTicketUpdatedAsync(
            workspace.Id,
            It.Is<Ticket>(value => value.Id == ticket.Id),
            25,
            "Ticket closed. Resolution: Resolved on site.",
            null), Times.Once);
    }

    [Fact]
    public async Task ReopenTicketAsync_WhenTicketIsReopened_ShouldNotifyTicketUpdateRecipients()
    {
        await using var databaseContext = CreateDatabaseContext();
        var workspace = await SeedWorkspaceWithStatusesAsync(databaseContext);
        var ticket = await SeedTicketAsync(databaseContext, workspace.Id, "Investigate pump alarm", "Closed");
        var notificationTriggerService = new Mock<INotificationTriggerService>();
        var ticketClosingService = new TicketClosingService(databaseContext, notificationTriggerService.Object);

        await ticketClosingService.ReopenTicketAsync(workspace.Id, ticket.Id, "Issue returned.", 26);

        notificationTriggerService.Verify(service => service.NotifyTicketUpdatedAsync(
            workspace.Id,
            It.Is<Ticket>(value => value.Id == ticket.Id),
            26,
            "Ticket reopened. Reason: Issue returned.",
            null), Times.Once);
    }

    [Fact]
    public async Task ResolveTicketAsync_WhenTicketIsResolved_ShouldNotifyTicketUpdateRecipients()
    {
        await using var databaseContext = CreateDatabaseContext();
        var workspace = await SeedWorkspaceWithStatusesAsync(databaseContext);
        var ticket = await SeedTicketAsync(databaseContext, workspace.Id, "Investigate pump alarm", "Open");
        var notificationTriggerService = new Mock<INotificationTriggerService>();
        var ticketClosingService = new TicketClosingService(databaseContext, notificationTriggerService.Object);

        await ticketClosingService.ResolveTicketAsync(workspace.Id, ticket.Id, "Work completed.", 27);

        notificationTriggerService.Verify(service => service.NotifyTicketUpdatedAsync(
            workspace.Id,
            It.Is<Ticket>(value => value.Id == ticket.Id),
            27,
            "Ticket resolved. Work completed.",
            null), Times.Once);
    }

    [Fact]
    public async Task CancelTicketAsync_WhenTicketIsCancelled_ShouldNotifyTicketUpdateRecipients()
    {
        await using var databaseContext = CreateDatabaseContext();
        var workspace = await SeedWorkspaceWithStatusesAsync(databaseContext);
        var ticket = await SeedTicketAsync(databaseContext, workspace.Id, "Investigate pump alarm", "Open");
        var notificationTriggerService = new Mock<INotificationTriggerService>();
        var ticketClosingService = new TicketClosingService(databaseContext, notificationTriggerService.Object);

        await ticketClosingService.CancelTicketAsync(workspace.Id, ticket.Id, "Duplicate request.", 28);

        notificationTriggerService.Verify(service => service.NotifyTicketUpdatedAsync(
            workspace.Id,
            It.Is<Ticket>(value => value.Id == ticket.Id),
            28,
            "Ticket cancelled. Reason: Duplicate request.",
            null), Times.Once);
    }

    private static TickfloDbContext CreateDatabaseContext()
    {
        var options = new DbContextOptionsBuilder<TickfloDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new TickfloDbContext(options);
    }

    private static async Task<Workspace> SeedWorkspaceWithStatusesAsync(TickfloDbContext databaseContext)
    {
        var workspace = new Workspace { Name = "Operations", Slug = "operations" };
        databaseContext.Workspaces.Add(workspace);
        await databaseContext.SaveChangesAsync();

        databaseContext.TicketStatuses.AddRange(
            new TicketStatus { WorkspaceId = workspace.Id, Name = "Open", IsClosedState = false },
            new TicketStatus { WorkspaceId = workspace.Id, Name = "Resolved", IsClosedState = false },
            new TicketStatus { WorkspaceId = workspace.Id, Name = "Cancelled", IsClosedState = false },
            new TicketStatus { WorkspaceId = workspace.Id, Name = "Closed", IsClosedState = true });
        await databaseContext.SaveChangesAsync();

        return workspace;
    }

    private static async Task<Ticket> SeedTicketAsync(
        TickfloDbContext databaseContext,
        int workspaceId,
        string subject,
        string statusName)
    {
        var status = await databaseContext.TicketStatuses
            .FirstAsync(value => value.WorkspaceId == workspaceId && value.Name == statusName);

        var ticket = new Ticket
        {
            WorkspaceId = workspaceId,
            Subject = subject,
            StatusId = status.Id
        };

        databaseContext.Tickets.Add(ticket);
        await databaseContext.SaveChangesAsync();

        return ticket;
    }
}
