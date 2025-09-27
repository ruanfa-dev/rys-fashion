using System.Security.Cryptography;
using System.Text;

using Core.Identity.Users;

namespace Core.Identity.Tokens;

/// <summary>
/// Represents a refresh token for a user.
/// </summary>
public sealed partial class RefreshToken
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid UserId { get; private set; }
    public string TokenHash { get; private set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;
    public string CreatedByIp { get; private set; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; private set; }

    public DateTimeOffset? RevokedAt { get; private set; }
    public string? RevokedByIp { get; private set; }
    public string? RevokedReason { get; private set; }

    public bool IsExpired => DateTimeOffset.UtcNow >= ExpiresAt;
    public bool IsRevoked => RevokedAt.HasValue;

    public User User { get; private set; } = null!;

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