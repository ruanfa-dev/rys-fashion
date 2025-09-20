using ErrorOr;

using SharedKernel.Messaging.Abstracts;

using UseCases.Common.Security.Authentication.Services;
using UseCases.Common.Security.Authentication.Models;

namespace UseCases.Admin.Users.Update;

public static partial class UpdateUserCommand
{
    internal sealed class Handler(IKeycloakAdminService keycloakService)
        : ICommandHandler<Command, UpdateUserResult>
    {
        public async Task<ErrorOr<UpdateUserResult>> Handle(Command request, CancellationToken cancellationToken)
        {
            try
            {
                var param = request.Param;
                var updateRequest = new UpdateKeycloakUserRequest
                {
                    Email = param.Email,
                    FirstName = param.FirstName,
                    LastName = param.LastName,
                    Enabled = param.Enabled,
                    EmailVerified = param.EmailVerified,
                    Attributes = param.Attributes
                };

                await keycloakService.UpdateUserAsync(request.Id, updateRequest, cancellationToken);

                return new UpdateUserResult { Id = request.Id, Success = true };
            }
            catch (Exception ex)
            {
                return Error.Failure("UpdateUser.Failed", $"Failed to update user: {ex.Message}");
            }
        }
    }
}