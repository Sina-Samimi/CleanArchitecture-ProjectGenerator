using System;
using System.Threading;
using System.Threading.Tasks;
using LogTableRenameTest.Application.Abstractions.Messaging;
using LogTableRenameTest.Application.Interfaces;
using LogTableRenameTest.Application.Services;
using LogTableRenameTest.SharedKernel.BaseTypes;

namespace LogTableRenameTest.Application.Commands.Catalog;

public sealed record UpdateProductOfferCommand(
    Guid OfferId,
    decimal? Price,
    decimal? CompareAtPrice,
    bool TrackInventory,
    int StockQuantity,
    bool IsActive,
    bool IsPublished,
    DateTimeOffset? PublishedAt = null) : ICommand<bool>
{
    public sealed class Handler : ICommandHandler<UpdateProductOfferCommand, bool>
    {
        private readonly IProductOfferRepository _offerRepository;
        private readonly IAuditContext _auditContext;
        private readonly IBackInStockNotificationService _backInStockNotificationService;

        public Handler(
            IProductOfferRepository offerRepository,
            IAuditContext auditContext,
            IBackInStockNotificationService backInStockNotificationService)
        {
            _offerRepository = offerRepository;
            _auditContext = auditContext;
            _backInStockNotificationService = backInStockNotificationService;
        }

        public async Task<Result<bool>> Handle(UpdateProductOfferCommand request, CancellationToken cancellationToken)
        {
            if (request.OfferId == Guid.Empty)
            {
                return Result<bool>.Failure("شناسه پیشنهاد معتبر نیست.");
            }

            if (request.Price.HasValue && request.Price.Value < 0)
            {
                return Result<bool>.Failure("قیمت نمی‌تواند منفی باشد.");
            }

            if (request.CompareAtPrice.HasValue && request.CompareAtPrice.Value < 0)
            {
                return Result<bool>.Failure("قیمت قبل از تخفیف نمی‌تواند منفی باشد.");
            }

            if (request.TrackInventory && request.StockQuantity < 0)
            {
                return Result<bool>.Failure("موجودی نمی‌تواند منفی باشد.");
            }

            var offer = await _offerRepository.GetByIdAsync(request.OfferId, cancellationToken);
            if (offer is null || offer.IsDeleted)
            {
                return Result<bool>.Failure("پیشنهاد مورد نظر یافت نشد.");
            }

            var previousTrackInventory = offer.TrackInventory;
            var previousStockQuantity = offer.StockQuantity;

            offer.UpdatePricing(request.Price, request.CompareAtPrice);
            offer.UpdateInventory(request.TrackInventory, request.StockQuantity);
            offer.SetActive(request.IsActive);

            if (request.IsPublished)
            {
                offer.Publish(request.PublishedAt);
            }
            else
            {
                offer.Unpublish();
            }

            var audit = _auditContext.Capture();
            offer.UpdaterId = audit.UserId;
            offer.UpdateDate = audit.Timestamp;
            offer.Ip = audit.IpAddress;

            await _offerRepository.UpdateAsync(offer, cancellationToken);

            // اگر موجودی از ناموجود به موجود تغییر کرد، نوتیفیکیشن‌های خبرم کن را ارسال کن
            if (request.TrackInventory &&
                (!previousTrackInventory || previousStockQuantity <= 0) &&
                request.StockQuantity > 0)
            {
                await _backInStockNotificationService.NotifyOfferBackInStockAsync(
                    offer.Id,
                    offer.Product.Name,
                    offer.SellerId ?? string.Empty,
                    cancellationToken);
            }

            return Result<bool>.Success(true);
        }
    }
}

