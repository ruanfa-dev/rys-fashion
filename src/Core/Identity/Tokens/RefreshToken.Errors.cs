using ErrorOr;

namespace Core.Identity.Tokens;
public sealed partial class RefreshToken
{
    public static class Errors
    {
        #region NotFound
        public static Error RefreshTokenNotFound => Error.NotFound("RefreshToken.NotFound", "Refresh token not found");
        #endregion

        #region Validation
        public static Error RefreshTokenRequired => Error.Validation("RefreshToken.Required", "Refresh token is required");
        public static Error Expired => Error.Validation("RefreshToken.Expired", "Refresh token has expired");
        public static Error Revoked => Error.Validation("RefreshToken.Revoked", "Refresh token has been revoked");
        public static Error InvalidIpAddress => Error.Validation("RefreshToken.InvalidIpAddress", "Invalid IP address format");
        #endregion

        #region Failures
        public static Error GenerationFailed => Error.Failure("RefreshToken.GenerationFailed", "Failed to generate refresh token");
        public static Error RotationFailed => Error.Failure("RefreshToken.RotationFailed", "Failed to rotate refresh token");
        public static Error RevocationFailed => Error.Failure("RefreshToken.RevocationFailed", "Failed to revoke refresh token");
        public static Error ValidationFailed => Error.Failure("RefreshToken.ValidationFailed", "Failed to validate refresh token");
        public static Error CleanupFailed => Error.Failure("RefreshToken.CleanupFailed", "Token cleanup failed");
        public static Error RevokeAllFailed => Error.Failure("RefreshToken.RevokeAllFailed", "Failed to revoke all user tokens");
        #endregion
    }
}