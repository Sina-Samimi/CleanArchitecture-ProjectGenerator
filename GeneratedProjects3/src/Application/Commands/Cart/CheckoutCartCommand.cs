using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LogTableRenameTest.Application.Abstractions.Messaging;
using LogTableRenameTest.Application.Commands.Billing;
using LogTableRenameTest.Application.Commands.Notifications;
using LogTableRenameTest.Application.DTOs.Notifications;
using LogTableRenameTest.Application.Interfaces;
using LogTableRenameTest.Application.Queries.Admin.FinancialSettings;
using LogTableRenameTest.Domain.Entities.Notifications;
using LogTableRenameTest.Domain.Enums;
using LogTableRenameTest.Domain.Exceptions;
using LogTableRenameTest.SharedKernel.BaseTypes;
using MediatR;

namespace LogTableRenameTest.Application.Commands.Cart;

public sealed record CheckoutCartCommand(
    string UserId,
    Guid? AnonymousId,
    Guid? ShippingAddressId = null) : ICommand<Guid>
{
    public sealed class Handler : ICommandHandler<CheckoutCartCommand, Guid>
    {
        private readonly IShoppingCartRepository _cartRepository;
        private readonly IDiscountCodeRepository _discountRepository;
        private readonly IProductRepository _productRepository;
        private readonly IInvoiceRepository _invoiceRepository;
        private readonly INotificationRepository _notificationRepository;
        private readonly IUserAddressRepository _addressRepository;
        private readonly IMediator _mediator;
        private readonly IAuditContext _auditContext;

        public Handler(
            IShoppingCartRepository cartRepository,
            IDiscountCodeRepository discountRepository,
            IProductRepository productRepository,
            IInvoiceRepository invoiceRepository,
            INotificationRepository notificationRepository,
            IUserAddressRepository addressRepository,
            IMediator mediator,
            IAuditContext auditContext)
        {
            _cartRepository = cartRepository;
            _discountRepository = discountRepository;
            _productRepository = productRepository;
            _invoiceRepository = invoiceRepository;
            _notificationRepository = notificationRepository;
            _addressRepository = addressRepository;
            _mediator = mediator;
            _auditContext = auditContext;
        }

        public async Task<Result<Guid>> Handle(CheckoutCartCommand request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.UserId))
            {
                return Result<Guid>.Failure("شناسه کاربر معتبر نیست.");
            }

            var cart = await ResolveCartAsync(request.UserId, request.AnonymousId, cancellationToken);
            if (cart is null || cart.IsEmpty)
            {
                return Result<Guid>.Failure("سبد خرید خالی است.");
            }

            // Apply discount code if exists and register usage
            decimal discountAmount = 0;
            if (cart.HasDiscount && !string.IsNullOrWhiteSpace(cart.AppliedDiscountCode))
            {
                var discount = await _discountRepository.GetByCodeAsync(cart.AppliedDiscountCode, cancellationToken);
                if (discount is not null)
                {
                    try
                    {
                        var audit = _auditContext.Capture();
                        var audienceKey = !string.IsNullOrWhiteSpace(request.UserId) ? request.UserId : null;
                        
                        // Redeem the discount code (this will register usage)
                        var discountResult = discount.Redeem(cart.Subtotal, audit.Timestamp, audienceKey);
                        discountAmount = discountResult.DiscountAmount;
                        
                        // Update discount code to persist usage
                        discount.UpdaterId = audit.UserId;
                        discount.Ip = audit.IpAddress;
                        await _discountRepository.UpdateAsync(discount, cancellationToken);
                    }
                    catch (DomainException ex)
                    {
                        // Discount code is no longer valid, clear it from cart
                        cart.ClearDiscount();
                        await _cartRepository.UpdateAsync(cart, cancellationToken);
                        return Result<Guid>.Failure(ex.Message);
                    }
                }
            }

            // Create invoice from cart
            var invoiceItems = cart.Items.Select(item => new CreateInvoiceCommand.Item(
                Name: item.ProductName,
                Description: null,
                ItemType: item.ProductType == ProductType.Digital ? InvoiceItemType.Course : InvoiceItemType.Product,
                ReferenceId: item.ProductId,
                Quantity: item.Quantity,
                UnitPrice: item.UnitPrice,
                DiscountAmount: null, // Individual item discounts are not tracked in cart
                Attributes: null,
                VariantId: item.VariantId
            )).ToList();

            // Calculate tax amount from financial settings
            decimal taxAmount = 0;
            var financialSettingsResult = await _mediator.Send(new GetFinancialSettingsQuery(), cancellationToken);
            if (financialSettingsResult.IsSuccess && financialSettingsResult.Value is not null)
            {
                var vatPercentage = financialSettingsResult.Value.ValueAddedTaxPercentage;
                if (vatPercentage > 0)
                {
                    // Calculate items total (subtotal - discount)
                    var itemsSubtotal = cart.Subtotal;
                    var itemsTotal = itemsSubtotal - discountAmount;
                    if (itemsTotal > 0)
                    {
                        taxAmount = decimal.Round(
                            itemsTotal * vatPercentage / 100m,
                            2,
                            MidpointRounding.AwayFromZero);
                    }
                }
            }

            // Get shipping address if provided
            Guid? shippingAddressId = null;
            string? shippingRecipientName = null;
            string? shippingRecipientPhone = null;
            string? shippingProvince = null;
            string? shippingCity = null;
            string? shippingPostalCode = null;
            string? shippingAddressLine = null;
            string? shippingPlaque = null;
            string? shippingUnit = null;

            if (request.ShippingAddressId.HasValue)
            {
                var address = await _addressRepository.GetByIdForUserAsync(request.ShippingAddressId.Value, request.UserId, cancellationToken);
                if (address is not null)
                {
                    shippingAddressId = address.Id;
                    shippingRecipientName = address.RecipientName;
                    shippingRecipientPhone = address.RecipientPhone;
                    shippingProvince = address.Province;
                    shippingCity = address.City;
                    shippingPostalCode = address.PostalCode;
                    shippingAddressLine = address.AddressLine;
                    shippingPlaque = address.Plaque;
                    shippingUnit = address.Unit;
                }
            }

            var createInvoiceCommand = new CreateInvoiceCommand(
                InvoiceNumber: null, // Will be auto-generated
                Title: "فاکتور خرید محصولات",
                Description: $"سفارش شامل {cart.Items.Count} محصول",
                Currency: "IRT",
                UserId: request.UserId,
                IssueDate: DateTimeOffset.Now,
                DueDate: DateTimeOffset.Now.AddDays(7),
                TaxAmount: taxAmount,
                AdjustmentAmount: -discountAmount, // Negative adjustment for discount
                ExternalReference: cart.Id.ToString(),
                Items: invoiceItems,
                ShippingAddressId: shippingAddressId,
                ShippingRecipientName: shippingRecipientName,
                ShippingRecipientPhone: shippingRecipientPhone,
                ShippingProvince: shippingProvince,
                ShippingCity: shippingCity,
                ShippingPostalCode: shippingPostalCode,
                ShippingAddressLine: shippingAddressLine,
                ShippingPlaque: shippingPlaque,
                ShippingUnit: shippingUnit
            );

            var invoiceResult = await _mediator.Send(createInvoiceCommand, cancellationToken);
            if (!invoiceResult.IsSuccess)
            {
                return Result<Guid>.Failure(invoiceResult.Error ?? "خطا در ایجاد فاکتور.");
            }

            var invoiceId = invoiceResult.Value;

            // Notify sellers about new orders
            await NotifySellersAsync(cart, invoiceId, cancellationToken);

            // Clear the cart
            await _cartRepository.RemoveAsync(cart, cancellationToken);

            return Result<Guid>.Success(invoiceId);
        }

        private async Task<Domain.Entities.Orders.ShoppingCart?> ResolveCartAsync(
            string? userId,
            Guid? anonymousId,
            CancellationToken cancellationToken)
        {
            var normalizedUserId = string.IsNullOrWhiteSpace(userId) ? null : userId.Trim();
            if (!string.IsNullOrWhiteSpace(normalizedUserId))
            {
                var cart = await _cartRepository.GetByUserIdAsync(normalizedUserId, cancellationToken);
                if (cart is not null)
                {
                    return cart;
                }
            }

            if (anonymousId is null || anonymousId.Value == Guid.Empty)
            {
                return null;
            }

            return await _cartRepository.GetByAnonymousIdAsync(anonymousId.Value, cancellationToken);
        }

        private async Task NotifySellersAsync(
            Domain.Entities.Orders.ShoppingCart cart,
            Guid invoiceId,
            CancellationToken cancellationToken)
        {
            try
            {
                // Get invoice to retrieve invoice number
                var invoice = await _invoiceRepository.GetByIdAsync(invoiceId, cancellationToken);
                var invoiceNumber = invoice?.InvoiceNumber ?? "نامشخص";

                // Get unique product IDs from cart
                var productIds = cart.Items
                    .Where(item => item.ProductId != Guid.Empty)
                    .Select(item => item.ProductId)
                    .Distinct()
                    .ToList();

                if (productIds.Count == 0)
                {
                    return;
                }

                // Get products to find seller IDs
                var products = await _productRepository.GetByIdsAsync(productIds, cancellationToken);
                
                // Group products by seller ID
                var sellerProducts = products
                    .Where(p => !string.IsNullOrWhiteSpace(p.SellerId))
                    .GroupBy(p => p.SellerId!)
                    .ToList();

                if (sellerProducts.Count == 0)
                {
                    return;
                }

                var audit = _auditContext.Capture();

                // Create notification for each seller
                foreach (var sellerGroup in sellerProducts)
                {
                    var sellerId = sellerGroup.Key;
                    var sellerProductNames = sellerGroup.Select(p => p.Name).ToList();
                    var productCount = sellerGroup.Count();

                    var title = "سفارش جدید";
                    var message = $"شما {productCount} سفارش جدید برای محصول{(productCount > 1 ? "ات" : "")} خود در فاکتور {invoiceNumber} دریافت کرده‌اید: {string.Join("، ", sellerProductNames.Take(3))}{(sellerProductNames.Count > 3 ? " و..." : "")}";

                    // Create notification
                    var notification = new Notification(
                        title,
                        message,
                        NotificationType.System,
                        NotificationPriority.Normal,
                        null);

                    notification.CreatorId = audit.UserId;
                    notification.CreateDate = audit.Timestamp;
                    notification.UpdateDate = audit.Timestamp;
                    notification.Ip = audit.IpAddress;
                    notification.SetCreatedBy(audit.UserId);

                    await _notificationRepository.AddAsync(notification, cancellationToken);

                    // Create UserNotification for seller
                    var userNotification = new UserNotification(notification.Id, sellerId);
                    userNotification.CreatorId = audit.UserId;
                    userNotification.CreateDate = audit.Timestamp;
                    userNotification.UpdateDate = audit.Timestamp;
                    userNotification.Ip = audit.IpAddress;

                    await _notificationRepository.AddUserNotificationAsync(userNotification, cancellationToken);
                }
            }
            catch (Exception)
            {
                // Silently fail notification creation - don't break checkout process
                // Log error if needed
            }
        }
    }
}
