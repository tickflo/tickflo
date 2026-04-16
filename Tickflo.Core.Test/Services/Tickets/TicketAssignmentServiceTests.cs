namespace Tickflo.CoreTest.Services.Tickets;

using Microsoft.EntityFrameworkCore;
using Moq;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;
using Tickflo.Core.Services.Notifications;
using Tickflo.Core.Services.Tickets;
using Xunit;

public class TicketAssignmentServiceTests
{
    [Fact]
    public async Task UpdateAssignmentAsyncWhenAssignmentChangesShouldDispatchNotifications()
    {
        await using var databaseContext = CreateDatabaseContext();
        var workspace = new Workspace { Name = "Operations", Slug = "operations" };
        var assignee = new User("Tech", "tech@example.com", "tech-recovery@example.com", "password-hash");
        databaseContext.Workspaces.Add(workspace);
        databaseContext.Users.Add(assignee);
        await databaseContext.SaveChangesAsync();

        var ticket = new Ticket
        {
            WorkspaceId = workspace.Id,
            Subject = "Replace ballast"
        };
        databaseContext.Tickets.Add(ticket);
        await databaseContext.SaveChangesAsync();

        var notificationTriggerService = new Mock<INotificationTriggerService>();
        var ticketAssignmentService = new TicketAssignmentService(databaseContext, notificationTriggerService.Object);

        var changed = await ticketAssignmentService.UpdateAssignmentAsync(ticket, assignee.Id, 42);

        Assert.True(changed);
        notificationTriggerService.Verify(service => service.NotifyTicketAssignmentChangedAsync(
            workspace.Id,
            It.Is<Ticket>(value => value.Id == ticket.Id && value.AssignedUserId == assignee.Id),
            null,
            null,
            42), Times.Once);
        notificationTriggerService.Verify(service => service.NotifyTicketUpdatedAsync(
            workspace.Id,
            It.Is<Ticket>(value => value.Id == ticket.Id && value.AssignedUserId == assignee.Id),
            42,
            "Assignment changed.",
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
