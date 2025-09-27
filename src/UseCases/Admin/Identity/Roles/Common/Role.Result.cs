using SharedKernel.Models.PagedLists;

namespace UseCases.Admin.Identity.Roles.Common;


public class RoleResult
{
    /// <summary>
    /// Base result for role operations
    /// </summary>
    public record ListItem : RoleParam
    {
        public required Guid Id { get; init; }
        public bool IsDefault { get; init; } = false;
        public DateTimeOffset CreatedAt { get; init; }
        public string? CreatedBy { get; init; }
        public int UserCount { get; init; } = 0;
        public int PermissionCount { get; init; } = 0;
    }

    /// <summary>
    /// Detailed result for role operations with comprehensive information
    /// </summary>
    public record Detail : ListItem
    {
        #region Audit Information
        public DateTimeOffset? UpdatedAt { get; init; }
        public string? UpdatedBy { get; init; }
        #endregion

        #region Permissions and Users
        public string[] Permissions { get; init; } = [];
        public PagedList<UserInRoleListItemResult> Users { get; init; } = null!;
        #endregion
    }

    /// <summary>
    /// Select item result for dropdowns and selections
    /// </summary>
    public record OptionItem
    {
        public required Guid Id { get; init; }
        public required string Name { get; init; }
        public string? Description { get; init; }
        public bool IsDefault { get; init; } = true;
        public bool IsSystemRole { get; init; } = false;
        public int UserCount { get; init; } = 0;
    }

}




/// <summary>
/// Supporting classes for detailed results
/// </summary>
public record UserInRoleListItemResult
{
    public required Guid UserId { get; init; }
    public required string Email { get; init; }
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public DateTimeOffset AssignedAt { get; init; }
    public string? AssignedBy { get; init; }
}

