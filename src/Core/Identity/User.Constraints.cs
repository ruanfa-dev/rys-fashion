namespace Core.Identity;

public partial class User
{
    public static class Constraints
    {
        // Username: Prevent injection, enforce safe length, disallow dangerous characters
        public const int UserNameMaxLength = 256;
        public const int UserNameMinLength = 3;

        public const string
            UserNameAllowedPattern =
                @"^[a-zA-Z0-9._-]{3,256}$"; // Alphanumeric + safe symbols

        // First/Last Name: Allow international names but restrict length and disallow control chars
        public const int NameMaxLength = 50;

        public const int
            NameMinLength = 1; // Names can be single char (e.g., "Li", "Xu")

        public const string
            NameAllowedPattern =
                @"^[\p{L} \.'-]{1,50}$"; // Unicode letters, space, hyphen, apostrophe

        // Phone Number: Enforce E.164 format globally
        public const int PhoneNumberMaxLength = 15;
        public const int PhoneNumberMinLength = 7;

        public const string
            PhoneNumberFormat =
                @"^\+[1-9]\d{1,14}$"; // E.164: + followed by 1–15 digits, starting with 1–9

        // Email: Enforce standard email format, allow international characters, and restrict length
        public const int EmailMaxLength = 256;

        // Password: Enforce strong password policy (OWASP/ASP.NET Core Identity defaults)
        public const int PasswordMinLength = 8;

        public const int PasswordMaxLength = 32;

        // At least one uppercase, one lowercase, one digit, one special char, min 8 chars, at least 6 unique chars
        public const string PasswordAllowedPattern =
            @"^(?=(?:.*[A-Z]))(?=(?:.*[a-z]))(?=(?:.*\d))(?=(?:.*[^\da-zA-Z]))(?=(?:.*(.)(?!.*\1)){6,}).{8,32}$";

        // 2FA Code: 6 digits, numeric only
        public const int TwoFactorCodeLength = 6;
        public const string TwoFactorCodePattern = @"^\d{6}$";

        // 2FA Recovery Code: 8 alphanumeric characters, uppercase only
        public const int TwoFactorRecoveryCodeLength = 8;
        public const string TwoFactorRecoveryCodePattern = @"^[A-Z0-9]{8}$";

        // Confirm Email: Base64 encoded string, similar to reset password tokens
        public const int ConfirmationCodeMinLength = 80;

        public const int ConfirmationCodeMaxLength = 256;

        // Pattern: URL-safe Base64 (alphanumeric, '-', '_', '=', '.')
        public const string ConfirmationCodePattern =
            @"^[A-Za-z0-9\-_=.]{80,256}$"; // matches Identity tokens

        // Reset Password Token: Identity tokens are long, URL-safe Base64 strings
        public const int
            ResetPasswordCodeMinLength = 80; // typical minimum length

        public const int ResetPasswordCodeMaxLength = 256; // safe upper bound

        // Pattern: URL-safe Base64 (alphanumeric, '-', '_', '=', '.')
        public const string ResetPasswordCodePattern =
            @"^[A-Za-z0-9\-_=.]{80,256}$"; // matches Identity tokens

        // External Authentication: Provider names and constraints
        public const string GoogleProvider = "Google";
        public const string MicrosoftProvider = "Microsoft";
        public const string FacebookProvider = "Facebook";
        public const string GitHubProvider = "Github";

        public const int ExternalProviderNameMaxLength = 50;
        public const int ExternalProviderIdMaxLength = 256;
        public const int CallbackUrlMaxLength = 2048;
        public const int StateParameterMaxLength = 256;

        // External login URL validation pattern
        public const string CallbackUrlPattern = @"^https?:\/\/[^\s/$.?#].[^\s]*$";

        // Tracking: User-Agent and IP Address
        public const int IpAddressMaxLength = 45; // IPv6 max length (IPv4 fits in this too)
        public const string IpAddressPattern =
            @"^((25[0-5]|(2[0-4]\d|1\d{2}|[1-9]?\d))\.){3}(25[0-5]|(2[0-4]\d|1\d{2}|[1-9]?\d))$|^([a-fA-F0-9]{1,4}:){7}[a-fA-F0-9]{1,4}$";
    }
}