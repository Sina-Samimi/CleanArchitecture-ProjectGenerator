using System;
using System.Threading;
using System.Threading.Tasks;
using LogTableRenameTest.Application.Abstractions.Messaging;
using LogTableRenameTest.Application.DTOs.Seo;
using LogTableRenameTest.Application.Interfaces;
using LogTableRenameTest.Domain.Entities.Seo;
using LogTableRenameTest.SharedKernel.BaseTypes;

namespace LogTableRenameTest.Application.Commands.Seo;

public sealed record CreateSeoOgImageCommand(
    Guid SeoMetadataId,
    string ImageUrl,
    int DisplayOrder,
    int? Width = null,
    int? Height = null,
    string? ImageType = null,
    string? Alt = null) : ICommand<SeoOgImageDto>
{
    public sealed class Handler : ICommandHandler<CreateSeoOgImageCommand, SeoOgImageDto>
    {
        private readonly ISeoOgImageRepository _repository;

        public Handler(ISeoOgImageRepository repository)
        {
            _repository = repository;
        }

        public async Task<Result<SeoOgImageDto>> Handle(CreateSeoOgImageCommand request, CancellationToken cancellationToken)
        {
            var image = new SeoOgImage(
                request.SeoMetadataId,
                request.ImageUrl,
                request.DisplayOrder,
                request.Width,
                request.Height,
                request.ImageType,
                request.Alt);

            await _repository.AddAsync(image, cancellationToken);

            var dto = new SeoOgImageDto(
                image.Id,
                image.SeoMetadataId,
                image.ImageUrl,
                image.Width,
                image.Height,
                image.ImageType,
                image.Alt,
                image.DisplayOrder,
                image.CreateDate,
                image.UpdateDate);

            return Result<SeoOgImageDto>.Success(dto);
        }
    }
}

