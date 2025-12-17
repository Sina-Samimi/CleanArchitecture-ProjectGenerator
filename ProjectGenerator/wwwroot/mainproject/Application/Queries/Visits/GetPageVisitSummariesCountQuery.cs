using System;
using System.Threading;
using System.Threading.Tasks;
using Attar.Application.Abstractions.Messaging;
using Attar.Application.Interfaces;
using Attar.SharedKernel.BaseTypes;

namespace Attar.Application.Queries.Visits;

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

