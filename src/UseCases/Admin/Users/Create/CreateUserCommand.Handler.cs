using ErrorOr;

using SharedKernel.Messaging.Abstracts;

using UseCases.Common.Security.Authentication.Services;
using UseCases.Common.Security.Authentication.Models;

namespace UseCases.Admin.Users.Create;

public static partial class CreateUserCommand
{
    internal sealed class Handler(IKeycloakAdminService keycloakService)
        : ICommandHandler<Command, CreateUserResult>
    {
        public async Task<ErrorOr<CreateUserResult>> Handle(Command request, CancellationToken cancellationToken)
        {
            try
            {
                var param = request.Param;
                var createRequest = new CreateKeycloakUserRequest
                {
                    Username = param.Username,
                    Email = param.Email,
                    FirstName = param.FirstName,
                    LastName = param.LastName,
                    Enabled = param.Enabled,
                    EmailVerified = param.EmailVerified,
                    Attributes = param.Attributes
                };

                var userId = await keycloakService.CreateUserAsync(createRequest, cancellationToken);

                // Set password if provided
                if (!string.IsNullOrEmpty(param.Password))
                {
                    await keycloakService.SetUserPasswordAsync(userId, param.Password, param.TemporaryPassword, cancellationToken);
                }

                // Assign roles if provided
                if (param.RoleNames?.Any() == true)
                {
                    await keycloakService.AssignRolesToUserAsync(userId, param.RoleNames, cancellationToken);
                }

                return new CreateUserResult { Id = userId };
            }
            catch (Exception ex)
            {
                return Error.Failure("CreateUser.Failed", $"Failed to create user: {ex.Message}");
            }
        }
    }
}