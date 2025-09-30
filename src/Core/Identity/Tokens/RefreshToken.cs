using System.Security.Cryptography;
using System.Text;

using Core.Identity.Users;

namespace Core.Identity.Tokens;

/// <summary>
/// Represents a refresh token for a user.
/// </summary>
public sealed partial class RefreshToken
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public string TokenHash { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public string CreatedByIp { get; set; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; set; }

    public DateTimeOffset? RevokedAt { get; set; }
    public string? RevokedByIp { get; set; }
    public string? RevokedReason { get; set; }

    public bool IsExpired => DateTimeOffset.UtcNow >= ExpiresAt;
    public bool IsRevoked => RevokedAt.HasValue;

    public User User { get; set; } = null!;

    // Private constructor for EF Core
    private RefreshToken() { }

    public static RefreshToken Create(Guid userId, string rawToken, DateTimeOffset expiresAt, string ipAddress)
    {
        return new RefreshToken
        {
            UserId = userId,
            TokenHash = Hash(rawToken),
            ExpiresAt = expiresAt,
            CreatedByIp = ipAddress
        };
    }

    public void Revoke(string ipAddress, string? reason = null)
    {
        if (IsRevoked) return;

        RevokedAt = DateTimeOffset.UtcNow;
        RevokedByIp = ipAddress;
        RevokedReason = reason;
    }

    public static string Hash(string rawToken)
    {
        using var sha = SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(rawToken));
        return Convert.ToBase64String(bytes);
    }
}