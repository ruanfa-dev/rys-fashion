using ErrorOr;

namespace Core.Identity;
public static class Authentication
{
    public static class Errors
    {
        public static Error ConfigurationError => Error.Failure("Authentication.ConfigurationError", "Authentication configuration error");
        public static Error DatabaseError => Error.Failure("Authentication.DatabaseError", "Database error occurred during authentication");
    }
}
