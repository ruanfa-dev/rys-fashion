using SharedKernel.Messaging.Abstracts;
using SharedKernel.Models.PagedLists;
using SharedKernel.Models.Queries;

namespace UseCases.Admin.Roles.List;

public static partial class ListRoles
{
    internal const string Route = "";
    internal const string Tag = "Role Management";
    internal const string Name = "ListRoles";
    internal const string Summary = "List roles with pagination";
    internal const string Description = "Retrieves a paginated list of roles with their information";
}