using System;
using System.Threading;
using System.Threading.Tasks;
using Arsis.Application.Abstractions.Messaging;
using Arsis.Application.DTOs.Cart;
using Arsis.Application.Interfaces;
using Arsis.SharedKernel.BaseTypes;

namespace Arsis.Application.Commands.Cart;

public sealed record MergeCartCommand(
    string UserId,
    Guid? AnonymousId) : ICommand<CartDto>
{
    public sealed class Handler : ICommandHandler<MergeCartCommand, CartDto>
    {
        private readonly IShoppingCartRepository _cartRepository;
        private readonly IAuditContext _auditContext;

        public Handler(IShoppingCartRepository cartRepository, IAuditContext auditContext)
        {
            _cartRepository = cartRepository;
            _auditContext = auditContext;
        }

        public async Task<Result<CartDto>> Handle(MergeCartCommand request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.UserId))
            {
                return Result<CartDto>.Failure("شناسه کاربر نامعتبر است.");
            }

            var normalizedUserId = request.UserId.Trim();

            var userCart = await _cartRepository.GetByUserIdAsync(normalizedUserId, cancellationToken);
            var anonymousCart = request.AnonymousId is null || request.AnonymousId.Value == Guid.Empty
                ? null
                : await _cartRepository.GetByAnonymousIdAsync(request.AnonymousId.Value, cancellationToken);

            if (userCart is null && anonymousCart is null)
            {
                return Result<CartDto>.Success(CartDtoMapper.CreateEmpty(null, normalizedUserId));
            }

            var audit = _auditContext.Capture();

            if (userCart is null && anonymousCart is not null)
            {
                anonymousCart.AssignToUser(normalizedUserId);
                anonymousCart.ClearDiscount();
                anonymousCart.UpdaterId = audit.UserId;
                anonymousCart.UpdateDate = audit.Timestamp;
                anonymousCart.Ip = audit.IpAddress;

                await _cartRepository.UpdateAsync(anonymousCart, cancellationToken);
                return Result<CartDto>.Success(anonymousCart.ToDto());
            }

            if (userCart is not null && anonymousCart is not null)
            {
                userCart.MergeFrom(anonymousCart);
                userCart.AssignToUser(normalizedUserId);
                userCart.ClearDiscount();

                userCart.UpdaterId = audit.UserId;
                userCart.UpdateDate = audit.Timestamp;
                userCart.Ip = audit.IpAddress;

                await _cartRepository.UpdateAsync(userCart, cancellationToken);
                await _cartRepository.RemoveAsync(anonymousCart, cancellationToken);

                return Result<CartDto>.Success(userCart.ToDto());
            }

            userCart!.AssignToUser(normalizedUserId);
            userCart.UpdaterId = audit.UserId;
            userCart.UpdateDate = audit.Timestamp;
            userCart.Ip = audit.IpAddress;

            await _cartRepository.UpdateAsync(userCart, cancellationToken);
            return Result<CartDto>.Success(userCart.ToDto());
        }
    }
}
