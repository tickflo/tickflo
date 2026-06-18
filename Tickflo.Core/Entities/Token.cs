namespace Tickflo.Core.Entities;

using System.ComponentModel.DataAnnotations.Schema;
using Tickflo.Core.Utils;

public class Token
{
    public int UserId { get; set; }

    [Column("token")]
    public string Value { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public int MaxAge { get; set; }

    public int TypeId { get; set; } = (int)TokenType.Session;

    [NotMapped]
    public TokenType Type
    {
        get => (TokenType)this.TypeId;
        set => this.TypeId = (int)value;
    }

    private Token()
    {
    }

    public Token(int userId, int maxAgeInSeconds)
    {
        this.UserId = userId;
        this.Value = SecureTokenGenerator.GenerateToken(16);
        this.MaxAge = maxAgeInSeconds;
    }

    public Token(int userId, int maxAgeInSeconds, TokenType type)
    {
        this.UserId = userId;
        this.Value = SecureTokenGenerator.GenerateToken(16);
        this.MaxAge = maxAgeInSeconds;
        this.TypeId = (int)type;
    }

    public Token(int userId, int maxAgeInSeconds, TokenType type, int tokenByteLength)
    {
        this.UserId = userId;
        this.Value = SecureTokenGenerator.GenerateToken(tokenByteLength);
        this.MaxAge = maxAgeInSeconds;
        this.TypeId = (int)type;
    }
}
