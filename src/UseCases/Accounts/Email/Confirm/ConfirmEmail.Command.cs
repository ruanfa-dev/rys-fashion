using Core.Identity;

using ErrorOr;

using FluentValidation;

using Microsoft.AspNetCore.Identity;

using Serilog;

using SharedKernel.Messaging.Abstracts;

using UseCases.Accounts.Common;

namespace UseCases.Accounts.Email.Confirm;
public static partial class ConfirmEmail
{
    public sealed record Command(Param Param) : ICommand<Result>;
    public sealed record Result(string ConfirmMessage);
    public sealed class CommandValidator : AbstractValidator<Command>
    {
        public CommandValidator()
        {
            RuleFor(x => x.Param).SetValidator(new ParamValidator());
        }
    }
    public sealed class Handler(UserManager<User> userManager) : ICommandHandler<Command, Result>
    {
        public async Task<ErrorOr<Result>> Handle(Command request, CancellationToken cancellationToken)
        {
            var param = request.Param;

            // Validate: User existence
            var user = await userManager.FindByIdAsync(param.UserId.ToString());
            if (user == null)
            {
                Log.Warning("ConfirmEmail: User {UserId} not found", param.UserId);
                return User.Errors.UserNotFound;
            }

            // Decode: token with enhanced error handling
            var decodedTokenResult = param.Code.DecodeToken();
            if (decodedTokenResult.IsError)
            {
                Log.Warning("ConfirmEmail: Token decoding failed for user {UserId}", param.UserId);
                return decodedTokenResult.Errors;
            }

            string decodedToken = decodedTokenResult.Value;

            // Determine: confirmation scenario
            return string.IsNullOrWhiteSpace(param.ChangedEmail)
                ? await HandleInitialEmailConfirmationAsync(user, decodedToken, userId: param.UserId)
                : await HandleEmailChangeConfirmationAsync(user, decodedToken, changedEmail: param.ChangedEmail, userId: param.UserId);
        }

        /// <summary>
        /// Handles initial email confirmation after registration
        /// </summary>
        private async Task<ErrorOr<Result>> HandleInitialEmailConfirmationAsync(
            User user,
            string decodedToken,
            string userId)
        {
            // Check: Email already confirmed
            if (await userManager.IsEmailConfirmedAsync(user))
            {
                Log.Information("ConfirmEmail: User {UserId} email already confirmed", userId);
                return new Result("Your email address is already confirmed.");
            }

            // Confirm: email address
            var result = await userManager.ConfirmEmailAsync(user, decodedToken);
            if (!result.Succeeded)
            {
                Log.Warning("ConfirmEmail: Initial confirmation failed for user {UserId}: {Errors}",
                    userId, string.Join(", ", result.Errors.Select(e => e.Description)));
                return result.Errors.ToApplicationResult(nameof(ConfirmEmail), "ConfirmEmailFailed");
            }

            Log.Information("ConfirmEmail: Initial email confirmed for user {UserId}", userId);
            return new Result("Thank you for confirming your email address. Your account is now active.");
        }

        /// <summary>
        /// Handles email change confirmation
        /// </summary>
        private async Task<ErrorOr<Result>> HandleEmailChangeConfirmationAsync(
            User user,
            string decodedToken,
            string changedEmail,
            string userId)
        {
            // Additional Security: Check if target email is already in use
            var existingUser = await userManager.FindByEmailAsync(changedEmail);
            if (existingUser != null && existingUser.Id != user.Id)
            {
                Log.Warning("ConfirmEmail: Email change blocked for user {UserId} - email {Email} already in use by user {ExistingUserId}",
                    userId, changedEmail, existingUser.Id);

                return User.Errors.EmailAlreadyExists(changedEmail);
            }

            // Store original email for logging
            var originalEmail = user.Email;

            // Execute: email change
            var changeResult = await userManager.ChangeEmailAsync(user, changedEmail, decodedToken);
            if (!changeResult.Succeeded)
            {
                Log.Warning("ConfirmEmail: Email change failed for user {UserId}: {Errors}",
                    userId, string.Join(", ", changeResult.Errors.Select(e => e.Description)));
                return changeResult.Errors.ToApplicationResult(nameof(ConfirmEmail), "ChangeEmailFailed");
            }

            // Update: username to match new email if it was previously the same
            await UpdateUsernameIfNeededAsync(user, originalEmail, changedEmail, userId);

            Log.Information("ConfirmEmail: Email successfully changed for user {UserId} from {OldEmail} to {NewEmail}",
                userId, originalEmail, changedEmail);

            return new Result("Your email address has been successfully changed. Please use your new email address for future logins.");
        }

        /// <summary>
        /// Updates username to match new email if it was previously the same as the old email
        /// </summary>
        private async Task UpdateUsernameIfNeededAsync(User user, string? originalEmail, string newEmail, string userId)
        {
            if (originalEmail != null && string.Equals(user.UserName, originalEmail, StringComparison.OrdinalIgnoreCase))
            {
                var setUserNameResult = await userManager.SetUserNameAsync(user, newEmail);
                if (!setUserNameResult.Succeeded)
                {
                    Log.Warning("ConfirmEmail: Failed to update username for user {UserId}: {Errors}",
                        userId, string.Join(", ", setUserNameResult.Errors.Select(e => e.Description)));
                }
                else
                {
                    Log.Information("ConfirmEmail: Username updated to match new email for user {UserId}", userId);
                }
            }
        }
    }
}