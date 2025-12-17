using System;
using System.Threading;
using System.Threading.Tasks;
using Attar.Application.Abstractions.Messaging;
using Attar.Application.Interfaces;
using Attar.SharedKernel.BaseTypes;

namespace Attar.Application.Commands.Catalog;

public sealed record DeleteProductOfferCommand(Guid OfferId) : ICommand<bool>
{
    public sealed class Handler : ICommandHandler<DeleteProductOfferCommand, bool>
    {
        private readonly IProductOfferRepository _offerRepository;
        private readonly IAuditContext _auditContext;

        public Handler(
            IProductOfferRepository offerRepository,
            IAuditContext auditContext)
        {
            _offerRepository = offerRepository;
            _auditContext = auditContext;
        }

        public async Task<Result<bool>> Handle(DeleteProductOfferCommand request, CancellationToken cancellationToken)
        {
            if (request.OfferId == Guid.Empty)
            {
                return Result<bool>.Failure("شناسه پیشنهاد معتبر نیست.");
            }

            var offer = await _offerRepository.GetByIdAsync(request.OfferId, cancellationToken);
            if (offer is null || offer.IsDeleted)
            {
                return Result<bool>.Failure("پیشنهاد مورد نظر یافت نشد.");
            }

            var audit = _auditContext.Capture();
            offer.IsDeleted = true;
            offer.RemoveDate = DateTimeOffset.UtcNow;
            offer.UpdaterId = audit.UserId;
            offer.UpdateDate = audit.Timestamp;
            offer.Ip = audit.IpAddress;

            await _offerRepository.UpdateAsync(offer, cancellationToken);

            return Result<bool>.Success(true);
        }
    }
}

