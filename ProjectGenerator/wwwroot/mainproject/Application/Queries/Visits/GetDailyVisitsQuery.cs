using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Attar.Application.Abstractions.Messaging;
using Attar.Application.DTOs.Visits;
using Attar.Application.Interfaces;
using Attar.SharedKernel.BaseTypes;

namespace Attar.Application.Queries.Visits;

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

