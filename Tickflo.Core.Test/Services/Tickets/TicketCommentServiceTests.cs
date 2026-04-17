namespace Tickflo.CoreTest.Services.Tickets;

using Microsoft.EntityFrameworkCore;
using Moq;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;
using Tickflo.Core.Services.Notifications;
using Tickflo.Core.Services.Tickets;
using Xunit;

public class TicketCommentServiceTests
{
    [Fact]
    public async Task AddCommentAndNotifyAsync_WhenCommentIsAdded_ShouldDispatchTicketCommentNotification()
    {
        await using var databaseContext = CreateDatabaseContext();
        var workspace = new Workspace { Name = "Operations", Slug = "operations" };
        var commenter = new User("Coordinator", "coordinator@example.com", "recovery@example.com", "password-hash");
        databaseContext.Workspaces.Add(workspace);
        databaseContext.Users.Add(commenter);
        await databaseContext.SaveChangesAsync();

        var ticket = new Ticket
        {
            WorkspaceId = workspace.Id,
            Subject = "Inspect gate motor"
        };
        databaseContext.Tickets.Add(ticket);
        await databaseContext.SaveChangesAsync();

        var notificationTriggerService = new Mock<INotificationTriggerService>();
        var ticketCommentService = new TicketCommentService(databaseContext, notificationTriggerService.Object);

        var comment = await ticketCommentService.AddCommentAndNotifyAsync(workspace.Id, ticket.Id, commenter.Id, " Added details. ", true);

        Assert.Equal("Added details.", comment.Content);
        notificationTriggerService.Verify(service => service.NotifyTicketCommentAddedAsync(
            workspace.Id,
            It.Is<Ticket>(value => value.Id == ticket.Id),
            commenter.Id,
            true), Times.Once);
    }

    [Fact]
    public async Task AddCommentAndNotifyAsync_WhenTicketDoesNotExist_ShouldNotPersistComment()
    {
        await using var databaseContext = CreateDatabaseContext();
        var workspace = new Workspace { Name = "Operations", Slug = "operations" };
        var commenter = new User("Coordinator", "coordinator@example.com", "recovery@example.com", "password-hash");
        databaseContext.Workspaces.Add(workspace);
        databaseContext.Users.Add(commenter);
        await databaseContext.SaveChangesAsync();

        var notificationTriggerService = new Mock<INotificationTriggerService>();
        var ticketCommentService = new TicketCommentService(databaseContext, notificationTriggerService.Object);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            ticketCommentService.AddCommentAndNotifyAsync(workspace.Id, 999, commenter.Id, "Added details.", true));

        Assert.Empty(databaseContext.TicketComments);
        notificationTriggerService.Verify(
            service => service.NotifyTicketCommentAddedAsync(
                It.IsAny<int>(),
                It.IsAny<Ticket>(),
                It.IsAny<int>(),
                It.IsAny<bool>()),
            Times.Never);
    }

    private static TickfloDbContext CreateDatabaseContext()
    {
        var options = new DbContextOptionsBuilder<TickfloDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new TickfloDbContext(options);
    }
}
