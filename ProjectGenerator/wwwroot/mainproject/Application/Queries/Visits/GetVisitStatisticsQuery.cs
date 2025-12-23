using System;
using System.Threading;
using System.Threading.Tasks;
using MobiRooz.Application.Abstractions.Messaging;
using MobiRooz.Application.DTOs.Visits;
using MobiRooz.Application.Interfaces;
using MobiRooz.SharedKernel.BaseTypes;

namespace MobiRooz.Application.Queries.Visits;

public sealed record GetVisitStatisticsQuery(
    bool IsPageVisit,
    Guid? PageId,
    DateOnly? FromDate,
    DateOnly? ToDate) : IQuery<VisitStatisticsDto>
{
    public sealed class Handler : IQueryHandler<GetVisitStatisticsQuery, VisitStatisticsDto>
    {
        private readonly IVisitRepository _repository;

        public Handler(IVisitRepository repository)
        {
            _repository = repository;
        }

        public async Task<Result<VisitStatisticsDto>> Handle(GetVisitStatisticsQuery request, CancellationToken cancellationToken)
        {
            VisitStatisticsDto statistics;

            if (request.IsPageVisit)
            {
                statistics = await _repository.GetPageVisitStatisticsAsync(
                    request.PageId,
                    request.FromDate,
                    request.ToDate,
                    cancellationToken);
            }
            else
            {
                statistics = await _repository.GetSiteVisitStatisticsAsync(
                    request.FromDate,
                    request.ToDate,
                    cancellationToken);
            }

            return Result<VisitStatisticsDto>.Success(statistics);
        }
    }
}

