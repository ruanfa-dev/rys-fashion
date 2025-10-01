using Microsoft.AspNetCore.Identity;

using SharedKernel.Domain.Attributes.Assignable;

namespace Core.Identity.Roles;
public sealed class RoleClaim : IdentityRoleClaim<Guid>, IAssignable
{
    public DateTimeOffset? AssignedAt { get; set; }
    public string? AssignedBy { get; set; }

    // Add navigation property to Role
    public Role Role { get; set; } = null!;

    public void MarkAsAssigned(string? userId = null)
    {
        AssignedBy = userId;
        AssignedAt = DateTimeOffset.UtcNow;
    }
}
