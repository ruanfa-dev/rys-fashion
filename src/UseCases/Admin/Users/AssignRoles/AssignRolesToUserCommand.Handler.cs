using ErrorOr;

using SharedKernel.Messaging.Abstracts;

using UseCases.Common.Security.Authentication.Services;

namespace UseCases.Admin.Users.AssignRoles;

public static partial class AssignRolesToUserCommand
{
    internal sealed class Handler(IKeycloakAdminService keycloakService)
        : ICommandHandler<Command, AssignRolesResult>
    {
        public async Task<ErrorOr<AssignRolesResult>> Handle(Command request, CancellationToken cancellationToken)
        {
            try
            {
                await keycloakService.AssignRolesToUserAsync(request.UserId, request.Param.RoleNames, cancellationToken);

                return new AssignRolesResult 
                { 
                    UserId = request.UserId, 
                    AssignedRoles = request.Param.RoleNames.ToList(), 
                    Success = true 
                };
            }
            catch (Exception ex)
            {
                return Error.Failure("AssignRoles.Failed", $"Failed to assign roles: {ex.Message}");
            }
        }
    }
}