using System;
using System.Threading;
using System.Threading.Tasks;
using Attar.Application.Abstractions.Messaging;
using Attar.Application.DTOs.Billing;
using Attar.Application.Interfaces;
using Attar.SharedKernel.BaseTypes;

namespace Attar.Application.Queries.Billing;

public sealed record FrontGetCartTransactionQuery(Guid CartId) : IQuery<FrontTransactionInfoDto>;

public sealed class FrontGetCartTransactionQueryHandler : IQueryHandler<FrontGetCartTransactionQuery, FrontTransactionInfoDto>
{
    private readonly IShoppingCartRepository _cartRepository;

    public FrontGetCartTransactionQueryHandler(IShoppingCartRepository cartRepository)
    {
        _cartRepository = cartRepository;
    }

    public async Task<Result<FrontTransactionInfoDto>> Handle(FrontGetCartTransactionQuery request, CancellationToken cancellationToken)
    {
        if (request.CartId == Guid.Empty)
        {
            return Result<FrontTransactionInfoDto>.Failure("شناسه سبد خرید نامعتبر است.");
        }

        var cart = await _cartRepository.GetByIdAsync(request.CartId, cancellationToken);
        if (cart is null)
        {
            return Result<FrontTransactionInfoDto>.Failure("سبد خرید پیدا نشد.");
        }

        var dto = new FrontTransactionInfoDto(
            cart.GrandTotal,
            string.Empty,
            cart.UserId ?? string.Empty,
            cart.Id);

        return Result<FrontTransactionInfoDto>.Success(dto);
    }
}
