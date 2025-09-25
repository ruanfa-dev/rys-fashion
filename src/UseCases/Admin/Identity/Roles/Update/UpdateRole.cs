namespace UseCases.Admin.Roles.Update;

public static partial class UpdateRole
{
    internal const string Route = "/{id:guid}";
    internal const string Tag = "Role Management";
    internal const string Name = "UpdateRole";
    internal const string Summary = "Update role information";
    internal const string Description = "Updates role description and properties (name cannot be changed for system roles)";
}