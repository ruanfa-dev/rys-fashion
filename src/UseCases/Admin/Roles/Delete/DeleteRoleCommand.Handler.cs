using ErrorOr;

using SharedKernel.Messaging.Abstracts;

using UseCases.Common.Security.Authentication.Services;

namespace UseCases.Admin.Roles.Delete;

public static partial class DeleteRoleCommand
{
    internal sealed class Handler(IKeycloakAdminService keycloakService)
        : ICommandHandler<Command, DeleteRoleResult>
    {
        public async Task<ErrorOr<DeleteRoleResult>> Handle(Command request, CancellationToken cancellationToken)
        {
            try
            {
                await keycloakService.DeleteRoleAsync(request.RoleName, cancellationToken);

                return new DeleteRoleResult { Name = request.RoleName, Success = true };
            }
            catch (Exception ex)
            {
                return Error.Failure("DeleteRole.Failed", $"Failed to delete role: {ex.Message}");
            }
        }
    }
}