namespace Tickflo.CoreTest.Services.Tickets;

using Microsoft.EntityFrameworkCore;
using Moq;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;
using Tickflo.Core.Services.Notifications;
using Tickflo.Core.Services.Tickets;
using Xunit;

public class TicketManagementServiceTests
{
    [Fact]
    public async Task CreateTicketAndNotifyAsyncShouldDispatchTicketCreatedNotification()
    {
        await using var databaseContext = CreateDatabaseContext();
        var workspace = new Workspace { Name = "Operations", Slug = "operations" };
        databaseContext.Workspaces.Add(workspace);
        await databaseContext.SaveChangesAsync();

        var notificationTriggerService = new Mock<INotificationTriggerService>();
        var ticketManagementService = new TicketManagementService(databaseContext, notificationTriggerService.Object);

        var ticket = await ticketManagementService.CreateTicketAndNotifyAsync(new CreateTicketRequest
        {
            WorkspaceId = workspace.Id,
            CreatedByUserId = 17,
            Subject = "Replace thermostat",
            Description = "Controller is offline."
        });

        notificationTriggerService.Verify(service => service.NotifyTicketCreatedAsync(
            workspace.Id,
            It.Is<Ticket>(value => value.Id == ticket.Id),
            17), Times.Once);
    }

    [Fact]
    public async Task UpdateTicketAndNotifyAsyncWhenAssignmentStatusAndDetailsChangeShouldDispatchNotifications()
    {
        await using var databaseContext = CreateDatabaseContext();
        var workspace = new Workspace { Name = "Operations", Slug = "operations" };
        var assignee = new User("Tech", "tech@example.com", "recovery@example.com", "password-hash");
        databaseContext.Workspaces.Add(workspace);
        databaseContext.Users.Add(assignee);
        await databaseContext.SaveChangesAsync();

        var openStatus = new TicketStatus { WorkspaceId = workspace.Id, Name = "Open" };
        var closedStatus = new TicketStatus { WorkspaceId = workspace.Id, Name = "Closed", IsClosedState = true };
        databaseContext.TicketStatuses.AddRange(openStatus, closedStatus);
        databaseContext.UserWorkspaces.Add(new UserWorkspace
        {
            WorkspaceId = workspace.Id,
            UserId = assignee.Id,
            Accepted = true
        });
        await databaseContext.SaveChangesAsync();

        var ticket = new Ticket
        {
            WorkspaceId = workspace.Id,
            Subject = "Inspect rooftop unit",
            Description = "Original details.",
            StatusId = openStatus.Id
        };
        databaseContext.Tickets.Add(ticket);
        await databaseContext.SaveChangesAsync();

        var notificationTriggerService = new Mock<INotificationTriggerService>();
        var ticketManagementService = new TicketManagementService(databaseContext, notificationTriggerService.Object);

        var updatedTicket = await ticketManagementService.UpdateTicketAndNotifyAsync(new UpdateTicketRequest
        {
            TicketId = ticket.Id,
            WorkspaceId = workspace.Id,
            UpdatedByUserId = 29,
            Subject = "Inspect rooftop unit urgently",
            Description = "Updated details.",
            StatusId = closedStatus.Id,
            AssignedUserId = assignee.Id
        });

        notificationTriggerService.Verify(service => service.NotifyTicketAssignmentChangedAsync(
            workspace.Id,
            It.Is<Ticket>(value => value.Id == updatedTicket.Id && value.AssignedUserId == assignee.Id),
            null,
            null,
            29), Times.Once);
        notificationTriggerService.Verify(service => service.NotifyTicketStatusChangedAsync(
            workspace.Id,
            It.Is<Ticket>(value => value.Id == updatedTicket.Id && value.StatusId == closedStatus.Id),
            "Open",
            "Closed",
            29), Times.Once);
        notificationTriggerService.Verify(service => service.NotifyTicketUpdatedAsync(
            workspace.Id,
            It.Is<Ticket>(value => value.Id == updatedTicket.Id),
            29,
            "Assignment changed. Status changed from 'Open' to 'Closed'. Ticket details were updated.",
            It.Is<IReadOnlyCollection<int>>(excludedUserIds => excludedUserIds.Count == 1 && excludedUserIds.Contains(assignee.Id))), Times.Once);
    }

    private static TickfloDbContext CreateDatabaseContext()
    {
        var options = new DbContextOptionsBuilder<TickfloDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new TickfloDbContext(options);
    }
}
