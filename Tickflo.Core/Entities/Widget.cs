namespace Tickflo.Core.Entities;

using System.ComponentModel.DataAnnotations.Schema;

/// <summary>
/// A status widget shown on a workspace's ops dashboard. Reads a value from an
/// external system (HTTP JSON endpoint, Wazuh, Uptime Kuma, etc.) and surfaces
/// its latest health snapshot.
/// </summary>
public class Widget : IWorkspaceEntity
{
    public int Id { get; set; }
    public int WorkspaceId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int TypeId { get; set; } = (int)WidgetType.HttpJson;

    /// <summary>The base URL or endpoint the widget reads from.</summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>Encrypted secret (API key, token, or password). Null when the source needs no auth.</summary>
    public string? ApiKeyCiphertext { get; set; }

    /// <summary>Adapter-specific settings serialized as JSON (json path, metric, thresholds, etc.).</summary>
    public string ConfigJson { get; set; } = "{}";

    public bool IsEnabled { get; set; } = true;
    public int RefreshIntervalSeconds { get; set; } = 60;
    public int SortOrder { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public int? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int? UpdatedBy { get; set; }

    [NotMapped]
    public WidgetType Type
    {
        get => (WidgetType)this.TypeId;
        set => this.TypeId = (int)value;
    }
}
