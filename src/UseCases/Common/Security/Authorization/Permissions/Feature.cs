using Core.Identity.Permissions;

namespace UseCases.Common.Security.Authorization.Permissions;
public static partial class Feature
{
    public static readonly Permission[] Permissions =
    [
        .. Testing.All,
        .. Admin.All,
        //.. Store.All
    ];
}
