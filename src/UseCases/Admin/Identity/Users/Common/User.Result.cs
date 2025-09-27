namespace UseCases.Admin.Identity.Users.Common;

public record UserResult 
{
    public record ListItem : UserParam
    {
        public required Guid Id { get; init; }
        public DateTimeOffset CreatedAt { get; set; }
        public string? CreatedBy { get; set; }
        public string[]? Roles { get; set; } = [];
        public DateTimeOffset? LastSignInAt { get; set; }
        public int SignInCount { get; set; }
    }
    public record Detail : ListItem
    {
        #region Audits
        public DateTimeOffset? UpdatedAt { get; set; }
        public string? UpdatedBy { get; set; }
        #endregion

        #region Sign-in Information
        public DateTimeOffset? CurrentSignInAt { get; set; }
        public string? LastSignInIp { get; set; }
        public string? CurrentSignInIp { get; set; }
        #endregion

        #region Access Control
        public string[]? RolePermissions { get; set; } = [];
        public string[]? UserPermissions { get; set; } = [];
        #endregion
    }

}


