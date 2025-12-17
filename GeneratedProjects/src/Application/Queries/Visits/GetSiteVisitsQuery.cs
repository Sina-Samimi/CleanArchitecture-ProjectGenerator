using System;
using System.Threading;
using System.Threading.Tasks;
using TestAttarClone.Application.Abstractions.Messaging;
using TestAttarClone.Application.DTOs.Visits;
using TestAttarClone.Application.Interfaces;
using TestAttarClone.SharedKernel.BaseTypes;

namespace TestAttarClone.Application.Queries.Visits;

public sealed record GetSiteVisitsQuery(
    DateOnly? FromDate = null,
    DateOnly? ToDate = null,
    string? IpAddress = null,
    string? DeviceType = null,
    string? Browser = null,
    string? OperatingSystem = null,
    int PageNumber = 1,
    int PageSize = 20) : IQuery<SiteVisitListResultDto>;

public sealed class GetSiteVisitsQueryHandler : IQueryHandler<GetSiteVisitsQuery, SiteVisitListResultDto>
{
    private readonly IVisitRepository _repository;

    public GetSiteVisitsQueryHandler(IVisitRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<SiteVisitListResultDto>> Handle(GetSiteVisitsQuery request, CancellationToken cancellationToken)
    {
        if (request.PageNumber < 1)
        {
            return Result<SiteVisitListResultDto>.Failure("شماره صفحه باید بزرگتر از 0 باشد.");
        }

        if (request.PageSize < 1 || request.PageSize > 100)
        {
            return Result<SiteVisitListResultDto>.Failure("تعداد آیتم‌ها در هر صفحه باید بین 1 تا 100 باشد.");
        }

        var visits = await _repository.GetSiteVisitsAsync(
            request.FromDate,
            request.ToDate,
            request.IpAddress,
            request.DeviceType,
            request.Browser,
            request.OperatingSystem,
            request.PageNumber,
            request.PageSize,
            cancellationToken);

        var totalCount = await _repository.GetSiteVisitsCountAsync(
            request.FromDate,
            request.ToDate,
            request.IpAddress,
            request.DeviceType,
            request.Browser,
            request.OperatingSystem,
            cancellationToken);

        var totalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize);
        var pageNumber = Math.Min(request.PageNumber, Math.Max(1, totalPages));

        var result = new SiteVisitListResultDto(
            Items: visits,
            TotalCount: totalCount,
            PageNumber: pageNumber,
            PageSize: request.PageSize,
            TotalPages: totalPages);

        return Result<SiteVisitListResultDto>.Success(result);
    }
}

