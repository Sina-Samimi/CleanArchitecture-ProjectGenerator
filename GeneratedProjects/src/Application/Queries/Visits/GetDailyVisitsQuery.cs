using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TestAttarClone.Application.Abstractions.Messaging;
using TestAttarClone.Application.DTOs.Visits;
using TestAttarClone.Application.Interfaces;
using TestAttarClone.SharedKernel.BaseTypes;

namespace TestAttarClone.Application.Queries.Visits;

public sealed record GetDailyVisitsQuery(
    bool IsPageVisit,
    Guid? PageId,
    DateOnly? FromDate,
    DateOnly? ToDate) : IQuery<IReadOnlyCollection<DailyVisitDto>>
{
    public sealed class Handler : IQueryHandler<GetDailyVisitsQuery, IReadOnlyCollection<DailyVisitDto>>
    {
        private readonly IVisitRepository _repository;

        public Handler(IVisitRepository repository)
        {
            _repository = repository;
        }

        public async Task<Result<IReadOnlyCollection<DailyVisitDto>>> Handle(GetDailyVisitsQuery request, CancellationToken cancellationToken)
        {
            IReadOnlyCollection<DailyVisitDto> dailyVisits;

            if (request.IsPageVisit)
            {
                dailyVisits = await _repository.GetDailyPageVisitsAsync(
                    request.PageId,
                    request.FromDate,
                    request.ToDate,
                    cancellationToken);
            }
            else
            {
                dailyVisits = await _repository.GetDailySiteVisitsAsync(
                    request.FromDate,
                    request.ToDate,
                    cancellationToken);
            }

            return Result<IReadOnlyCollection<DailyVisitDto>>.Success(dailyVisits);
        }
    }
}

