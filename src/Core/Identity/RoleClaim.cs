using Microsoft.AspNetCore.Identity;

using SharedKernel.Domain.Attributes;

namespace Core.Identity;
public sealed class RoleClaim : IdentityRoleClaim<Guid>, IAssignable
{
    public DateTimeOffset? AssignedAt { get; set; }
    public string? AssignedBy { get; set; }

    // Add navigation property to Role
    public Role Role { get; set; } = default!;

    public void MarkAsAssigned(string? userId = null)
    {
        AssignedBy = userId;
        AssignedAt = DateTimeOffset.UtcNow;
    }
}
