using Core.Identity.Roles;

using ErrorOr;

using FluentValidation;

using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

using SharedKernel.Messaging.Abstracts;

using UseCases.Admin.Identity.Roles.Common;

namespace UseCases.Admin.Identity.Roles.Update;

public static partial class UpdateRole
{

    public sealed record Command(Guid Id, Param Param) : ICommand<Result>;

    public sealed record Param : RoleParam;
    public sealed record Result : RoleResult.ListItem;
    public sealed class CommandValidator : AbstractValidator<Command>
    {
        public CommandValidator()
        {
            RuleFor(x => x.Param)
                .SetValidator(new RoleParamValidator());
        }
    }
    public sealed class Handler(
        RoleManager<Role> roleManager,
        ILogger<Handler> logger
    ) : ICommandHandler<Command, Result>
    {
        public async Task<ErrorOr<Result>> Handle(Command request, CancellationToken cancellationToken)
        {
            Param param = request.Param;

            try
            {
                Role? role = await roleManager.FindByIdAsync(request.Id.ToString());
                if (role == null)
                    return Role.Errors.RoleNotFound(Name);

                // Check: if trying to modify a default role inappropriately
                if (role.IsDefault)
                    return Role.Errors.CannotModifyDefaultRole(role.Name!);

                // Update: role properties
                role.Update(
                    name: param.Name,
                    displayName: param.Description,
                    priority: param.Priority,
                    description: param.Description,
                    isSystemRole: param.IsSystemRole);

                // Save: role
                IdentityResult result = await roleManager.UpdateAsync(role);
                if (!result.Succeeded)
                {
                    string errors = string.Join("; ", result.Errors.Select(e => e.Description));
                    logger.LogError("Failed to update role {RoleId}: {Errors}", request.Id, errors);
                    return Error.Failure("Role.UpdateFailed", $"Failed to update role: {errors}");
                }

                logger.LogInformation("Successfully updated role {RoleId}", role.Id);

                return new Result()
                {
                    Id = role.Id,
                    Name = role.Name!,
                    DisplayName = role.DisplayName,
                    Priority = role.Priority,
                    IsSystemRole = role.IsSystemRole,
                    Description = role.Description,
                    IsDefault = role.IsDefault,
                    CreatedAt = role.CreatedAt,
                    CreatedBy = role.CreatedBy,
                    UserCount = role.UserRoles?.Count ?? 0,
                    PermissionCount = role.RoleClaims?.Count ?? 0,

                };
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected error updating role {RoleId}", request.Id);
                return Role.Errors.UnexpectedError("updating");
            }
        }
    }
}