using ErrorOr;

using FluentValidation;
using FluentValidation.Results;

using MediatR;

namespace UseCases.Common.Behaviors;

public sealed class ValidationBehavior<TRequest, TResponse>(IValidator<TRequest>? validator = null)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
    where TResponse : IErrorOr
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (validator is null)
        {
            return await next(cancellationToken);
        }

        ValidationResult? validationResult = await validator.ValidateAsync(request, cancellationToken);

        if (validationResult.IsValid)
        {
            return await next(cancellationToken);
        }

        List<Error> errors = validationResult.Errors
            .ConvertAll(error => Error.Validation(error.ErrorCode, error.ErrorMessage));

        return (dynamic)errors;
    }
}