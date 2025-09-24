using ErrorOr;

namespace Core.Identity;

public sealed partial class Jwt
{
    public static class Errors
    {
        // General
        public static Error EmptyToken => Error.Validation("JWT.EmptyToken", "Token cannot be empty");
        public static Error InvalidFormat => Error.Validation("JWT.InvalidFormat", "Invalid token format");
        public static Error MissingAlgorithm => Error.Validation("JWT.MissingAlgorithm", "JWT header missing algorithm");
        public static Error EmptyPayload => Error.Validation("JWT.EmptyPayload", "JWT payload is empty");
        public static Error NoExpiration => Error.Validation("JWT.NoExpiration", "Token does not have an expiration claim");

        // Validation
        public static Error InvalidSignature => Error.Validation("JWT.InvalidSignature", "Token signature is invalid");
        public static Error ValidationFailed => Error.Validation("JWT.ValidationFailed", "Token validation failed");

        // Failures
        public static Error ParseFailed => Error.Failure("JWT.ParseFailed", "Failed to parse token");
        public static Error GenerationFailed => Error.Failure("JWT.GenerationFailed", "Failed to generate JWT token");
        public static Error SecurityTokenError => Error.Failure("JWT.SecurityTokenError", "Security token error");

        // Service specific
        public static Error InvalidUser => Error.Validation("JWT.InvalidUser", "Valid user is required");
        public static Error PrincipalExtraction => Error.Failure("JWT.PrincipalExtraction", "Failed to extract principal from token");
        public static Error RemainingTime => Error.Failure("JWT.RemainingTime", "Failed to get remaining time for token");
        public static Error FormatValidation => Error.Failure("JWT.FormatValidation", "Token format validation failed");
        public static Error ClaimsExtraction => Error.Failure("JWT.ClaimsExtraction", "Failed to extract claims from token");
        public static Error ValidationError => Error.Failure("JWT.ValidationError", "Unexpected error during token validation");
    }
}
