namespace Tickflo.Core.Entities;

public class UserEmailChange
{
    public int UserId { get; set; }
    public string OldEmail { get; set; } = string.Empty;
    public string NewEmail { get; set; } = string.Empty;
    public string ConfirmToken { get; set; } = string.Empty;
    public string UndoToken { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public int CreatedBy { get; set; }
    public DateTime? ConfirmedAt { get; set; }
    public int ConfirmMaxAge { get; set; }
    public int UndoMaxAge { get; set; }
    public DateTime? UndoneAt { get; set; }
}
