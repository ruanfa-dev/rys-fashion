using ErrorOr;

using SharedKernel.Messaging.Abstracts;

using UseCases.Common.Security.Authentication.Services;

namespace UseCases.Admin.Users.Delete;

public static partial class DeleteUserCommand
{
    internal sealed class Handler(IKeycloakAdminService keycloakService)
        : ICommandHandler<Command, DeleteUserResult>
    {
        public async Task<ErrorOr<DeleteUserResult>> Handle(Command request, CancellationToken cancellationToken)
        {
            try
            {
                await keycloakService.DeleteUserAsync(request.Id, cancellationToken);

                return new DeleteUserResult { Id = request.Id, Success = true };
            }
            catch (Exception ex)
            {
                return Error.Failure("DeleteUser.Failed", $"Failed to delete user: {ex.Message}");
            }
        }
    }
}