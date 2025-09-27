using Core.Identity;
using Core.Identity.Permissions;

using FluentValidation;

using UseCases.Common.Security.Authorization.Permissions;

namespace UseCases.Admin.Identity.Permissions.Common;

/// <summary>
/// Validator for permission name
/// </summary>  
public sealed class PermissionNameValidator : AbstractValidator<string>
{
    public PermissionNameValidator()
    {
        // Required
        RuleFor(name => name)
            .NotEmpty()
            .WithErrorCode(Permission.Errors.PermissionRequired.Code)
            .WithMessage(Permission.Errors.PermissionRequired.Description);

        // Structural/format validation (segmentation, lengths, allowed characters)
        RuleFor(name => name)
            .Must(Permission.IsValidPermissionName)
            .When(name => !string.IsNullOrWhiteSpace(name))
            .WithErrorCode(Permission.Errors.InvalidFormat.Code)
            .WithMessage(Permission.Errors.InvalidFormat.Description);

        // Known permission check (exists in Feature.Permissions)
        RuleFor(name => name)
            .Must(BeKnownPermissionFormat)
            .When(name => !string.IsNullOrWhiteSpace(name) && Permission.IsValidPermissionName(name))
            .WithErrorCode(Permission.Errors.NotFound().Code)
            .WithMessage(m => Permission.Errors.NotFound(m).Description);
    }

    private static bool BeKnownPermissionFormat(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return false;

        var normalized = name.Trim().ToLowerInvariant();

        // Feature.Permissions may be null or empty; defend against that.
        var permissions = Feature.Permissions;
        if (permissions == null)
            return false;

        return permissions.Any(permission => string.Equals(permission?.Name, normalized, StringComparison.Ordinal));
    }
}
