using Core.Identity.Users;

using ErrorOr;

using FluentValidation;

using Microsoft.AspNetCore.Identity;

using SharedKernel.Messaging.Abstracts;

using UseCases.Accounts.Common;
using UseCases.Common.Security.Authentication.Contexts;

namespace UseCases.Accounts.Password.Change;
public static partial class ChangePassword
{
    public sealed record Command(Param Param) : ICommand<Updated>;
    public sealed class CommandValidator : AbstractValidator<Command>
    {
        public CommandValidator()
        {
            RuleFor(x => x.Param).SetValidator(new ParamValidator());
        }
    }
    public sealed class Handler(UserManager<User> userManager, IUserContext userContext)
        : ICommandHandler<Command, Updated>
    {
        public async Task<ErrorOr<Updated>> Handle(Command request, CancellationToken cancellationToken)
        {
            // Load: user context
            var userId = userContext.UserId;
            var isAuthenticated = userContext.IsAuthenticated;

            // Check: user is authenticated
            if (userId is null || !isAuthenticated)
                return User.Errors.UserUnauthorized;

            // Check: user exists
            var user = await userManager.FindByIdAsync(userId.Value.ToString());
            if (user is null)
                return User.Errors.UserNotFound;

            // Check: current password is correct
            var param = request.Param;
            var result = await userManager.ChangePasswordAsync(user, currentPassword: param.CurrentPassword, newPassword: param.NewPassword);
            if (!result.Succeeded)
            {
                return result.Errors.ToApplicationResult(fallbackCode: "");
            }

            return Result.Updated;
        }
    }
}
