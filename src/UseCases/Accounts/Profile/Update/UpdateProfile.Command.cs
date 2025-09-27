using Core.Identity;
using Core.Identity.Users;

using ErrorOr;

using FluentValidation;

using Microsoft.AspNetCore.Identity;

using SharedKernel.Messaging.Abstracts;

using UseCases.Accounts.Common;
using UseCases.Accounts.Profile.Common;
using UseCases.Common.Persistence.Context;
using UseCases.Common.Security.Authentication.Contexts;

namespace UseCases.Accounts.Profile.Update;
public static partial class UpdateProfile
{
    public sealed record Command(AccountProfileParam Param) : ICommand<Updated>;

    public sealed class CommandValidator : AbstractValidator<Command>
    {
        public CommandValidator()
        {
            RuleFor(x => x.Param).SetValidator(new AccountProfileParamValidator());
        }
    }

    public sealed class Handler : ICommandHandler<Command, Updated>
    {
        private readonly IUserContext _userContext;
        private readonly UserManager<User> _userManager;
        private readonly IUnitOfWork _unitOfWork;

        public Handler(
            IUserContext userContext,
            UserManager<User> userManager,
            IUnitOfWork unitOfWork)
        {
            _userContext = userContext;
            _userManager = userManager;
            _unitOfWork = unitOfWork;
        }

        public async Task<ErrorOr<Updated>> Handle(Command command, CancellationToken cancellationToken)
        {
            try
            {
                // Load: user context
                var userId = _userContext.UserId;
                var isAuthenticated = _userContext.IsAuthenticated;

                // Check: user is authenticated
                if (userId is null || !isAuthenticated)
                    return User.Errors.UserUnauthorized;

                // Get: user
                var user = await _userManager.FindByIdAsync(userId.Value.ToString());
                if (user is null)
                    return User.Errors.UserNotFound;

                var param = command.Param;

                // Begin: transaction
                await _unitOfWork.BeginTransactionAsync(cancellationToken);

                // Check: username uniqueness
                if (!string.IsNullOrWhiteSpace(param.UserName) && param.UserName != user.UserName)
                {
                    var existingUser = await _userManager.FindByNameAsync(param.UserName);
                    if (existingUser is not null && existingUser.Id != user.Id)
                    {
                        await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                        return User.Errors.UserNameAlreadyExists(param.UserName);
                    }
                }

                // Update: profile fields
                if (!string.IsNullOrWhiteSpace(param.UserName))
                    user.UserName = param.UserName;
                if (!string.IsNullOrWhiteSpace(param.FirstName))
                    user.FirstName = param.FirstName;
                if (!string.IsNullOrWhiteSpace(param.LastName))
                    user.LastName = param.LastName;
                if (!string.IsNullOrWhiteSpace(param.ProfileImagePath))
                    user.ProfileImagePath = param.ProfileImagePath;

                var result = await _userManager.UpdateAsync(user);
                if (!result.Succeeded)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return result.Errors.ToApplicationResult();
                }

                await _unitOfWork.Context.SaveChangesAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync(cancellationToken);

                return new Updated();
            }
            catch (Exception)
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                return Error.Unexpected(
                    code: "UpdateProfile.Unexpected",
                    description: "An unexpected error occurred while updating the user profile."
                );
            }
        }
    }
}
