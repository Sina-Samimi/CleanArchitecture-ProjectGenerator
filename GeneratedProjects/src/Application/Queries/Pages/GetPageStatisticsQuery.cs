using System.Threading;
using System.Threading.Tasks;
using TestAttarClone.Application.Abstractions.Messaging;
using TestAttarClone.Application.DTOs.Pages;
using TestAttarClone.Application.Interfaces;
using TestAttarClone.SharedKernel.BaseTypes;

namespace TestAttarClone.Application.Queries.Pages;

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

