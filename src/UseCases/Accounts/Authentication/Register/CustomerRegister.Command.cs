using Core.Identity;

using ErrorOr;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

using Serilog;

using SharedKernel.Messaging.Abstracts;

using UseCases.Accounts.Common;
using UseCases.Common.Notification.Services;
using UseCases.Common.Security.Authorization.Roles;

namespace UseCases.Accounts.Authentication.Register;
public static partial class CustomerRegister
{
    public record Command(Param Param) : ICommand<Guid>;
    public sealed class Handler(
        UserManager<User> userManager,
        RoleManager<Role> roleManager,
        INotificationService notificationService,
        IConfiguration configuration) : ICommandHandler<Command, Guid>
    {
        public async Task<ErrorOr<Guid>> Handle(Command request, CancellationToken cancellationToken)
        {
            // Check: user already exists by email
            var param = request.Param;
            var existingUser = await userManager.FindByEmailAsync(param.Email);
            if (existingUser != null)
            {
                Log.Warning("User with email {Email} already exists", param.Email);
                return User.Errors.EmailAlreadyExists(param.Email);
            }

            // Check: user already exists by username
            if (!string.IsNullOrWhiteSpace(param.UserName))
            {
                existingUser = await userManager.FindByNameAsync(param.UserName);
                if (existingUser != null)
                {
                    Log.Warning("User with username {UserName} already exists", param.UserName);
                    return User.Errors.UserNameAlreadyExists(param.UserName);
                }
            }

            // Check: phone number already exists if provided
            if (!string.IsNullOrWhiteSpace(param.PhoneNumber))
            {
                var existingUserByPhone = await userManager.Users
                    .FirstOrDefaultAsync(u => u.PhoneNumber == param.PhoneNumber, cancellationToken);
                if (existingUserByPhone != null)
                {
                    Log.Warning("User with phone number {PhoneNumber} already exists", param.PhoneNumber);
                    return Error.Conflict("Register.PhoneNumberAlreadyExists", "A user with this phone number already exists.");
                }
            }

            // Check: current user role is init
            if (!await roleManager.RoleExistsAsync(DefaultRole.Customer))
            {
                Log.Warning("Role {Role} not found", DefaultRole.Customer);
                return Role.Errors.DefaultRoleNotFound;
            }

            // Create: new user
            var user = new User
            {
                UserName = param.UserName ?? param.Email,
                Email = param.Email,
                FirstName = param.FirstName,
                LastName = param.LastName,
                PhoneNumber = param.PhoneNumber,
            };

            // Set: password
            var passwordResult = await userManager.CreateAsync(user, param.Password);
            if (!passwordResult.Succeeded)
                return passwordResult.Errors.ToApplicationResult(fallbackCode: "CreateUserFailed");

            // Assign: default customer role
            var roleResult = await userManager.AddToRoleAsync(user, DefaultRole.Customer);
            if (!roleResult.Succeeded)
            {
                // Rollback: user creation if role assignment fails
                await userManager.DeleteAsync(user);
                Log.Error("Failed to assign role {Role} to user {UserId}: {Errors}", DefaultRole.Customer, user.Id, roleResult.Errors);
                return roleResult.Errors.ToApplicationResult(fallbackCode: "AssignRoleFailed");
            }

            // Log: user registration
            Log.Information("User {UserId} registered successfully with email {Email}", user.Id, user.Email);

            // Send: confirmation email
            var emailResult = await userManager.GenerateAndSendConfirmationEmailAsync(
                notificationService: notificationService,
                configuration: configuration,
                user: user,
                cancellationToken: cancellationToken);

            if (emailResult.IsError)
            {
                // Rollback: user creation if email sending fails
                await userManager.DeleteAsync(user);
                Log.Error("Failed to send confirmation email to {Email}: {Errors}", param.Email, emailResult.Errors);
                return emailResult.Errors;
            }

            // Send: confirmation SMS if phone number is provided
            if (!string.IsNullOrWhiteSpace(param.PhoneNumber))
            {
                var smsResult = await userManager.GenerateAndSendConfirmationSmsAsync(
                    notificationService: notificationService,
                    configuration: configuration,
                    user: user,
                    cancellationToken: cancellationToken);

                if (smsResult.IsError)
                {
                    // Don't rollback user creation for SMS failure, just log the warning
                    Log.Warning("Failed to send confirmation SMS to {PhoneNumber} for user {UserId}: {Errors}",
                        param.PhoneNumber, user.Id, string.Join(", ", smsResult.Errors.Select(e => e.Description)));
                }
                else
                {
                    Log.Information("Confirmation SMS sent to {PhoneNumber} for user {UserId}", param.PhoneNumber, user.Id);
                }
            }

            // Return: user ID
            return user.Id;
        }
    }
}
