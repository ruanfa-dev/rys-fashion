using Core.Identity;

using ErrorOr;

using FluentValidation;

using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

using SharedKernel.Messaging.Abstracts;

using UseCases.Accounts.Common;
using UseCases.Admin.Roles.Common;

namespace UseCases.Admin.Roles.Create;

public static partial class CreateRole
{
    public sealed record Param : RoleParam;
    public sealed class ParamValidator : AbstractValidator<Param>
    {
        public ParamValidator()
        {
            Include(new RoleParamValidator());
        }
    }

    public sealed record class Result(Guid Id);
    public sealed record Command(Param Param) : ICommand<Result>;
    public sealed class CommandValidator : AbstractValidator<Command>
    {
        public CommandValidator()
        {
            RuleFor(x => x.Param)
                .SetValidator(new ParamValidator());
        }
    }

    public sealed class Handler(
      RoleManager<Role> roleManager,
      ILogger<Handler> logger
  ) : ICommandHandler<Command, Result>
    {
        public async Task<ErrorOr<Result>> Handle(Command request, CancellationToken cancellationToken)
        {
            var param = request.Param;
            try
            {
                // Check: if role already exists
                var existingRole = await roleManager.FindByNameAsync(param.Name);
                if (existingRole != null)
                    return Role.Errors.RoleAlreadyExists(param.Name);

                // Create: new role
                var role = Role.Create(
                    name: param.Name,
                    description: param.Description,
                    priority: param.Priority,
                    isSystemRole: param.IsSystemRole);

                var result = await roleManager.CreateAsync(role);
                if (!result.Succeeded)
                {
                    var errors = string.Join("; ", result.Errors.Select(e => e.Description));
                    logger.LogError("Failed to create role {RoleName}: {Errors}", param.Name, errors);
                    return result.Errors.ToApplicationResult(
                        prefix: "Role",
                        fallbackCode: "CreationFailed");
                }

                logger.LogInformation("Successfully created role {RoleId} with name {RoleName}", role.Id, param.Name);

                return new Result(role.Id);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected error creating role with name {RoleName}", param.Name);
                return Role.Errors.UnexpectedError("creating");
            }
        }
    }
}