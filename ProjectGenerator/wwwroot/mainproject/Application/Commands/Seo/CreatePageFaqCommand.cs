using System;
using System.Threading;
using System.Threading.Tasks;
using Attar.Application.Abstractions.Messaging;
using Attar.Application.DTOs.Seo;
using Attar.Application.Interfaces;
using Attar.Domain.Entities.Seo;
using Attar.Domain.Enums;
using Attar.SharedKernel.BaseTypes;

namespace Attar.Application.Commands.Seo;

public sealed record CreatePageFaqCommand(
    SeoPageType PageType,
    string Question,
    string Answer,
    int DisplayOrder,
    string? PageIdentifier = null) : ICommand<PageFaqDto>
{
    public sealed class Handler : ICommandHandler<CreatePageFaqCommand, PageFaqDto>
    {
        private readonly IPageFaqRepository _repository;

        public Handler(IPageFaqRepository repository)
        {
            _repository = repository;
        }

        public async Task<Result<PageFaqDto>> Handle(CreatePageFaqCommand request, CancellationToken cancellationToken)
        {
            var pageFaq = new PageFaq(
                request.PageType,
                request.Question,
                request.Answer,
                request.DisplayOrder,
                request.PageIdentifier);

            await _repository.AddAsync(pageFaq, cancellationToken);

            var dto = new PageFaqDto(
                pageFaq.Id,
                pageFaq.PageType,
                pageFaq.PageIdentifier,
                pageFaq.Question,
                pageFaq.Answer,
                pageFaq.DisplayOrder,
                pageFaq.CreateDate,
                pageFaq.UpdateDate);

            return Result<PageFaqDto>.Success(dto);
        }
    }
}

