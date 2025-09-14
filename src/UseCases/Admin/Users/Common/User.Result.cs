namespace UseCases.Admin.Users.Common;

public record UserResult : UserParam
{
    public required Guid Id { get; init; }
    public DateTimeOffset CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
}

public record UserDetailedResult : UserResult
{
    #region Audits
    public DateTimeOffset? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
    #endregion

    #region Sign-in Information
    public DateTimeOffset? LastSignInAt { get; set; }
    public DateTimeOffset? CurrentSignInAt { get; set; }
    public string? LastSignInIp { get; set; }
    public string? CurrentSignInIp { get; set; }
    public int SignInCount { get; set; }
    #endregion

    #region Access Control
    public string[]? Roles { get; set; } = [];
    public string[]? RolePermissions { get; set; } = [];
    public string[]? UserPermissions { get; set; } = [];
    #endregion
}

public record UserListResult : UserResult
{
    public string[] Roles { get; set; } = [];
    public DateTimeOffset? LastSignInAt { get; set; }
    public int SignInCount { get; set; }
}

public record UserSelectItemResult
{
    public required Guid Id { get; init; }
    public required string Email { get; init; }
    public string? FullName => !string.IsNullOrEmpty(FirstName) && !string.IsNullOrEmpty(LastName) 
        ? $"{FirstName} {LastName}" 
        : Email;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
}