using System;
using System.Threading;
using System.Threading.Tasks;
using TestAttarClone.Application.Abstractions.Messaging;
using TestAttarClone.Application.DTOs.Cart;
using TestAttarClone.Application.Interfaces;
using TestAttarClone.SharedKernel.BaseTypes;

namespace TestAttarClone.Application.Commands.Cart;

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
                // Only set UpdaterId and Ip here, UpdateDate will be set by AuditInterceptor
                anonymousCart.UpdaterId = audit.UserId;
                anonymousCart.Ip = audit.IpAddress;

                await _cartRepository.UpdateAsync(anonymousCart, cancellationToken);
                return Result<CartDto>.Success(anonymousCart.ToDto());
            }

            if (userCart is not null && anonymousCart is not null)
            {
                userCart.MergeFrom(anonymousCart);
                userCart.AssignToUser(normalizedUserId);
                userCart.ClearDiscount();

                // Only set UpdaterId and Ip here, UpdateDate will be set by AuditInterceptor
                userCart.UpdaterId = audit.UserId;
                userCart.Ip = audit.IpAddress;

                // Note: MergeFrom and ClearDiscount call Touch() which updates UpdateDate
                // AuditInterceptor will set UpdateDate again, which may cause concurrency issues
                // We need to ensure the UpdateDate is properly handled in UpdateAsync

                await _cartRepository.UpdateAsync(userCart, cancellationToken);
                await _cartRepository.RemoveAsync(anonymousCart, cancellationToken);

                return Result<CartDto>.Success(userCart.ToDto());
            }

            userCart!.AssignToUser(normalizedUserId);
            // Only set UpdaterId and Ip here, UpdateDate will be set by AuditInterceptor
            userCart.UpdaterId = audit.UserId;
            userCart.Ip = audit.IpAddress;

            await _cartRepository.UpdateAsync(userCart, cancellationToken);
            return Result<CartDto>.Success(userCart.ToDto());
        }
    }
}
