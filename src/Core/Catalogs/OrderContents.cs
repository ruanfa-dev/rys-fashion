using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Core.Catalogs;

using ErrorOr;

using SharedKernel.Domain.Primitives;

namespace Core.Cart;

/// <summary>
/// Lightweight port of Spree::OrderContents — a small facade that delegates
/// cart operations to application services. The concrete services should be
/// provided by the application/infrastructure layer (via DI).
/// </summary>
public sealed class OrderContents
{
    public Order Order { get; }

    private readonly ICartAddItemService _addService;
    private readonly ICartRemoveItemService _removeService;
    private readonly ICartUpdateService _updateService;
    private readonly ICartRemoveLineItemService _removeLineItemService;

    public OrderContents(
        Order order,
        ICartAddItemService addService,
        ICartRemoveItemService removeService,
        ICartRemoveLineItemService removeLineItemService,
        ICartUpdateService updateService)
    {
        Order = order ?? throw new ArgumentNullException(nameof(order));
        _addService = addService ?? throw new ArgumentNullException(nameof(addService));
        _removeService = removeService ?? throw new ArgumentNullException(nameof(removeService));
        _removeLineItemService = removeLineItemService ?? throw new ArgumentNullException(nameof(removeLineItemService));
        _updateService = updateService ?? throw new ArgumentNullException(nameof(updateService));
    }

    // Mirrors Spree::OrderContents#add
    public Task<ErrorOr<LineItem>> AddAsync(Variant variant, int quantity = 1, IDictionary<string, object>? options = null)
        => _addService.CallAsync(Order, variant, quantity, options ?? new Dictionary<string, object>());

    // Mirrors Spree::OrderContents#remove
    public Task<ErrorOr<LineItem>> RemoveAsync(Variant variant, int quantity = 1, IDictionary<string, object>? options = null)
        => _removeService.CallAsync(Order, variant, quantity, options ?? new Dictionary<string, object>());

    // Mirrors Spree::OrderContents#remove_line_item
    public Task<ErrorOr<bool>> RemoveLineItemAsync(LineItem lineItem, IDictionary<string, object>? options = null)
        => _removeLineItemService.CallAsync(Order, lineItem, options ?? new Dictionary<string, object>());

    // Mirrors Spree::OrderContents#update_cart
    public Task<ErrorOr<Order>> UpdateCartAsync(IDictionary<string, object> @params)
        => _updateService.CallAsync(Order, @params);

    #region Service contracts (implement in application/infrastructure)

    public interface ICartAddItemService
    {
        Task<ErrorOr<LineItem>> CallAsync(Order order, Variant variant, int quantity, IDictionary<string, object> options);
    }

    public interface ICartRemoveItemService
    {
        Task<ErrorOr<LineItem>> CallAsync(Order order, Variant variant, int quantity, IDictionary<string, object> options);
    }

    public interface ICartRemoveLineItemService
    {
        Task<ErrorOr<bool>> CallAsync(Order order, LineItem lineItem, IDictionary<string, object> options);
    }

    public interface ICartUpdateService
    {
        Task<ErrorOr<Order>> CallAsync(Order order, IDictionary<string, object> @params);
    }

    #endregion
}