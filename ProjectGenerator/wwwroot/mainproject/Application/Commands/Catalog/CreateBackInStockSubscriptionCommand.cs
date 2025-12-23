using System;
using System.Threading;
using System.Threading.Tasks;
using MobiRooz.Application.DTOs.Sms;
using MobiRooz.Application.Interfaces;
using MobiRooz.SharedKernel.DTOs;
using MediatR;

namespace MobiRooz.Application.Commands.Catalog;

public sealed record CreateBackInStockSubscriptionCommand(
    Guid? ProductId,
    Guid? ProductOfferId,
    string PhoneNumber,
    string? UserId) : IRequest<ResponseDto>;

public sealed class CreateBackInStockSubscriptionCommandHandler
    : IRequestHandler<CreateBackInStockSubscriptionCommand, ResponseDto>
{
    private readonly IProductBackInStockSubscriptionRepository _subscriptionRepository;

    public CreateBackInStockSubscriptionCommandHandler(
        IProductBackInStockSubscriptionRepository subscriptionRepository)
    {
        _subscriptionRepository = subscriptionRepository;
    }

    public async Task<ResponseDto> Handle(CreateBackInStockSubscriptionCommand request, CancellationToken cancellationToken)
    {
        var response = new ResponseDto();

        if (string.IsNullOrWhiteSpace(request.PhoneNumber))
        {
            response.Success = false;
            response.Code = 400;
            response.Messages.Add(new Messages { message = "شماره موبایل الزامی است." });
            return response;
        }

        if (request.ProductId is null && request.ProductOfferId is null)
        {
            response.Success = false;
            response.Code = 400;
            response.Messages.Add(new Messages { message = "شناسه محصول نامعتبر است." });
            return response;
        }

        var existing = await _subscriptionRepository.GetActiveAsync(
            request.ProductId,
            request.ProductOfferId,
            request.PhoneNumber,
            cancellationToken);

        if (existing is not null)
        {
            response.Success = true;
            response.Code = 200;
            response.Messages.Add(new Messages { message = "درخواست شما قبلاً ثبت شده است." });
            return response;
        }

        var entity = new Domain.Entities.Catalog.ProductBackInStockSubscription(
            request.ProductId,
            request.ProductOfferId,
            request.PhoneNumber,
            request.UserId);

        await _subscriptionRepository.AddAsync(entity, cancellationToken);

        response.Success = true;
        response.Code = 201;
        response.Messages.Add(new Messages { message = "درخواست شما برای اطلاع‌رسانی ثبت شد." });
        return response;
    }
}


