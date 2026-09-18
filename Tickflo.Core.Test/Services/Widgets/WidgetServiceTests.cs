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
        var (service, secretProtector, _) = CreateWidgetService(databaseContext);
        secretProtector.Setup(protector => protector.Protect("top-secret")).Returns("enc:top-secret");

        var widget = await service.CreateWidgetAsync(
            workspace.Id,
            new WidgetDraft("Critical alerts", WidgetType.Wazuh, "https://wazuh.example", "top-secret", false, "{}", 60, 0, true),
            1);

        Assert.Equal("enc:top-secret", widget.ApiKeyCiphertext);
        Assert.Equal(WidgetType.Wazuh, widget.Type);
        Assert.Equal(workspace.Id, widget.WorkspaceId);
        Assert.True(widget.IsEnabled);
    }

    [Fact]
    public async Task CreateWidgetAsync_WhenNoSecret_ShouldStoreNullCiphertext()
    {
        await using var databaseContext = CreateDatabaseContext();
        var workspace = await SeedWorkspaceAsync(databaseContext);
        var (service, _, _) = CreateWidgetService(databaseContext);

        var widget = await service.CreateWidgetAsync(
            workspace.Id,
            new WidgetDraft("Uptime", WidgetType.HttpJson, "https://uptime.example", null, false, "{}", 60, 0, true),
            1);

        Assert.Null(widget.ApiKeyCiphertext);
    }

    [Fact]
    public async Task GetWidgetsForWorkspaceAsync_WhenMultipleWorkspaces_ShouldOnlyReturnRequestedWorkspace()
    {
        await using var databaseContext = CreateDatabaseContext();
        var workspaceA = await SeedWorkspaceAsync(databaseContext, "alpha");
        var workspaceB = await SeedWorkspaceAsync(databaseContext, "beta");
        var (service, _, _) = CreateWidgetService(databaseContext);

        await service.CreateWidgetAsync(workspaceA.Id, new WidgetDraft("A1", WidgetType.HttpJson, "https://a.example", null, false, "{}", 60, 0, true), 1);
        await service.CreateWidgetAsync(workspaceB.Id, new WidgetDraft("B1", WidgetType.HttpJson, "https://b.example", null, false, "{}", 60, 0, true), 1);

        var widgets = await service.GetWidgetsForWorkspaceAsync(workspaceA.Id);

        Assert.Single(widgets);
        Assert.Equal("A1", widgets[0].Name);
    }

    [Fact]
    public async Task UpdateWidgetAsync_WhenClearSecret_ShouldSetCiphertextToNull()
    {
        await using var databaseContext = CreateDatabaseContext();
        var workspace = await SeedWorkspaceAsync(databaseContext);
        var (service, secretProtector, _) = CreateWidgetService(databaseContext);
        secretProtector.Setup(protector => protector.Protect("top-secret")).Returns("enc:top-secret");

        var created = await service.CreateWidgetAsync(
            workspace.Id,
            new WidgetDraft("Wazuh", WidgetType.Wazuh, "https://wazuh.example", "top-secret", false, "{}", 60, 0, true),
            1);

        await service.UpdateWidgetAsync(
            workspace.Id,
            created.Id,
            new WidgetDraft("Wazuh", WidgetType.Wazuh, "https://wazuh.example", null, true, "{}", 60, 0, true),
            1);

        var updated = await service.GetWidgetAsync(workspace.Id, created.Id);
        Assert.Null(updated!.ApiKeyCiphertext);
    }

    [Fact]
    public async Task UpdateWidgetAsync_WhenClearSecretWithNewSecretProvided_ShouldPersistNewCiphertext()
    {
        await using var databaseContext = CreateDatabaseContext();
        var workspace = await SeedWorkspaceAsync(databaseContext);
        var (service, secretProtector, _) = CreateWidgetService(databaseContext);
        secretProtector.Setup(protector => protector.Protect("top-secret")).Returns("enc:top-secret");
        secretProtector.Setup(protector => protector.Protect("new-secret")).Returns("enc:new-secret");

        var created = await service.CreateWidgetAsync(
            workspace.Id,
            new WidgetDraft("Wazuh", WidgetType.Wazuh, "https://wazuh.example", "top-secret", false, "{}", 60, 0, true),
            1);

        await service.UpdateWidgetAsync(
            workspace.Id,
            created.Id,
            new WidgetDraft("Wazuh", WidgetType.Wazuh, "https://wazuh.example", "new-secret", true, "{}", 60, 0, true),
            1);

        var updated = await service.GetWidgetAsync(workspace.Id, created.Id);
        Assert.Equal("enc:new-secret", updated!.ApiKeyCiphertext);
    }

    [Fact]
    public async Task UpdateWidgetAsync_WhenSecretEmpty_ShouldKeepExistingCiphertext()
    {
        await using var databaseContext = CreateDatabaseContext();
        var workspace = await SeedWorkspaceAsync(databaseContext);
        var (service, secretProtector, _) = CreateWidgetService(databaseContext);
        secretProtector.Setup(protector => protector.Protect("top-secret")).Returns("enc:top-secret");

        var created = await service.CreateWidgetAsync(
            workspace.Id,
            new WidgetDraft("Wazuh", WidgetType.Wazuh, "https://wazuh.example", "top-secret", false, "{}", 60, 0, true),
            1);

        await service.UpdateWidgetAsync(
            workspace.Id,
            created.Id,
            new WidgetDraft("Wazuh", WidgetType.Wazuh, "https://wazuh.example", null, false, "{}", 60, 0, true),
            1);

        var updated = await service.GetWidgetAsync(workspace.Id, created.Id);
        Assert.Equal("enc:top-secret", updated!.ApiKeyCiphertext);
    }

    [Fact]
    public async Task UpdateWidgetAsync_WhenWidgetDoesNotExist_ShouldThrowNotFoundException()
    {
        await using var databaseContext = CreateDatabaseContext();
        var workspace = await SeedWorkspaceAsync(databaseContext);
        var (service, _, _) = CreateWidgetService(databaseContext);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            service.UpdateWidgetAsync(workspace.Id, 999, new WidgetDraft("Nope", WidgetType.HttpJson, "https://x.example", null, false, "{}", 60, 0, true), 1));
    }

    [Fact]
    public async Task DeleteWidgetAsync_WhenWidgetExists_ShouldRemoveSnapshot()
    {
        await using var databaseContext = CreateDatabaseContext();
        var workspace = await SeedWorkspaceAsync(databaseContext);
        var (service, _, snapshotStore) = CreateWidgetService(databaseContext);

        var created = await service.CreateWidgetAsync(
            workspace.Id,
            new WidgetDraft("A", WidgetType.HttpJson, "https://a.example", null, false, "{}", 60, 0, true),
            1);

        await service.DeleteWidgetAsync(workspace.Id, created.Id);

        snapshotStore.Verify(store => store.RemoveSnapshot(created.Id), Times.Once);
    }

    [Fact]
    public async Task DeleteWidgetAsync_WhenWidgetDoesNotExist_ShouldThrowNotFoundException()
    {
        await using var databaseContext = CreateDatabaseContext();
        var workspace = await SeedWorkspaceAsync(databaseContext);
        var (service, _, _) = CreateWidgetService(databaseContext);

        await Assert.ThrowsAsync<NotFoundException>(() => service.DeleteWidgetAsync(workspace.Id, 999));
    }

    private static (WidgetService Service, Mock<IWidgetSecretProtector> SecretProtector, Mock<IWidgetSnapshotStore> SnapshotStore) CreateWidgetService(TickfloDbContext databaseContext)
    {
        var secretProtector = new Mock<IWidgetSecretProtector>();
        var snapshotStore = new Mock<IWidgetSnapshotStore>();
        return (new WidgetService(databaseContext, secretProtector.Object, snapshotStore.Object), secretProtector, snapshotStore);
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
