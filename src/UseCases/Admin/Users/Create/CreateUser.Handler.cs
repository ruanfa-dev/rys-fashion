using Core.Identity;

using ErrorOr;

using FluentValidation;

using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

using SharedKernel.Messaging.Abstracts;

using UseCases.Accounts.Common;
using UseCases.Admin.Users.Common;
using UseCases.Common.Persistence.Context;

namespace UseCases.Admin.Users.Create;

public static partial class CreateUser
{
    public sealed record Param : UserCreateParam;
    public sealed record Result : UserResult;
    public sealed record Command(Param Param) : ICommand<Result>;

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Param)
                .SetValidator(new UserCreateParamValidator());
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
                // Check: if user already exists
                var existingUser = await userManager.FindByEmailAsync(param.Email);
                if (existingUser != null)
                {
                    await unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return User.Errors.UserNameAlreadyExists(param.Email);
                }

                // Create: new user
                var user = User.Create(
                    email: param.Email,
                    emailConfirmed: param.EmailConfirmed,
                    userName: param.Email,
                    firstName: param.FirstName,
                    lastName: param.LastName,
                    profileImagePath: param.ProfileImagePath,
                    phoneNumber: param.PhoneNumber,
                    phoneNumberConfirmed: param.PhoneNumberConfirmed);

                // Create: new users
                var result = await userManager.CreateAsync(user, param.Password);
                if (!result.Succeeded)
                {
                    var errors = string.Join("; ", result.Errors.Select(e => e.Description));
                    logger.LogError("Failed to create user {Email}: {Errors}", param.Email, errors);
                    await unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return result.Errors.ToApplicationResult("User", "CreationFailed");
                }

                await unitOfWork.CommitTransactionAsync(cancellationToken);
                logger.LogInformation("Successfully created user {UserId} with email {Email}",
                    user.Id, param.Email);

                return new Result
                {
                    Id = user.Id,
                    Email = user.Email!,
                    UserName = user.UserName,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    PhoneNumber = user.PhoneNumber,
                    ProfileImagePath = user.ProfileImagePath,
                    EmailConfirmed = user.EmailConfirmed,
                    CreatedAt = user.CreatedAt,
                    CreatedBy = user.CreatedBy,
                    PhoneNumberConfirmed = user.PhoneNumberConfirmed
                };
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected error creating user with email {Email}", param.Email);
                return Error.Failure("User.UnexpectedError", "An unexpected error occurred while creating the user");
            }
        }
    }
}