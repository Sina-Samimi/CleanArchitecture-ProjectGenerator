using System;
using System.Threading;
using System.Threading.Tasks;
using LogsDtoCloneTest.Application.DTOs.Sms;
using LogsDtoCloneTest.Application.Interfaces;

namespace LogsDtoCloneTest.Application.Services;

public interface IBackInStockNotificationService
{
    Task NotifyProductBackInStockAsync(Guid productId, string productName, CancellationToken cancellationToken);

    Task NotifyOfferBackInStockAsync(Guid offerId, string productName, string sellerName, CancellationToken cancellationToken);
}

public sealed class BackInStockNotificationService : IBackInStockNotificationService
{
    private readonly IProductBackInStockSubscriptionRepository _subscriptionRepository;
    private readonly ISMSSenderService _smsSenderService;

    public BackInStockNotificationService(
        IProductBackInStockSubscriptionRepository subscriptionRepository,
        ISMSSenderService smsSenderService)
    {
        _subscriptionRepository = subscriptionRepository;
        _smsSenderService = smsSenderService;
    }

    public async Task NotifyProductBackInStockAsync(Guid productId, string productName, CancellationToken cancellationToken)
    {
        var subscriptions = await _subscriptionRepository.GetPendingNotificationsForProductAsync(productId, cancellationToken);
        if (subscriptions.Count == 0)
        {
            return;
        }

        foreach (var sub in subscriptions)
        {
            var dto = new BackInStockSmsDto(sub.PhoneNumber, productName, null);
            await _smsSenderService.BackInStockSms(dto);

            sub.MarkAsNotified();
            await _subscriptionRepository.UpdateAsync(sub, cancellationToken);
        }
    }

    public async Task NotifyOfferBackInStockAsync(Guid offerId, string productName, string sellerName, CancellationToken cancellationToken)
    {
        var subscriptions = await _subscriptionRepository.GetPendingNotificationsForOfferAsync(offerId, cancellationToken);
        if (subscriptions.Count == 0)
        {
            return;
        }

        foreach (var sub in subscriptions)
        {
            var dto = new BackInStockSmsDto(sub.PhoneNumber, productName, sellerName);
            await _smsSenderService.BackInStockSms(dto);

            sub.MarkAsNotified();
            await _subscriptionRepository.UpdateAsync(sub, cancellationToken);
        }
    }
}


