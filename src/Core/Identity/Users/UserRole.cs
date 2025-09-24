using Microsoft.AspNetCore.Identity;

using SharedKernel.Domain.Attributes;

namespace Core.Identity;
public partial class UserRole : IdentityUserRole<Guid>, IAssignable
{
    #region Properties
    #region Auditable Properties
    public DateTimeOffset? AssignedAt { get; set; }
    public string? AssignedBy { get; set; }
    #endregion
    #endregion

    #region Relationships
    public Role Role { get; set; } = default!;
    public User User { get; set; } = default!;
    #endregion

    #region Business Methods
    #region Auditable Methods
    public void MarkAsAssigned(string? userId = null)
    {
        AssignedAt = DateTimeOffset.UtcNow;
        AssignedBy = userId;
    }
    #endregion
    #endregion
}
