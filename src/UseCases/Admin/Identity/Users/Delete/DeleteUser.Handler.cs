using Core.Identity.Users;

using ErrorOr;

using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

using SharedKernel.Messaging.Abstracts;

using UseCases.Admin.Identity.Users.Common;
using UseCases.Common.Persistence.Context;

namespace UseCases.Admin.Identity.Users.Delete;

public static partial class DeleteUser
{
    public sealed record Result : UserResult.ListItem;
    public sealed record Command(Guid Id) : ICommand<Result>;
    public sealed class Handler(
        UserManager<User> userManager,
        IUnitOfWork unitOfWork,
        ILogger<Handler> logger
    ) : ICommandHandler<Command, Result>
    {
        public async Task<ErrorOr<Result>> Handle(Command request, CancellationToken cancellationToken)
        {
            try
            {
                await unitOfWork.BeginTransactionAsync(cancellationToken);
                var user = await userManager.FindByIdAsync(request.Id.ToString());
                if (user == null)
                {
                    await unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return User.Errors.UserNotFound;
                }

                // Store user info for response before deletion
                var userEmail = user.Email!;
                var userId = user.Id;

                // Delete: the user
                var result = await userManager.DeleteAsync(user);
                if (!result.Succeeded)
                {
                    var errors = string.Join("; ", result.Errors.Select(e => e.Description));
                    logger.LogError("Failed to delete user {UserId}: {Errors}", request.Id, errors);
                    await unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return Error.Failure("User.DeletionFailed", $"Failed to delete user: {errors}");
                }
                await unitOfWork.CommitTransactionAsync(cancellationToken);
                logger.LogInformation("Successfully deleted user {UserId} with email {Email}", userId, userEmail);

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
                logger.LogError(ex, "Unexpected error deleting user {UserId}", request.Id);
                await unitOfWork.RollbackTransactionAsync(cancellationToken);
                return Error.Failure("User.UnexpectedError", "An unexpected error occurred while deleting the user");
            }
        }
    }
}