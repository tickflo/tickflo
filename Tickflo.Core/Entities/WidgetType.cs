namespace Tickflo.Core.Entities;

/// <summary>
/// The kind of external data source a widget reads from.
/// </summary>
public enum WidgetType
{
    HttpJson = 1,
    UptimeKuma = 2,
    Wazuh = 3,
    Action1 = 4,
    Docker = 5,
}
