namespace UseCases.Admin.Roles.Delete;

public static partial class DeleteRole
{
    internal const string Route = "/{id:guid}";
    internal const string Name = "DeleteRole";
    internal const string Summary = "Delete a role";
    internal const string Description = "Permanently deletes a role (cannot delete system roles or roles with users)";
}