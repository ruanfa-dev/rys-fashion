using Core.Identity;
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
    public sealed class Handler : ICommandHandler<Command, Updated>
    {
        private readonly UserManager<User> _userManager;
        private readonly IUserContext _userContext;

        public Handler(UserManager<User> userManager, IUserContext userContext)
        {
            _userManager = userManager;
            _userContext = userContext;
        }

        public async Task<ErrorOr<Updated>> Handle(Command request, CancellationToken cancellationToken)
        {
            // Load: user context
            var userId = _userContext.UserId;
            var isAuthenticated = _userContext.IsAuthenticated;

            // Check: user is authenticated
            if (userId is null || !isAuthenticated)
                return User.Errors.UserUnauthorized;

            // Check: user exists
            var user = await _userManager.FindByIdAsync(userId.Value.ToString());
            if (user is null)
                return User.Errors.UserNotFound;

            // Check: current password is correct
            var param = request.Param;
            var result = await _userManager.ChangePasswordAsync(user, currentPassword: param.CurrentPassword, newPassword: param.NewPassword);
            if (!result.Succeeded)
            {
                return result.Errors.ToApplicationResult(fallbackCode: "");
            }

            return Result.Updated;
        }
    }
}
