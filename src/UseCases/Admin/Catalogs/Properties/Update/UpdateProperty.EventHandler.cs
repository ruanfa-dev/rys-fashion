using Core.Catalog.Products;
using Core.Catalog.Properties;

using Microsoft.EntityFrameworkCore;

using SharedKernel.Messaging.Abstracts;

using UseCases.Common.Persistence.Context;

namespace UseCases.Admin.Catalogs.Properties.Update;
public partial class UpdateProperty
{
    public class EventHandler : IDomainEventHandler<Property.Events.Updated>
    {
        public Task Handle(Property.Events.Updated domainEvent, CancellationToken cancellationToken)
        {
            // Implement any side effects or notifications here
            return Task.CompletedTask;
        }
    }

    public class FilterableChangedEventHandler(IUnitOfWork unitOfWork) : IDomainEventHandler<Property.Events.FilterableChanged>
    {
        public async Task Handle(Property.Events.FilterableChanged domainEvent, CancellationToken cancellationToken)
        {
            // Trigger: filterable in product properties update
            var dbContext = unitOfWork.Context;

            // Load: property with its product properties
            var property = dbContext.Set<Property>()
                .Where(p => p.Id == domainEvent.PropertyId)
                .Include(p => p.ProductProperties)
                .FirstOrDefault();

            // Update: ensure filter params in product properties if now filterable
            if (property != null)
            {
                var productProperties = property.EnsureProductPropertiesHaveFilterParams();
                if (productProperties?.Count > 0)
                {
                    dbContext.Set<ProductProperty>().UpdateRange(productProperties);
                    await dbContext.SaveChangesAsync(cancellationToken);
                }

            }
        }
    }
}
