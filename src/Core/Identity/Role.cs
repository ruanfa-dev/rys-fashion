using Microsoft.AspNetCore.Identity;

using SharedKernel.Domain.Attributes;

namespace Core.Identity;
public partial class Role : IdentityRole<Guid>, IAuditable
{
    #region Properties
    public string? DisplayName { get; set; }
    public string? Description { get; set; }
    public bool IsDefault { get; set; } = false;
    public int Priority { get; set; } = 0;
    public bool IsSystemRole { get; set; } = false;

    #region Auditable Properties
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public string? UpdatedBy { get; set; }
    #endregion
    #endregion

    #region Relationships
    public virtual ICollection<UserRole> UserRoles { get; set; } = default!;
    public virtual ICollection<RoleClaim> RoleClaims { get; set; } = default!;
    #endregion

    #region Contructors
    public static Role Create(
        string name,
        string? description = null,
        int priority = 0,
        bool isSystemRole = false,
        bool isDefault = false)
    {
        var role = new Role
        {
            Name = name,
            NormalizedName = name.ToUpperInvariant(),
            Description = description,
            Priority = priority,
            IsDefault = isDefault,
            IsSystemRole = isSystemRole
        };
        role.MarkAsCreated();
        return role;
    }
    #endregion

    #region Bussiness Methods
    public Role Update(
        string? name,
        string? displayName = null,
        string? description = null,
        int? priority = 0,
        bool? isSystemRole = null)
    {
        if (!string.IsNullOrEmpty(name))
            Name = name;

        if (!string.IsNullOrEmpty(displayName))
            DisplayName = displayName;

        if (!string.IsNullOrEmpty(description))
            Description = description;

        if (priority.HasValue)
            Priority = priority.Value;

        if (isSystemRole.HasValue)
            IsSystemRole = isSystemRole.Value;

        MarkAsUpdated();
        return this;
    }

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
