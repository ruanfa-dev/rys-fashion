using ErrorOr;

namespace Core.Identity;

public partial class Permission
{
    #region Constants
    public static class Errors
    {
        #region Basic Permission Errors
        public static Error PermissionRequired => Error.Validation(
            code: "Permission.PermissionRequired",
            description: "Permission is required");

        public static Error NotFound(string permission = "") => Error.NotFound(
            code: "Permission.NotFound",
            description: $"Permission '{permission}' was not found");
        #endregion

        #region Format and Structure Errors
        public static Error InvalidFormat => Error.Validation(
            code: "Permission.InvalidFormat",
            description: $"Permission does not follow the required format: area.resource.action (exactly {Constraints.Segments} segments). " +
                         $"Each segment must be {Constraints.MinSegmentLength}-{Constraints.MaxSegmentLength} characters and match the pattern '{Constraints.SegmentAllowedPattern}'.");
        #endregion
    }
    #endregion
}
