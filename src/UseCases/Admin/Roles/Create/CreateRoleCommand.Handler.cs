using ErrorOr;

using SharedKernel.Messaging.Abstracts;

using UseCases.Common.Security.Authentication.Services;
using UseCases.Common.Security.Authentication.Models;

namespace UseCases.Admin.Roles.Create;

public static partial class CreateRoleCommand
{
    internal sealed class Handler(IKeycloakAdminService keycloakService)
        : ICommandHandler<Command, CreateRoleResult>
    {
        public async Task<ErrorOr<CreateRoleResult>> Handle(Command request, CancellationToken cancellationToken)
        {
            try
            {
                var param = request.Param;
                var createRequest = new CreateKeycloakRoleRequest
                {
                    Name = param.Name,
                    Description = param.Description
                };

                await keycloakService.CreateRoleAsync(createRequest, cancellationToken);

                return new CreateRoleResult { Name = param.Name };
            }
            catch (Exception ex)
            {
                return Error.Failure("CreateRole.Failed", $"Failed to create role: {ex.Message}");
            }
        }
    }
}