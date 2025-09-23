using Core.Catalogs;

using ErrorOr;

using FluentValidation;

using MapsterMapper;

using Microsoft.EntityFrameworkCore;

using SharedKernel.Messaging.Abstracts;

using UseCases.Admin.Catalogs.OptionTypes.Commons;
using UseCases.Common.Persistence.Context;

namespace UseCases.Admin.Catalogs.OptionTypes.Update;
public partial class UpdateOptionType
{
    public record Param : OptionTypeParam;
    public record Result : OptionTypeResult;
    public sealed record Command(Guid Id, Param Param) : ICommand<Result>;
    public sealed class CommandValidator : AbstractValidator<Command>
    {
        public CommandValidator()
        {
            RuleFor(x => x.Id)
                .NotEmpty()
                .WithErrorCode(OptionType.Errors.IdRequired.Code)
                .WithMessage(OptionType.Errors.IdRequired.Description);

            RuleFor(x => x.Param)
                .SetValidator(new OptionTypeParamValidator());
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
                var entity = await context.Set<OptionType>()
                    .FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);
                if (entity is null)
                    return OptionType.Errors.NotFound(request.Id);

                // Check for name uniqueness
                var nameExists = await context.Set<OptionType>()
                    .AnyAsync(p => p.Id != request.Id && p.Name == request.Param.Name, cancellationToken);
                if (nameExists)
                    return OptionType.Errors.DuplicateName(request.Param.Name);

                // Update: property
                entity.Update(
                    name: request.Param.Name,
                    presentation: request.Param.Presentation,
                    filterable: request.Param.Filterable,
                    position: request.Param.Position
                );

                // Add: domain event
                entity.AddDomainEvent(new OptionType.Events.Updated(entity.Id));

                // Save: changes
                context.Set<OptionType>().Update(entity);
                var result = mapper.Map<Result>(entity);
                return result;
            }
            catch (Exception ex)
            {
                return OptionType.Errors.OptionTypeUnexpected(Name, ex.Message);
            }
        }
    }
}
