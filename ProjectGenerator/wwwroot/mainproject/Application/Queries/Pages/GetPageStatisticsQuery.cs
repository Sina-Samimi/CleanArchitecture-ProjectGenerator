using System.Threading;
using System.Threading.Tasks;
using MobiRooz.Application.Abstractions.Messaging;
using MobiRooz.Application.DTOs.Pages;
using MobiRooz.Application.Interfaces;
using MobiRooz.SharedKernel.BaseTypes;

namespace MobiRooz.Application.Queries.Pages;

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

