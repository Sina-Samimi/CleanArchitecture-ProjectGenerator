using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TestAttarClone.Application.Abstractions.Messaging;
using TestAttarClone.Application.DTOs.Visits;
using TestAttarClone.Application.Interfaces;
using TestAttarClone.SharedKernel.BaseTypes;

namespace TestAttarClone.Application.Queries.Visits;

public sealed record GetPageVisitSummariesQuery(
    DateOnly? FromDate,
    DateOnly? ToDate,
    int PageNumber = 1,
    int PageSize = 10) : IQuery<IReadOnlyCollection<PageVisitSummaryDto>>
{
    public sealed class Handler : IQueryHandler<GetPageVisitSummariesQuery, IReadOnlyCollection<PageVisitSummaryDto>>
    {
        private readonly IVisitRepository _repository;

        public Handler(IVisitRepository repository)
        {
            _repository = repository;
        }

        public async Task<Result<IReadOnlyCollection<PageVisitSummaryDto>>> Handle(GetPageVisitSummariesQuery request, CancellationToken cancellationToken)
        {
            var summaries = await _repository.GetPageVisitSummariesAsync(
                request.FromDate,
                request.ToDate,
                request.PageNumber,
                request.PageSize,
                cancellationToken);

            return Result<IReadOnlyCollection<PageVisitSummaryDto>>.Success(summaries);
        }
    }
}

