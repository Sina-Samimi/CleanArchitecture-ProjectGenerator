using System.Threading;
using System.Threading.Tasks;
using Attar.Application.Abstractions.Messaging;
using Attar.Application.DTOs.Pages;
using Attar.Application.Interfaces;
using Attar.SharedKernel.BaseTypes;

namespace Attar.Application.Queries.Pages;

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

