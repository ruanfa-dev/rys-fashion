namespace UseCases.Common.Security.Authorization.Permissions;
public static partial class Feature
{
    public static partial class Testing
    {
        public static partial class Todo
        {
            public static readonly string[] All = [.. Items.All, .. Lists.All];
        }
    }
}
