using Core.Identity;

using FluentValidation;

using UseCases.Admin.Roles.Common;
using UseCases.Common.Constants.Enums;

namespace UseCases.Admin.Users.Common;
/// <summary>
/// Parameter for multi-user role assignment operations
/// </summary>
public record BatchUserParam
{
    public required Guid[] UserIds { get; init; }
}

/// <summary>
/// Validator for multi-user role assignment parameters
/// </summary>
public sealed class BatchUserParamValidator : AbstractValidator<BatchUserParam>
{
    public BatchUserParamValidator()
    {
        RuleForEach(x => x.UserIds)
            .NotEmpty()
            .WithErrorCode(User.Errors.UserIdRequired.Code)
            .WithMessage(User.Errors.UserIdRequired.Description);
    }
}

