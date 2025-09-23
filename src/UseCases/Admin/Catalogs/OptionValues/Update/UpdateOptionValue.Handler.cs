using Core.Catalogs;

using ErrorOr;

using FluentValidation;

using MapsterMapper;

using Microsoft.EntityFrameworkCore;

using SharedKernel.Messaging.Abstracts;

using UseCases.Admin.Catalogs.OptionValues.Commons;
using UseCases.Common.Persistence.Context;

namespace UseCases.Admin.Catalogs.OptionValues.Update;
public partial class UpdateOptionValue
{
    public record Param : OptionValueParam;
    public record Result : OptionValueResult;
    public sealed record Command(Guid Id, Param Param) : ICommand<Result>;
    public sealed class CommandValidator : AbstractValidator<Command>
    {
        public CommandValidator()
        {
            RuleFor(x => x.Id)
                .NotEmpty()
                .WithErrorCode(OptionValue.Errors.IdRequired.Code)
                .WithMessage(OptionValue.Errors.IdRequired.Description);

            RuleFor(x => x.Param)
                .SetValidator(new OptionValueParamValidator());
        }
    }
    public sealed class Handler(
        IUnitOfWork unitOfWork,
        IMapper mapper
    ) : ICommandHandler<Command, Result>
    {
        public async Task<ErrorOr<Result>> Handle(Command request, CancellationToken cancellationToken)
        {
            try
            {
                var context = unitOfWork.Context;
                var entity = await context.Set<OptionValue>()
                    .FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);
                if (entity is null)
                    return OptionValue.Errors.NotFound(request.Id);

                // Check for name uniqueness
                var nameExists = await context.Set<OptionValue>()
                    .AnyAsync(p => p.Id != request.Id && p.Name == request.Param.Name, cancellationToken);
                if (nameExists)
                    return OptionValue.Errors.DuplicateName(request.Param.Name);

                // Update: property
                entity.Update(
                    name: request.Param.Name,
                    presentation: request.Param.Presentation,
                    filterable: request.Param.Filterable,
                    position: request.Param.Position
                );

                // Add: domain event
                entity.AddDomainEvent(new OptionValue.Events.Updated(entity.Id));

                // Save: changes
                context.Set<OptionValue>().Update(entity);
                var result = mapper.Map<Result>(entity);
                return result;
            }
            catch (Exception ex)
            {
                return OptionValue.Errors.OptionValueUnexpected(Name, ex.Message);
            }
        }
    }
}
