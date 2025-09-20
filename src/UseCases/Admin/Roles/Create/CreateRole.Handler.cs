using ErrorOr;

using FluentValidation;

using Microsoft.Extensions.Logging;

using SharedKernel.Messaging.Abstracts;

using UseCases.Admin.Roles.Common;
using UseCases.Common.Security.Authentication.Services;
using UseCases.Common.Security.Authentication.Models;

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

    public sealed record Result(Guid Id);
    
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
        IKeycloakAdminService keycloakAdminService,
        ILogger<Handler> logger
    ) : ICommandHandler<Command, Result>
    {
        public async Task<ErrorOr<Result>> Handle(Command request, CancellationToken cancellationToken)
        {
            var param = request.Param;
            try
            {
                // Check if role already exists in Keycloak
                var existingRole = await keycloakAdminService.GetRoleByNameAsync(param.Name, cancellationToken);
                if (existingRole != null)
                {
                    logger.LogWarning("Role {RoleName} already exists in Keycloak", param.Name);
                    return Error.Conflict("Role.AlreadyExists", $"Role '{param.Name}' already exists");
                }

                // Create role in Keycloak
                var createRoleRequest = new CreateKeycloakRoleRequest
                {
                    Name = param.Name,
                    Description = param.Description,
                    Composite = false, // Simple roles by default
                    Attributes = new Dictionary<string, object[]>
                    {
                        ["priority"] = [param.Priority.ToString()],
                        ["isSystemRole"] = [param.IsSystemRole.ToString().ToLower()],
                        ["isDefault"] = ["false"] // New roles are not default
                    }
                };

                await keycloakAdminService.CreateRoleAsync(createRoleRequest, cancellationToken);

                // Retrieve the created role to get its ID
                var createdRole = await keycloakAdminService.GetRoleByNameAsync(param.Name, cancellationToken);
                if (createdRole == null)
                {
                    logger.LogError("Failed to retrieve created role {RoleName} from Keycloak", param.Name);
                    return Error.Failure("Role.CreationFailed", "Role was created but could not be retrieved");
                }

                var roleId = Guid.Parse(createdRole.Id);

                logger.LogInformation("Successfully created role {RoleId} with name {RoleName} in Keycloak", 
                    roleId, param.Name);

                return new Result(roleId);
            }
            catch (NotSupportedException ex)
            {
                logger.LogWarning("Keycloak Admin API not configured: {Message}", ex.Message);
                return Error.Failure("Role.NotConfigured", "Keycloak role management is not properly configured");
            }
            catch (HttpRequestException ex)
            {
                logger.LogError(ex, "HTTP error creating role {RoleName} in Keycloak", param.Name);
                return Error.Failure("Role.CreationFailed", "Failed to create role in Keycloak due to network error");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected error creating role with name {RoleName}", param.Name);
                return Error.Failure("Role.UnexpectedError", "An unexpected error occurred while creating the role");
            }
        }
    }
}