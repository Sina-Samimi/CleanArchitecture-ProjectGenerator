using System;
using System.Threading;
using System.Threading.Tasks;
using LogsDtoCloneTest.Application.Abstractions.Messaging;
using LogsDtoCloneTest.Application.Interfaces;
using LogsDtoCloneTest.SharedKernel.BaseTypes;

namespace LogsDtoCloneTest.Application.Queries.Visits;

public sealed record GetPageVisitSummariesCountQuery(
    DateOnly? FromDate,
    DateOnly? ToDate) : IQuery<int>
{
    public sealed class Handler : IQueryHandler<GetPageVisitSummariesCountQuery, int>
    {
        private readonly IVisitRepository _repository;

        public Handler(IVisitRepository repository)
        {
            _repository = repository;
        }

        public async Task<Result<int>> Handle(GetPageVisitSummariesCountQuery request, CancellationToken cancellationToken)
        {
            var count = await _repository.GetPageVisitSummariesCountAsync(
                request.FromDate,
                request.ToDate,
                cancellationToken);

            return Result<int>.Success(count);
        }
    }
}

