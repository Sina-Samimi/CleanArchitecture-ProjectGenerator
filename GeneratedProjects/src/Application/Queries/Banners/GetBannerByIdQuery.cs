using System;
using System.Threading;
using System.Threading.Tasks;
using TestAttarClone.Application.Abstractions.Messaging;
using TestAttarClone.Application.DTOs.Banners;
using TestAttarClone.Application.Interfaces;
using TestAttarClone.SharedKernel.BaseTypes;

namespace TestAttarClone.Application.Queries.Banners;

public sealed record GetBannerByIdQuery(Guid Id) : IQuery<BannerDto>
{
    public sealed class Handler : IQueryHandler<GetBannerByIdQuery, BannerDto>
    {
        private readonly IBannerRepository _repository;

        public Handler(IBannerRepository repository)
        {
            _repository = repository;
        }

        public async Task<Result<BannerDto>> Handle(GetBannerByIdQuery request, CancellationToken cancellationToken)
        {
            if (request.Id == Guid.Empty)
            {
                return Result<BannerDto>.Failure("شناسه بنر معتبر نیست.");
            }

            var banner = await _repository.GetByIdAsync(request.Id, cancellationToken);

            if (banner is null)
            {
                return Result<BannerDto>.Failure("بنر یافت نشد.");
            }

            var dto = new BannerDto(
                banner.Id,
                banner.Title,
                banner.ImagePath,
                banner.LinkUrl,
                banner.AltText,
                banner.DisplayOrder,
                banner.IsActive,
                banner.StartDate,
                banner.EndDate,
                banner.ShowOnHomePage,
                banner.CreateDate,
                banner.UpdateDate);

            return Result<BannerDto>.Success(dto);
        }
    }
}

