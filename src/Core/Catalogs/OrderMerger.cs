using System;
using System.Collections.Generic;
using System.Linq;

using Core.Catalogs;
using Core.Identity;

namespace Core.Cart;

/// <summary>
/// Port of Spree::OrderMerger (simplified).
/// - Delegates comparison logic to an injected compare service.
/// - Delegates persistence to an injected updater/repository (application/infra responsibilities).
/// </summary>
public sealed class OrderMerger
{
    private readonly Order _order;
    private readonly ICartCompareLineItemsService _compareService;
    private readonly IOrderUpdater _updater;
    private readonly IOrderRepository? _orderRepository;

    public OrderMerger(
        Order order,
        ICartCompareLineItemsService compareService,
        IOrderUpdater updater,
        IOrderRepository? orderRepository = null)
    {
        _order = order ?? throw new ArgumentNullException(nameof(order));
        _compareService = compareService ?? throw new ArgumentNullException(nameof(compareService));
        _updater = updater ?? throw new ArgumentNullException(nameof(updater));
        _orderRepository = orderRepository;
    }

    /// <summary>
    /// Merge another order into the current order.
    /// Note: this method performs in-memory re-assignment and relies on the caller/app layer to persist changes
    /// via the provided <see cref="IOrderUpdater"/> or <see cref="IOrderRepository"/>.
    /// </summary>
    public void Merge(Order otherOrder, User? user = null, bool discardMerged = true)
    {
        if (otherOrder == null) throw new ArgumentNullException(nameof(otherOrder));

        HandleGiftCard(otherOrder);

        // iterate over a copy because we may modify collections
        foreach (var otherLi in (otherOrder.LineItems ?? Enumerable.Empty<LineItem>()).ToList())
        {
            // only merge if currencies match
            if (!string.Equals(otherOrder.Currency, _order.Currency, StringComparison.OrdinalIgnoreCase))
                continue;

            var current = FindMatchingLineItem(otherLi);
            HandleMerge(current, otherLi);
        }

        SetUser(user);

        if (discardMerged)
            ClearAddresses(otherOrder);

        PersistMerge();

        if (discardMerged)
        {
            // prefer repository delete when available
            if (_orderRepository != null)
                _orderRepository.Delete(otherOrder);
            else
            {
                // best-effort: try domain-level destroy method if present
                try { otherOrder.Destroy(); } catch { /* infra should handle deletion */ }
            }
        }
    }

    private LineItem? FindMatchingLineItem(LineItem otherLineItem)
    {
        return (_order.LineItems ?? Enumerable.Empty<LineItem>())
            .FirstOrDefault(myLi =>
                myLi.Variant != null &&
                otherLineItem.Variant != null &&
                myLi.Variant.Id == otherLineItem.Variant.Id &&
                _compareService.Matches(_order, myLi, otherLineItem));
    }

    private void HandleMerge(LineItem? currentLineItem, LineItem otherOrderLineItem)
    {
        if (currentLineItem != null)
        {
            currentLineItem.SetQuantity(currentLineItem.Quantity + otherOrderLineItem.Quantity);
            currentLineItem.MarkAsUpdated();
        }
        else
        {
            // move the line item from other order to this order (in-memory)
            try
            {
                // reassign order id and navigation
                otherOrderLineItem.OrderId = _order.Id;
                otherOrderLineItem.Order = _order;

                // reassign any adjustments to the new order
                if (otherOrderLineItem.Adjustments != null)
                {
                    foreach (var adj in otherOrderLineItem.Adjustments)
                    {
                        adj.OrderId = _order.Id;
                        adj.MarkAsUpdated();
                    }
                }

                // add to current order
                _order.LineItems.Add(otherOrderLineItem);
                otherOrderLineItem.MarkAsUpdated();
            }
            catch
            {
                // best-effort: record errors on order if domain supports it
                try { _order.Errors.Add("merge", new[] { "Failed to move line item during merge." }); } catch { }
            }
        }
    }

    private void SetUser(User? user)
    {
        if (user == null) return;

        try
        {
            if (_order.User == null)
            {
                // prefer domain method if available
                _order.AssociateUser!(user);
            }
        }
        catch
        {
            // fallback to set id if property available
            try { _order.UserId = user.Id; } catch { }
        }
    }

    private void ClearAddresses(Order otherOrder)
    {
        try
        {
            otherOrder.ShipAddress = null;
            otherOrder.BillAddress = null;
        }
        catch
        {
            // ignore if properties don't exist or are read-only
        }
    }

    private void HandleGiftCard(Order otherOrder)
    {
        try
        {
            if (otherOrder.GiftCard != null)
            {
                var giftCard = otherOrder.GiftCard;
                otherOrder.RemoveGiftCard();
                _order.ApplyGiftCard(giftCard);
            }
        }
        catch
        {
            // ignore if giftcard APIs not present
        }
    }

    private void PersistMerge()
    {
        // delegate to updater (application/infra responsibility)
        _updater.Update(_order);
    }

    #region Service contracts (implement in application/infrastructure)

    public interface ICartCompareLineItemsService
    {
        /// <summary>
        /// Returns true when the two line items should be considered matching for merging purposes.
        /// </summary>
        bool Matches(Order order, LineItem existingLineItem, LineItem otherLineItem);
    }

    public interface IOrderUpdater
    {
        /// <summary>
        /// Persist updated order (save changes, recalculate totals, touch timestamps, etc).
        /// </summary>
        void Update(Order order);
    }

    public interface IOrderRepository
    {
        void Delete(Order order);
    }

    #endregion
}