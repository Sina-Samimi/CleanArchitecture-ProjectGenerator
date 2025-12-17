using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LogsDtoCloneTest.Application.Abstractions.Messaging;
using LogsDtoCloneTest.Application.DTOs.Banners;
using LogsDtoCloneTest.Application.Interfaces;
using LogsDtoCloneTest.SharedKernel.BaseTypes;

namespace LogsDtoCloneTest.Application.Queries.Banners;

public sealed record GetBannersQuery(
    bool? IsActive = null,
    bool? ShowOnHomePage = null,
    int PageNumber = 1,
    int PageSize = 20) : IQuery<BannerListResultDto>
{
    public sealed class Handler : IQueryHandler<GetBannersQuery, BannerListResultDto>
    {
        private readonly IBannerRepository _repository;

        public Handler(IBannerRepository repository)
        {
            _repository = repository;
        }

        public async Task<Result<BannerListResultDto>> Handle(GetBannersQuery request, CancellationToken cancellationToken)
        {
            var pageNumber = request.PageNumber < 1 ? 1 : request.PageNumber;
            var pageSize = request.PageSize < 1 ? 20 : request.PageSize;

            var banners = await _repository.GetAllAsync(
                request.IsActive,
                request.ShowOnHomePage,
                null,
                pageNumber,
                pageSize,
                cancellationToken);

            var totalCount = await _repository.GetCountAsync(
                request.IsActive,
                request.ShowOnHomePage,
                null,
                cancellationToken);

            var dtos = banners.Select(b => new BannerDto(
                b.Id,
                b.Title,
                b.ImagePath,
                b.LinkUrl,
                b.AltText,
                b.DisplayOrder,
                b.IsActive,
                b.StartDate,
                b.EndDate,
                b.ShowOnHomePage,
                b.CreateDate,
                b.UpdateDate)).ToArray();

            var result = new BannerListResultDto(
                dtos,
                totalCount,
                pageNumber,
                pageSize);

            return Result<BannerListResultDto>.Success(result);
        }
    }
}

