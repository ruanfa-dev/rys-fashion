using ErrorOr;

using SharedKernel.Messaging.Abstracts;

using UseCases.Common.Security.Authentication.Services;
using UseCases.Common.Security.Authentication.Models;

namespace UseCases.Admin.Roles.Update;

public static partial class UpdateRoleCommand
{
    internal sealed class Handler(IKeycloakAdminService keycloakService)
        : ICommandHandler<Command, UpdateRoleResult>
    {
        public async Task<ErrorOr<UpdateRoleResult>> Handle(Command request, CancellationToken cancellationToken)
        {
            try
            {
                var param = request.Param;
                var updateRequest = new UpdateKeycloakRoleRequest
                {
                    Name = param.Name ?? request.RoleName,
                    Description = param.Description
                };

                await keycloakService.UpdateRoleAsync(request.RoleName, updateRequest, cancellationToken);

                return new UpdateRoleResult { Name = updateRequest.Name, Success = true };
            }
            catch (Exception ex)
            {
                return Error.Failure("UpdateRole.Failed", $"Failed to update role: {ex.Message}");
            }
        }
    }
}