using Core.Identity.Permissions;

namespace UseCases.Common.Security.Authorization.Permissions;
public static partial class Feature
{
    public static partial class Testing
    {
        public static readonly Permission[] All =
        [
            .. TodoLists.All,
            .. TodoItems.All
        ];
    }
}
