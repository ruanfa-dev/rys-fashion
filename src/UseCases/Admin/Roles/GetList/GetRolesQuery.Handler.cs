using ErrorOr;

using SharedKernel.Messaging.Abstracts;

using UseCases.Common.Security.Authentication.Services;

namespace UseCases.Admin.Roles.GetList;

public static partial class GetRolesQuery
{
    internal sealed class Handler(IKeycloakAdminService keycloakService)
        : IQueryHandler<Query, List<RoleResponse>>
    {
        public async Task<ErrorOr<List<RoleResponse>>> Handle(Query request, CancellationToken cancellationToken)
        {
            try
            {
                var roles = await keycloakService.GetRolesAsync(cancellationToken);
                
                var roleList = roles.Select(r => new RoleResponse
                {
                    Id = r.Id,
                    Name = r.Name,
                    Description = r.Description,
                    Composite = r.Composite,
                    ClientRole = r.ClientRole
                }).ToList();

                return roleList;
            }
            catch (Exception ex)
            {
                return Error.Failure("GetRoles.Failed", $"Failed to retrieve roles: {ex.Message}");
            }
        }
    }
}