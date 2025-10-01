using Core.Catalog.Options;
using Core.Catalog.Products;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using SharedKernel.Domain.Attributes.Auditable;
using SharedKernel.Messaging.Abstracts;

using UseCases.Common.Persistence.Context;

namespace UseCases.Admin.Catalogs.Options.Events;

public class OptionTypeEventHandlers(IApplicationDbContext context, ILogger<OptionTypeEventHandlers> logger)
    : IDomainEventHandler<OptionType.Events.TouchProducts>
{
    private readonly IApplicationDbContext _context = context ?? throw new ArgumentNullException(nameof(context));
    private readonly ILogger<OptionTypeEventHandlers> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    public async Task Handle(OptionType.Events.TouchProducts notification, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Handling TouchProducts for option type {OptionTypeId}", notification.OptionTypeId);

            var optionType = await _context.Set<OptionType>()
                .Include(ot => ot.ProductOptionTypes)
                    .ThenInclude(pot => pot.Product)
                .FirstOrDefaultAsync(ot => ot.Id == notification.OptionTypeId, cancellationToken);

            if (optionType == null)
            {
                _logger.LogWarning("OptionType {OptionTypeId} not found for touch.", notification.OptionTypeId);
                return;
            }

            var products = optionType.ProductOptionTypes.Select(pot => pot.Product).Where(p => p != null).Cast<Product>().ToList();

            foreach (var product in products)
            {
                product.ApplyMarkAsUpdated();
                // Optionally raise a product updated domain event here if needed
            }

            _logger.LogInformation("Marked {Count} products as updated for OptionType {OptionTypeId}.", products.Count, notification.OptionTypeId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed handling TouchProducts for OptionType {OptionTypeId}.", notification.OptionTypeId);
        }
    }
}
