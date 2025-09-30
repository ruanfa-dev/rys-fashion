using Microsoft.AspNetCore.Identity;

using SharedKernel.Domain.Attributes.Assignable;

namespace Core.Identity.Users;

public sealed class UserClaim : IdentityUserClaim<Guid>, IAssignable
{
    public DateTimeOffset? AssignedAt { get; set; }
    public string? AssignedBy { get; set; }

    public User User { get; set; } = default!;

    public void MarkAsAssigned(string? userId = null)
    {
        AssignedBy = userId;
        AssignedAt = DateTimeOffset.UtcNow;
    }
}
