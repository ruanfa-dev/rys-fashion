using SharedKernel.Messaging.Abstracts;

using UseCases.Admin.Roles.Common;

namespace UseCases.Admin.Roles.GetById;

public static partial class GetRoleById
{
    internal const string Route = "/{id:guid}";
    internal const string Name = "GetRoleById";
    internal const string Summary = "Get role by ID";
    internal const string Description = "Retrieves detailed information about a specific role including permissions and user count";
}