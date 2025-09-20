using ErrorOr;

using SharedKernel.Messaging.Abstracts;

using UseCases.Common.Security.Authentication.Services;

namespace UseCases.Admin.Roles.GetByName;

public static partial class GetRoleByNameQuery
{
    internal sealed class Handler(IKeycloakAdminService keycloakService)
        : IQueryHandler<Query, RoleDetailResponse>
    {
        public async Task<ErrorOr<RoleDetailResponse>> Handle(Query request, CancellationToken cancellationToken)
        {
            try
            {
                var role = await keycloakService.GetRoleByNameAsync(request.RoleName, cancellationToken);
                if (role is null)
                {
                    return Error.NotFound("Role.NotFound", $"Role with name {request.RoleName} not found");
                }

                return new RoleDetailResponse
                {
                    Id = role.Id,
                    Name = role.Name,
                    Description = role.Description,
                    Composite = role.Composite,
                    ClientRole = role.ClientRole,
                    ContainerId = role.ContainerId
                };
            }
            catch (Exception ex)
            {
                return Error.Failure("GetRoleByName.Failed", $"Failed to retrieve role: {ex.Message}");
            }
        }
    }
}