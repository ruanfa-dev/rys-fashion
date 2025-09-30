using Core.Identity.Users;

using ErrorOr;

using FluentValidation;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

using Serilog;

using SharedKernel.Messaging.Abstracts;

using UseCases.Accounts.Common;
using UseCases.Common.Security.Authentication.Contexts;

namespace UseCases.Accounts.Phone.Confirm;
public static partial class ConfirmPhoneChange
{
    public sealed record Command(Param Param) : ICommand<Updated>;
    public sealed class CommandValidator : AbstractValidator<Command>
    {
        public CommandValidator()
        {
            RuleFor(x => x.Param).SetValidator(new ParamValidator());
        }
    }

    public sealed class Handler(
        UserManager<User> userManager,
        IUserContext userContext)
        : ICommandHandler<Command, Updated>
    {
        public async Task<ErrorOr<Updated>> Handle(Command request, CancellationToken cancellationToken)
        {
            // Load: user context
            Guid? userId = userContext.UserId;
            bool isAuthenticated = userContext.IsAuthenticated;

            // Check: user is authenticated
            if (userId is null || !isAuthenticated)
                return User.Errors.UserUnauthorized;

            // Check: user exists
            User? user = await userManager.FindByIdAsync(userId.Value.ToString());
            if (user is null)
                return User.Errors.UserNotFound;

            Param param = request.Param;

            // Check: new phone is not already in use by another user
            IQueryable<User> existingUserQuery = userManager.Users.Where(u => u.PhoneNumber == param.NewPhone && u.Id != user.Id);
            User? existingUser = await existingUserQuery.FirstOrDefaultAsync(cancellationToken);
            if (existingUser != null)
                return User.Errors.PhoneNumberAlreadyExists(param.NewPhone);

            // Verify: confirmation code and change phone number
            IdentityResult changeResult = await userManager.ChangePhoneNumberAsync(user, param.NewPhone, param.Code);
            if (!changeResult.Succeeded)
            {
                Log.Information("Failed to change phone number for user {UserId} to {NewPhone}: {Errors}",
                    userId, param.NewPhone, string.Join(", ", changeResult.Errors.Select(e => e.Description)));
                return changeResult.Errors.ToApplicationResult(fallbackCode: "ChangePhoneNumber.Failed");
            }

            Log.Information("Phone number successfully changed for user {UserId} from {OldPhone} to {NewPhone}",
                userId, user.PhoneNumber, param.NewPhone);

            return Result.Updated;
        }
    }
}
