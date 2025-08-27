using SharedKernel.Domain.Primitives;

namespace Infrastructure.Identity.Models;

/// <summary>
/// Refresh token entity for JWT token management.
/// Supports rotation, revocation, and validation checks.
/// </summary>
public sealed partial class RefreshToken : AuditableEntity
{
    #region Properties
    public Guid UserId { get; set; }
    public string Token { get; set; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; set; }
    public string CreatedByIp { get; set; } = string.Empty;

    // Revocation info
    public bool IsRevoked { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }
    public string? RevokedByIp { get; set; }
    public string? ReplacedByToken { get; set; }
    public string? RevocationReason { get; set; }
    #endregion

    #region Relationships
    public User User { get; set; } = null!;
    #endregion

    #region Computed Properties
    public bool IsExpired => DateTimeOffset.UtcNow >= ExpiresAt;
    public bool IsActive => !IsRevoked && !IsExpired;
    public bool IsReplaced => !string.IsNullOrWhiteSpace(ReplacedByToken);
    #endregion

    #region Factory
    /// <summary>
    /// Create a new refresh token.
    /// </summary>
    public static RefreshToken Create(Guid userId, string token, DateTimeOffset expiresAt, string createdByIp)
    {
        return new RefreshToken
        {
            UserId = userId,
            Token = token,
            ExpiresAt = expiresAt,
            CreatedByIp = createdByIp,
            IsRevoked = false
        };
    }
    #endregion

    #region Behaviors

    /// <summary>
    /// Revoke this token (manually or as part of rotation).
    /// </summary>
    public void Revoke(string revokedByIp, string? replacedByToken = null, string? reason = null)
    {
        if (IsRevoked)
            return; // already revoked

        IsRevoked = true;
        RevokedAt = DateTimeOffset.UtcNow;
        RevokedByIp = revokedByIp;
        ReplacedByToken = replacedByToken;
        RevocationReason = reason;

        MarkAsUpdated();
    }

    /// <summary>
    /// Replace this token with a new one during rotation.
    /// </summary>
    public RefreshToken Replace(string newToken, DateTimeOffset newExpiresAt, string createdByIp)
    {
        // Revoke current token and point it to replacement
        Revoke(createdByIp, newToken, "Rotated");

        // Return new token instance (caller must persist)
        return Create(UserId, newToken, newExpiresAt, createdByIp);
    }

    /// <summary>
    /// Determine if this token can still be used for refreshing.
    /// </summary>
    public bool CanBeRefreshed() => IsActive;

    /// <summary>
    /// Extend expiry (for sliding sessions, e.g. "Remember Me").
    /// </summary>
    public void ExtendExpiry(int days)
    {
        if (IsActive)
        {
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(days);
            MarkAsUpdated();
        }
    }

    #endregion
}
