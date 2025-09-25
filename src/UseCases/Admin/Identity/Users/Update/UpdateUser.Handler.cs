using Core.Identity;

using ErrorOr;

using FluentValidation;

using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

using SharedKernel.Messaging.Abstracts;

using UseCases.Accounts.Common;
using UseCases.Admin.Identity.Users.Common;
using UseCases.Admin.Users.Common;
using UseCases.Common.Persistence.Context;

namespace UseCases.Admin.Users.Update;

public static partial class UpdateUser
{
    public sealed record Param : UserParam;
    public sealed record Result : UserResult;

    public sealed record Command(Guid Id, Param Param) : ICommand<Result>;
    public sealed class CommandValidator : AbstractValidator<Command>
    {
        public CommandValidator()
        {
            RuleFor(m => m.Id)
                .NotEmpty()
                .WithErrorCode(User.Errors.UserIdRequired.Code)
                .WithMessage(User.Errors.UserIdRequired.Description);
            RuleFor(x => x.Param)
                .SetValidator(new UserParamValidator());
        }
    }
    public sealed class Handler(
        UserManager<User> userManager,
        IUnitOfWork unitOfWork,
        ILogger<Handler> logger
    ) : ICommandHandler<Command, Result>
    {
        public async Task<ErrorOr<Result>> Handle(Command request, CancellationToken cancellationToken)
        {
            var param = request.Param;

            try
            {
                await unitOfWork.BeginTransactionAsync(cancellationToken);
                var user = await userManager.FindByIdAsync(request.Id.ToString());
                if (user == null)
                {
                    await unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return User.Errors.UserNotFound;
                }

                user = user.Update(
                  email: user.Email,
                  emailConfirmed: param.EmailConfirmed,
                  userName: user.UserName,
                  firstName: param.FirstName ?? user.FirstName,
                  lastName: param.LastName ?? user.LastName,
                  dateOfBirth: user.DateOfBirth,
                  profileImagePath: param.ProfileImagePath ?? user.ProfileImagePath,
                  phoneNumber: param.PhoneNumber ?? user.PhoneNumber,
                  phoneNumberConfirmed: user.PhoneNumberConfirmed);
                var result = await userManager.UpdateAsync(user);

                if (!result.Succeeded)
                {
                    var errors = string.Join("; ", result.Errors.Select(e => e.Description));
                    logger.LogError("Failed to update user {UserId}: {Errors}", request.Id, errors);

                    await unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return result.Errors.ToApplicationResult("User", "UpdateUserFailed");
                }

                logger.LogInformation("Successfully updated user {UserId}", user.Id);

                await unitOfWork.CommitTransactionAsync(cancellationToken);
                return new Result
                {
                    Id = user.Id,
                    Email = user.Email!,
                    EmailConfirmed = user.EmailConfirmed,
                    UserName = user.UserName,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    PhoneNumber = user.PhoneNumber,
                    PhoneNumberConfirmed = user.PhoneNumberConfirmed,
                    ProfileImagePath = user.ProfileImagePath,
                    CreatedAt = user.CreatedAt,
                    CreatedBy = user.CreatedBy,
                };
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected error updating user {UserId}", request.Id);
                return Error.Failure("User.UnexpectedError", "An unexpected error occurred while updating the user");
            }
        }
    }
}