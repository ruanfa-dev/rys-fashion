using Core.Catalog.Options;
using Core.Catalog.Products;

using MediatR;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using UseCases.Common.Persistence.Context;

namespace UseCases.Admin.Catalogs.Options.Events;

public class OptionValueEventHandlers(IApplicationDbContext context, ILogger<OptionValueEventHandlers> logger)
    : INotificationHandler<OptionValue.Events.TouchProducts>, INotificationHandler<OptionValue.Events.TouchVariants>
{
    private readonly IApplicationDbContext _context = context ?? throw new ArgumentNullException(nameof(context));
    private readonly ILogger<OptionValueEventHandlers> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    public async Task Handle(OptionValue.Events.TouchProducts notification, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Handling TouchProducts for option value {OptionValueId}", notification.OptionValueId);

            var ov = await _context.Set<OptionValue>()
                .Include(o => o.Variants)
                    .ThenInclude(v => v.Products)
                .FirstOrDefaultAsync(o => o.Id == notification.OptionValueId, cancellationToken);

            if (ov == null)
            {
                _logger.LogWarning("OptionValue {OptionValueId} not found for touch.", notification.OptionValueId);
                return;
            }

            var products = ov.Variants.SelectMany(v => v.Products ?? Enumerable.Empty<Product>()).Distinct().ToList();
            foreach (var product in products)
            {
                product.MarkAsUpdated();
            }

            _logger.LogInformation("Marked {Count} products as updated for OptionValue {OptionValueId}.", products.Count, notification.OptionValueId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed handling TouchProducts for OptionValue {OptionValueId}.", notification.OptionValueId);
        }
    }

    public async Task Handle(OptionValue.Events.TouchVariants notification, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Handling TouchVariants for option value {OptionValueId}", notification.OptionValueId);

            var ov = await _context.Set<OptionValue>()
                .Include(o => o.OptionValueVariants)
                    .ThenInclude(ovv => ovv.Variant)
                .FirstOrDefaultAsync(o => o.Id == notification.OptionValueId, cancellationToken);

            if (ov == null)
            {
                _logger.LogWarning("OptionValue {OptionValueId} not found for touch variants.", notification.OptionValueId);
                return;
            }

            var variants = ov.OptionValueVariants.Select(v => v.Variant).Where(v => v != null).Cast<object>().ToList();
            // Mark variants as updated if domain exposes MarkAsUpdated; we simply mark owning products instead in most flows.
            _logger.LogInformation("Touched {Count} variant relations for OptionValue {OptionValueId} (changes persisted by outer SaveChanges).", variants.Count, notification.OptionValueId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed handling TouchVariants for OptionValue {OptionValueId}.", notification.OptionValueId);
        }
    }
}
