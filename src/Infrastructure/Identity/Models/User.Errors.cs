using ErrorOr;

namespace Infrastructure.Identity.Models;

public partial class User
{
    public static class Errors
    {
        #region Identification errors
        public static Error UserIdRequired =>
            Error.Validation(
                code: $"User.UserIdRequired",
                description: "User ID is required.");
        public static Error UserNotFound =>
            Error.NotFound(
                code: $"User.UserNotFound",
                description: "User not found.");
        #endregion

        #region Username errors
        public static Error UserNameRequired =>
            Error.Validation(
                code: $"User.UserNameRequired",
                description: "Username is required.");

        public static Error UserNameTooShort =>
            Error.Validation(
                code: $"User.UserNameTooShort",
                description: $"Username must be at least {Constraints.UserNameMinLength} characters long.");
        public static Error UserNameTooLong =>
            Error.Validation(
                code: $"User.UserNameTooLong",
                description: $"Username must be at most {Constraints.UserNameMaxLength} characters long.");

        public static Error UserNameInvalidFormat => Error.Validation(
            code: $"User.UserNameInvalidFormat",
            description: "Username contains invalid characters. Only alphanumeric characters, dots, underscores, and hyphens are allowed.");

        public static Error UserNameAlreadyExists(string userName) => Error.Conflict(
            code: $"User.UserNameAlreadyExists",
            description: $"A user with the username '{userName}' already exists.");
        #endregion

        #region First Name errors
        public static Error FirstNameRequired => Error.Validation(
            code: $"User.FirstNameRequired",
            description: "First name is required.");

        public static Error FirstNameTooShort => Error.Validation(
            code: $"User.FirstNameTooShort",
            description: $"First name must be at least {Constraints.NameMinLength} characters long.");

        public static Error FirstNameTooLong => Error.Validation(
            code: $"User.FirstNameTooLong",
            description: $"First name must be at most {Constraints.NameMaxLength} characters long.");

        public static Error FirstNameInvalidFormat => Error.Validation(
            code: $"User.FirstNameInvalidFormat",
            description: "First name contains invalid characters. Only letters (including international characters), spaces, hyphens (-), apostrophes ('), and periods (.) are allowed."
        );
        #endregion

        #region Last Name errors
        public static Error LastNameRequired =>
            Error.Validation(
                code: $"User.LastNameRequired",
                description: "Last name is required.");

        public static Error LastNameTooShort =>
            Error.Validation(
                code: $"User.LastNameTooShort",
                description: $"Last name must be at least {Constraints.NameMinLength} characters long.");

        public static Error LastNameTooLong =>
            Error.Validation(
                code: $"User.LastNameTooLong",
                description: $"Last name must be at most {Constraints.NameMaxLength} characters long.");

        public static Error LastNameInvalidFormat =>
            Error.Validation($"User.LastNameInvalidFormat",
                "Last name contains invalid characters. Only letters (including international characters), spaces, hyphens (-), apostrophes ('), and periods (.) are allowed.");
        #endregion

        #region Phone Number errors
        public static Error PhoneNumberRequired => Error.Validation(code: $"User.PhoneNumberRequired",
                description: "Phone number is required.");

        public static Error PhoneNumberTooShort =>
            Error.Validation($"User.PhoneNumberTooShort",
                $"Phone number must be at least {Constraints.PhoneNumberMinLength} digits long.");

        public static Error PhoneNumberTooLong =>
            Error.Validation($"User.PhoneNumberTooLong",
                $"Phone number must be at most {Constraints.PhoneNumberMaxLength} digits long.");

        public static Error PhoneNumberInvalidFormat =>
            Error.Validation($"User.PhoneNumberInvalidFormat",
                "Phone number must be in valid E.164 format (e.g., +123456789).");

        public static Error PhoneNumberAlreadyExists =>
            Error.Conflict($"User.PhoneNumberAlreadyExists",
                "A user with this phone number already exists.");
        #endregion

        #region Email errors
        public static Error EmailRequired =>
            Error.Validation($"User.EmailRequired", "Email is required.");

        public static Error EmailTooLong =>
            Error.Validation($"User.EmailTooLong",
                $"Email must be at most {Constraints.EmailMaxLength} characters long.");

        public static Error
            EmailInvalidFormat =>
            Error.Validation($"User.EmailInvalidFormat",
                "Email address is not in a valid format.");

        public static Error EmailAlreadyExists(string email = "") =>
            Error.Conflict($"User.EmailAlreadyExists",
                $"A user with the email '{email}' already exists.");

        public static Error EmailNotConfirmed => Error.Validation(
            code: "Auth.EmailNotConfirmed",
            description: "Email address is not confirmed.");

        public static Error SameEmailNotAllowed =>
            Error.Validation($"User.SameEmailNotAllowed",
                "New email cannot be the same as the current email.");
        #endregion

        #region Password errors
        public static Error PasswordRequired =>
            Error.Validation($"User.PasswordRequired",
                "Password is required.");

        public static Error PasswordTooShort =>
            Error.Validation($"User.PasswordTooShort",
                $"Password must be at least {Constraints.PasswordMinLength} characters long.");

        public static Error PasswordTooLong =>
            Error.Validation($"User.PasswordTooLong",
                $"Password must be at most {Constraints.PasswordMaxLength} characters long.");

        public static Error PasswordInvalidFormat =>
            Error.Validation($"User.PasswordInvalidFormat",
                "Password must contain at least one uppercase letter, one lowercase letter, one digit, and one special character.");

        public static Error SamePasswordNotAllowed =>
            Error.Validation($"User.SamePasswordNotAllowed",
                "New password cannot be the same as the current password.");

        public static Error NewPasswordRequired =>
            Error.Validation($"User.NewPasswordRequired",
                "New password is required.");

        public static Error ConfirmPasswordRequired =>
            Error.Validation($"User.ConfirmPasswordRequired",
                "Confirm password is required.");

        public static Error ConfirmPasswordMismatch =>
            Error.Validation($"User.PasswordsDoNotMatch",
                "Password and confirm password do not match.");

        public static Error PasswordExpired =>
            Error.Validation($"User.PasswordExpired",
                "Password has expired and must be changed.");

        public static Error
            WeakPassword =>
            Error.Validation($"User.WeakPassword",
                "Password is too weak. Please choose a stronger password.");
        #endregion

        #region Two-Factor Authentication errors
        public static Error TwoFactorRequired => Error.Validation(
               code: $"User.TwoFactorRequired",
               description: "Two-factor authentication is required.");

        public static Error TwoFactorCodeRequired =>
            Error.Validation($"User.TwoFactorCodeRequired",
                "Two-factor authentication code is required.");

        public static Error TwoFactorCodeInvalidFormat =>
            Error.Validation($"User.TwoFactorCodeInvalidFormat",
                $"Two-factor authentication code must be exactly {Constraints.TwoFactorCodeLength} digits.");

        public static Error TwoFactorCodeExpired =>
            Error.Validation($"User.TwoFactorCodeExpired",
                "Two-factor authentication code has expired.");

        public static Error TwoFactorCodeInvalid =>
            Error.Validation($"User.TwoFactorCodeInvalid",
                "Invalid two-factor authentication code.");

        public static Error TwoFactorRecoveryCodeRequired =>
            Error.Validation($"User.TwoFactorRecoveryCodeRequired",
                "Two-factor recovery code is required.");

        public static Error TwoFactorRecoveryCodeInvalidFormat => Error.Validation(
            $"User.TwoFactorRecoveryCodeInvalidFormat",
            $"Two-factor recovery code must be exactly {Constraints.TwoFactorRecoveryCodeLength} uppercase letters or digits.");

        public static Error TwoFactorRecoveryCodeInvalid =>
            Error.Validation($"User.TwoFactorRecoveryCodeInvalid",
                "Invalid two-factor recovery code.");

        public static Error TwoFactorNotEnabled =>
            Error.Validation($"User.TwoFactorNotEnabled",
                "Two-factor authentication is not enabled for this account.");

        public static Error TwoFactorAlreadyEnabled =>
            Error.Validation($"User.TwoFactorAlreadyEnabled",
                "Two-factor authentication is already enabled for this account.");
        #endregion

        #region Email Confirmation errors
        public static Error ConfirmationCodeRequired =>
            Error.Validation($"User.ConfirmationCodeRequired",
                "Confirmation code is required.");

        public static Error ConfirmationCodeExpired =>
            Error.Validation($"User.ConfirmationCodeExpired",
                "Email confirmation code has expired.");

        public static Error
            ConfirmationCodeInvalid =>
            Error.Validation($"User.ConfirmationCodeInvalid",
                "Invalid email confirmation code.");

        public static Error EmailAlreadyConfirmed =>
            Error.Validation($"User.EmailAlreadyConfirmed",
                "Email address is already confirmed.");
        #endregion

        #region Password Reset errors
        public static Error ResetPasswordCodeRequired => Error.Validation(
            code: $"User.ResetPasswordCodeRequired",
            description: "Reset password code is required.");

        public static Error ResetPasswordCodeExpired => Error.Validation(
            code: $"User.ResetPasswordCodeExpired",
            description: "Password reset code has expired.");

        public static Error ResetPasswordCodeInvalid => Error.Validation(
            code: $"User.ResetPasswordCodeInvalid",
            description: "Invalid password reset code.");
        #endregion

        #region Authentication & Authorization errors
        public static Error InvalidCredentials => Error.Validation(
            code: "Auth.InvalidCredentials",
            description: "Invalid email or password.");

        public static Error LockedOut => Error.Validation(
            code: "Auth.LockedOut",
            description: "Account is locked out.");

        public static Error TooManyLoginAttempts => Error.Validation(
            code: $"User.TooManyLoginAttempts",
            description: "Too many failed login attempts. Please try again later.");


        public static Error AccountSuspended => Error.Validation(
            code: $"User.AccountSuspended",
            description: "Account has been suspended.");

        public static Error SessionExpired => Error.Validation(
            code: "Auth.SessionExpired",
            description: "Session has expired. Please log in again.");

        public static Error InsufficientPermissions => Error.Validation(
            code: "Auth.InsufficientPermissions",
            description: "You do not have permission to perform this action.");
        #endregion

        #region Token errors
        public static Error DecodeTokenFailed => Error.Validation(
            code: $"User.DecodeTokenFailed",
            description: "Invalid or expired token.");

        public static Error InvalidToken => Error.Validation(
            code: $"User.InvalidToken",
            description: "Invalid or expired token.");

        public static Error TokenExpired => Error.Validation(
            code: $"User.TokenExpired",
            description: "Token has expired.");

        public static Error RefreshTokenRequired => Error.Validation(
            code: $"User.RefreshTokenRequired",
            description: "Refresh token is required.");

        public static Error RefreshTokenInvalid => Error.Validation(
            code: "Auth.RefreshTokenInvalid",
            description: "Invalid refresh token.");

        public static Error RefreshTokenExpired => Error.Validation(
            code: "Auth.RefreshTokenExpired",
            description: "Refresh token has expired.");
        #endregion

        #region Profile & Account Management errors
        public static Error ProfileUpdateFailed => Error.Validation(
            code: $"User.ProfileUpdateFailed",
            description: "Failed to update user profile.");

        public static Error AvatarUploadFailed => Error.Validation(
            code: $"User.AvatarUploadFailed",
            description: "Failed to upload avatar image.");

        public static Error InvalidFileFormat => Error.Validation(
            code: $"User.InvalidFileFormat",
            description: "Invalid file format. Only JPEG, PNG, and GIF files are allowed.");

        public static Error FileTooLarge => Error.Validation(
            code: $"User.FileTooLarge",
            description: "File size exceeds the maximum allowed limit.");

        public static Error DeletionNotAllowed => Error.Validation(
            code: $"User.DeletionNotAllowed",
            description: "Account deletion is not allowed at this time.");

        public static Error DeactivationNotAllowed =>
            Error.Validation($"User.DeactivationNotAllowed",
                "Account deactivation is not allowed at this time.");
        #endregion

        #region External Authentication errors
        public static Error ExternalLoginFailed => Error.Validation(
            code: "Auth.ExternalLoginFailed",
            description: "External login authentication failed.");

        public static Error ExternalProviderError => Error.Validation(
            code: "Auth.ExternalProviderError",
            description: "External provider returned an error.");

        public static Error MissingExternalLoginInfo => Error.Validation(
            code: "Auth.MissingExternalLoginInfo",
            description: "Missing external login information.");

        public static Error ExternalLoginAlreadyAssociated => Error.Conflict(
            code: "Auth.ExternalLoginAlreadyAssociated",
            description: "This external login is already associated with another account.");

        public static Error CallbackUrlRequired => Error.Validation(
            code: "Auth.CallbackUrlRequired",
            description: "Callback URL is required for external login.");

        public static Error ExternalProviderNotSupported(string provider = "") => Error.Validation(
            code: "Auth.ExternalProviderNotSupported",
            description: $"External provider '{provider}' is not supported.");

        public static Error ExternalProviderNotConfigured(string provider = "") => Error.Validation(
            code: "Auth.ExternalProviderNotConfigured",
            description: $"External provider '{provider}' is not properly configured.");

        public static Error ExternalProviderDisabled(string provider = "") => Error.Validation(
            code: "Auth.ExternalProviderDisabled",
            description: $"External provider '{provider}' is currently disabled.");

        public static Error InvalidCallbackUrl => Error.Validation(
            code: "Auth.InvalidCallbackUrl",
            description: "Invalid callback URL format.");

        public static Error CallbackUrlTooLong => Error.Validation(
            code: "Auth.CallbackUrlTooLong",
            description: $"Callback URL must be at most {Constraints.CallbackUrlMaxLength} characters long.");

        public static Error StateParameterRequired => Error.Validation(
            code: "Auth.StateParameterRequired",
            description: "State parameter is required for external login.");

        public static Error StateParameterMismatch => Error.Validation(
            code: "Auth.StateParameterMismatch",
            description: "State parameter mismatch. Possible CSRF attack.");

        public static Error StateParameterTooLong => Error.Validation(
            code: "Auth.StateParameterTooLong",
            description: $"State parameter must be at most {Constraints.StateParameterMaxLength} characters long.");

        public static Error ExternalProviderIdRequired => Error.Validation(
            code: "Auth.ExternalProviderIdRequired",
            description: "External provider user ID is required.");

        public static Error ExternalProviderIdTooLong => Error.Validation(
            code: "Auth.ExternalProviderIdTooLong",
            description: $"External provider user ID must be at most {Constraints.ExternalProviderIdMaxLength} characters long.");

        public static Error ExternalLoginNotFound => Error.NotFound(
            code: "Auth.ExternalLoginNotFound",
            description: "External login not found for this user.");

        public static Error ExternalLoginRemovalFailed => Error.Validation(
            code: "Auth.ExternalLoginRemovalFailed",
            description: "Failed to remove external login.");

        public static Error ExternalLoginAdditionFailed => Error.Validation(
            code: "Auth.ExternalLoginAdditionFailed",
            description: "Failed to add external login.");

        public static Error CannotRemoveLastExternalLogin => Error.Validation(
            code: "Auth.CannotRemoveLastExternalLogin",
            description: "Cannot remove the last external login. Set a password first or add another external login.");

        public static Error ExternalProviderAccessDenied => Error.Validation(
            code: "Auth.ExternalProviderAccessDenied",
            description: "Access was denied by the external provider.");

        public static Error ExternalProviderTimeout => Error.Validation(
            code: "Auth.ExternalProviderTimeout",
            description: "External provider request timed out.");

        public static Error ExternalProviderRateLimited => Error.Validation(
            code: "Auth.ExternalProviderRateLimited",
            description: "External provider rate limit exceeded. Please try again later.");

        public static Error ExternalProviderScopeInsufficient => Error.Validation(
            code: "Auth.ExternalProviderScopeInsufficient",
            description: "Insufficient permissions granted by external provider.");

        public static Error ExternalProviderEmailNotVerified => Error.Validation(
            code: "Auth.ExternalProviderEmailNotVerified",
            description: "Email address from external provider is not verified.");

        public static Error ExternalProviderAccountSuspended => Error.Validation(
            code: "Auth.ExternalProviderAccountSuspended",
            description: "External provider account is suspended or disabled.");
        #endregion

    }
}