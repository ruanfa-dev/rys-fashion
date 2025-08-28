using Microsoft.AspNetCore.Identity;

using SharedKernel.Domain.Attributes;

namespace Core.Identity;
public partial class Role : IdentityRole<Guid>, IAuditable
{
    #region Properties
    public string? Description { get; set; }

    #region Auditable Properties
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public string? UpdatedBy { get; set; }
    #endregion
    #endregion

    #region Bussiness Methods
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
    #endregion
}
