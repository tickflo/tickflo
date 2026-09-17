namespace Tickflo.Web.Services;

using Microsoft.Extensions.DependencyInjection;
using Tickflo.Core.Services.Widgets;

/// <summary>
/// Periodically polls every enabled widget, caches the latest snapshot in memory,
/// and never blocks or crashes on a single failing source.
/// </summary>
public class WidgetPollingService(
    IServiceScopeFactory scopeFactory,
    IWidgetSnapshotStore snapshotStore,
    IWidgetSecretProtector secretProtector) : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(30);

    private readonly IServiceScopeFactory scopeFactory = scopeFactory;
    private readonly IWidgetSnapshotStore snapshotStore = snapshotStore;
    private readonly IWidgetSecretProtector secretProtector = secretProtector;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(PollInterval);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await this.PollAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                throw;
            }
            catch
            {
                // A transient failure (e.g. the database being briefly unavailable)
                // must not terminate the poller; it retries on the next tick.
            }
        }
    }

    private async Task PollAsync(CancellationToken cancellationToken)
    {
        using var scope = this.scopeFactory.CreateScope();
        var widgetService = scope.ServiceProvider.GetRequiredService<IWidgetService>();
        var sources = scope.ServiceProvider.GetRequiredService<IEnumerable<IWidgetSource>>();

        var widgets = await widgetService.GetEnabledWidgetsAsync();
        foreach (var widget in widgets)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            var snapshot = this.snapshotStore.GetSnapshot(widget.Id);
            if (snapshot?.FetchedAt is not null
                && DateTime.UtcNow - snapshot.FetchedAt < TimeSpan.FromSeconds(widget.RefreshIntervalSeconds))
            {
                continue;
            }

            var source = sources.FirstOrDefault(candidate => candidate.Type == widget.Type);
            if (source is null)
            {
                this.snapshotStore.SetSnapshot(widget.Id, new WidgetSnapshot(null, WidgetHealth.Error, DateTime.UtcNow, $"No source registered for widget type {widget.Type}."));
                continue;
            }

            var secret = this.DecryptSecret(widget.ApiKeyCiphertext);
            var result = await source.FetchAsync(widget, secret, cancellationToken);
            this.snapshotStore.SetSnapshot(widget.Id, new WidgetSnapshot(result.Value, result.Health, DateTime.UtcNow, result.ErrorMessage));
        }
    }

    private string? DecryptSecret(string? ciphertext)
    {
        if (string.IsNullOrWhiteSpace(ciphertext))
        {
            return null;
        }

        try
        {
            return this.secretProtector.Unprotect(ciphertext);
        }
        catch
        {
            return null;
        }
    }
}
