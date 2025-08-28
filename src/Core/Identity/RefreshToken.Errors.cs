using ErrorOr;

namespace Core.Identity;
public sealed partial class RefreshToken
{
    public static class Errors
    {
        public static Error NotFound => Error.NotFound("RefreshToken.NotFound", "Refresh token not found");
        public static Error Expired => Error.Validation("RefreshToken.Expired", "Refresh token has expired");
        public static Error Revoked => Error.Validation("RefreshToken.Revoked", "Refresh token has been revoked");
        public static Error Invalid => Error.Validation("RefreshToken.Invalid", "Refresh token is invalid");
        public static Error AlreadyUsed => Error.Validation("RefreshToken.AlreadyUsed", "Refresh token has already been used");
        public static Error GenerationFailed => Error.Failure("RefreshToken.GenerationFailed", "Failed to generate refresh token");
        public static Error RotationFailed => Error.Failure("RefreshToken.RotationFailed", "Failed to rotate refresh token");
        public static Error RevocationFailed => Error.Failure("RefreshToken.RevocationFailed", "Failed to revoke refresh token");
        public static Error TooManyActiveTokens => Error.Validation("RefreshToken.TooManyActiveTokens", "User has too many active refresh tokens");
        public static Error SecurityViolation => Error.Validation("RefreshToken.SecurityViolation", "Security violation detected - possible token theft");
        public static Error RateLimitExceeded => Error.Validation("RefreshToken.RateLimitExceeded", "Rate limit exceeded for refresh token operations");
        public static Error InvalidFormat => Error.Validation("RefreshToken.InvalidFormat", "Refresh token format is invalid");
        public static Error InvalidIpAddress => Error.Validation("RefreshToken.InvalidIpAddress", "Invalid IP address format");
    }
}
