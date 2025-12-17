using System.Threading;
using System.Threading.Tasks;
using LogTableRenameTest.Application.Abstractions.Messaging;
using LogTableRenameTest.Application.DTOs.Pages;
using LogTableRenameTest.Application.Interfaces;
using LogTableRenameTest.SharedKernel.BaseTypes;

namespace LogTableRenameTest.Application.Queries.Pages;

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

