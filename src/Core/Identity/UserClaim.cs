using Microsoft.AspNetCore.Identity;

using SharedKernel.Domain.Attributes;

namespace Core.Identity;

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
