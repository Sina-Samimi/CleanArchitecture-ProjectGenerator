using System.Threading;
using System.Threading.Tasks;
using LogsDtoCloneTest.Application.Abstractions.Messaging;
using LogsDtoCloneTest.Application.DTOs.Pages;
using LogsDtoCloneTest.Application.Interfaces;
using LogsDtoCloneTest.SharedKernel.BaseTypes;

namespace LogsDtoCloneTest.Application.Queries.Pages;

public sealed record GetPageStatisticsQuery : IQuery<PageStatisticsDto>
{
    public sealed class Handler : IQueryHandler<GetPageStatisticsQuery, PageStatisticsDto>
    {
        private readonly IPageRepository _repository;

        public Handler(IPageRepository repository)
        {
            _repository = repository;
        }

        public async Task<Result<PageStatisticsDto>> Handle(GetPageStatisticsQuery request, CancellationToken cancellationToken)
        {
            var statistics = await _repository.GetStatisticsAsync(cancellationToken);
            return Result<PageStatisticsDto>.Success(statistics);
        }
    }
}

