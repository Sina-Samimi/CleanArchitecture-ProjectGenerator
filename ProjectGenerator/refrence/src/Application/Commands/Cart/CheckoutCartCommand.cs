using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Arsis.Application.Abstractions.Messaging;
using Arsis.Application.Commands.Billing;
using Arsis.Application.Interfaces;
using Arsis.Domain.Enums;
using Arsis.SharedKernel.BaseTypes;
using MediatR;

namespace Arsis.Application.Commands.Cart;

public sealed record CheckoutCartCommand(
    string UserId,
    Guid? AnonymousId) : ICommand<Guid>
{
    public sealed class Handler : ICommandHandler<CheckoutCartCommand, Guid>
    {
        private readonly IShoppingCartRepository _cartRepository;
        private readonly IMediator _mediator;
        private readonly IAuditContext _auditContext;

        public Handler(
            IShoppingCartRepository cartRepository,
            IMediator mediator,
            IAuditContext auditContext)
        {
            _cartRepository = cartRepository;
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

            // Create invoice from cart
            var invoiceItems = cart.Items.Select(item => new CreateInvoiceCommand.Item(
                Name: item.ProductName,
                Description: null,
                ItemType: item.ProductType == ProductType.Digital ? InvoiceItemType.Course : InvoiceItemType.Product,
                ReferenceId: item.ProductId,
                Quantity: item.Quantity,
                UnitPrice: item.UnitPrice,
                DiscountAmount: null, // Individual item discounts are not tracked in cart
                Attributes: null
            )).ToList();

            var createInvoiceCommand = new CreateInvoiceCommand(
                InvoiceNumber: null, // Will be auto-generated
                Title: "فاکتور خرید محصولات",
                Description: $"سفارش شامل {cart.Items.Count} محصول",
                Currency: "IRT",
                UserId: request.UserId,
                IssueDate: DateTimeOffset.UtcNow,
                DueDate: DateTimeOffset.UtcNow.AddDays(7),
                TaxAmount: 0,
                AdjustmentAmount: 0,
                ExternalReference: cart.Id.ToString(),
                Items: invoiceItems
            );

            var invoiceResult = await _mediator.Send(createInvoiceCommand, cancellationToken);
            if (!invoiceResult.IsSuccess)
            {
                return Result<Guid>.Failure(invoiceResult.Error ?? "خطا در ایجاد فاکتور.");
            }

            // Clear the cart
            await _cartRepository.RemoveAsync(cart, cancellationToken);

            return Result<Guid>.Success(invoiceResult.Value);
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
    }
}
