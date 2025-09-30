using Core.Identity.Users;

using FluentValidation;

namespace UseCases.Admin.Identity.Users.Common;
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

