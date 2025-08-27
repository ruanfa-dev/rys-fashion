using Microsoft.AspNetCore.Identity;

using SharedKernel.Domain.Attributes;

namespace Infrastructure.Identity.Models;

public partial class User : IdentityUser<Guid>, IAuditable
{
    #region Properties

    #region Tracking
    public DateTimeOffset? LastSignInAt { get; set; }
    public string? LastSignInIp { get; set; }
    public DateTimeOffset? CurrentSignInAt { get; set; }
    public string? CurrentSignInIp { get; set; }
    public int SignInCount { get; set; } = 0;
    #endregion

    #region Auditable Properties
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public string? UpdatedBy { get; set; }
    #endregion

    #region Relationships
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    #endregion

    #endregion
    #region Constructors
    public User()
    {
        MarkAsCreated();
    }
    #endregion

    #region Bussiness Logic
    #region Auditable Methods
    public void MarkAsCreated(string? userId = null)
    {
        CreatedAt = DateTimeOffset.UtcNow;
        CreatedBy = userId;
    }

    public void MarkAsUpdated(string? userId = null)
    {
        UpdatedAt = DateTimeOffset.UtcNow;
        UpdatedBy = userId;
    }
    #endregion

    #region Tracking
    public void RecordSignIn(string ipAddress)
    {
        LastSignInAt = CurrentSignInAt;
        LastSignInIp = CurrentSignInIp;
        CurrentSignInAt = DateTimeOffset.UtcNow;
        CurrentSignInIp = ipAddress;
        SignInCount++;
        MarkAsUpdated();
    }

    #endregion
    #endregion

}
