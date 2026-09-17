namespace Tickflo.CoreTest.Services.Widgets;

using Microsoft.EntityFrameworkCore;
using Moq;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;
using Tickflo.Core.Exceptions;
using Tickflo.Core.Services.Widgets;
using Xunit;

public class WidgetServiceTests
{
    [Fact]
    public async Task CreateWidgetAsync_WhenSecretProvided_ShouldStoreEncryptedCiphertext()
    {
        await using var databaseContext = CreateDatabaseContext();
        var workspace = await SeedWorkspaceAsync(databaseContext);
        var secretProtector = new Mock<IWidgetSecretProtector>();
        secretProtector.Setup(protector => protector.Protect("top-secret")).Returns("enc:top-secret");
        var widgetService = new WidgetService(databaseContext, secretProtector.Object);

        var widget = await widgetService.CreateWidgetAsync(
            workspace.Id,
            "Critical alerts",
            WidgetType.Wazuh,
            "https://wazuh.example",
            "top-secret",
            "{}",
            60,
            0,
            1);

        Assert.Equal("enc:top-secret", widget.ApiKeyCiphertext);
        Assert.Equal(WidgetType.Wazuh, widget.Type);
        Assert.Equal(workspace.Id, widget.WorkspaceId);
    }

    [Fact]
    public async Task CreateWidgetAsync_WhenNoSecret_ShouldStoreNullCiphertext()
    {
        await using var databaseContext = CreateDatabaseContext();
        var workspace = await SeedWorkspaceAsync(databaseContext);
        var secretProtector = new Mock<IWidgetSecretProtector>();
        var widgetService = new WidgetService(databaseContext, secretProtector.Object);

        var widget = await widgetService.CreateWidgetAsync(
            workspace.Id,
            "Uptime",
            WidgetType.HttpJson,
            "https://uptime.example",
            null,
            "{}",
            60,
            0,
            1);

        Assert.Null(widget.ApiKeyCiphertext);
    }

    [Fact]
    public async Task GetWidgetsForWorkspaceAsync_WhenMultipleWorkspaces_ShouldOnlyReturnRequestedWorkspace()
    {
        await using var databaseContext = CreateDatabaseContext();
        var workspaceA = await SeedWorkspaceAsync(databaseContext, "alpha");
        var workspaceB = await SeedWorkspaceAsync(databaseContext, "beta");
        var secretProtector = new Mock<IWidgetSecretProtector>();
        var widgetService = new WidgetService(databaseContext, secretProtector.Object);

        await widgetService.CreateWidgetAsync(workspaceA.Id, "A1", WidgetType.HttpJson, "https://a.example", null, "{}", 60, 0, 1);
        await widgetService.CreateWidgetAsync(workspaceB.Id, "B1", WidgetType.HttpJson, "https://b.example", null, "{}", 60, 0, 1);

        var widgets = await widgetService.GetWidgetsForWorkspaceAsync(workspaceA.Id);

        Assert.Single(widgets);
        Assert.Equal("A1", widgets[0].Name);
    }

    [Fact]
    public async Task UpdateWidgetAsync_WhenWidgetDoesNotExist_ShouldThrowNotFoundException()
    {
        await using var databaseContext = CreateDatabaseContext();
        var workspace = await SeedWorkspaceAsync(databaseContext);
        var secretProtector = new Mock<IWidgetSecretProtector>();
        var widgetService = new WidgetService(databaseContext, secretProtector.Object);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            widgetService.UpdateWidgetAsync(workspace.Id, 999, "Nope", WidgetType.HttpJson, "https://x.example", null, "{}", 60, 0, 1));
    }

    [Fact]
    public async Task DeleteWidgetAsync_WhenWidgetDoesNotExist_ShouldThrowNotFoundException()
    {
        await using var databaseContext = CreateDatabaseContext();
        var workspace = await SeedWorkspaceAsync(databaseContext);
        var secretProtector = new Mock<IWidgetSecretProtector>();
        var widgetService = new WidgetService(databaseContext, secretProtector.Object);

        await Assert.ThrowsAsync<NotFoundException>(() => widgetService.DeleteWidgetAsync(workspace.Id, 999));
    }

    private static TickfloDbContext CreateDatabaseContext()
    {
        var options = new DbContextOptionsBuilder<TickfloDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new TickfloDbContext(options);
    }

    private static async Task<Workspace> SeedWorkspaceAsync(TickfloDbContext databaseContext, string slug = "operations")
    {
        var workspace = new Workspace { Name = "Operations", Slug = slug };
        databaseContext.Workspaces.Add(workspace);
        await databaseContext.SaveChangesAsync();
        return workspace;
    }
}
