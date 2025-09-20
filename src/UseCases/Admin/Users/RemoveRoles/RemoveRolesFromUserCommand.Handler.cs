using ErrorOr;

using SharedKernel.Messaging.Abstracts;

using UseCases.Common.Security.Authentication.Services;

namespace UseCases.Admin.Users.RemoveRoles;

public static partial class RemoveRolesFromUserCommand
{
    internal sealed class Handler(IKeycloakAdminService keycloakService)
        : ICommandHandler<Command, RemoveRolesResult>
    {
        public async Task<ErrorOr<RemoveRolesResult>> Handle(Command request, CancellationToken cancellationToken)
        {
            try
            {
                await keycloakService.RemoveRolesFromUserAsync(request.UserId, request.Param.RoleNames, cancellationToken);

                return new RemoveRolesResult 
                { 
                    UserId = request.UserId, 
                    RemovedRoles = request.Param.RoleNames.ToList(), 
                    Success = true 
                };
            }
            catch (Exception ex)
            {
                return Error.Failure("RemoveRoles.Failed", $"Failed to remove roles: {ex.Message}");
            }
        }
    }
}