namespace UseCases.Common.Security.Authorization.Permissions;
public static partial class Feature
{
    public static readonly string[] All =
    [
        .. Testing.All,
        .. Admin.All,
        .. Store.All
    ];
}
