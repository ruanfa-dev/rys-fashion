using Core.Catalogs;

using ErrorOr;

using MapsterMapper;

using Microsoft.EntityFrameworkCore;

using SharedKernel.Messaging.Abstracts;

using UseCases.Admin.Catalogs.Properties.Commons;
using UseCases.Common.Persistence.Context;

namespace UseCases.Catalogs.Properties.Update;
public partial class UpdateProperty
{
    public record Param : PropertyParam;
    public record Result : PropertyResult;
    public sealed record Command(Guid Id, Param Param) : ICommand<Result>;
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
                var entity = await context.Set<Property>()
                    .FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);
                if (entity is null)
                    return Property.Errors.NotFound(request.Id);

                // Check for name uniqueness
                var nameExists = await context.Set<Property>()
                    .AnyAsync(p => p.Id != request.Id && p.Name == request.Param.Name, cancellationToken);
                if (nameExists)
                    return Property.Errors.NameAlreadyExists(request.Param.Name);

                // Update: property
                entity.Update(
                    name: request.Param.Name,
                    presentation: request.Param.Presentation,
                    kind: request.Param.Kind,
                    filterable: request.Param.Filterable,
                    displayOn: request.Param.DisplayOn,
                    position: request.Param.Position
                );

                // Add: domain event
                entity.AddDomainEvent(new Property.Events.Updated(entity.Id));

                // Save: changes
                context.Set<Property>().Update(entity);
                var result = mapper.Map<Result>(entity);
                return result;
            }
            catch (Exception ex)
            {
                return Property.Errors.PropertyUnexpected(Name, ex.Message);
            }
        }
    }
}
