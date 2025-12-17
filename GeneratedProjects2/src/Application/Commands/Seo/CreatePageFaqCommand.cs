using System;
using System.Threading;
using System.Threading.Tasks;
using LogsDtoCloneTest.Application.Abstractions.Messaging;
using LogsDtoCloneTest.Application.DTOs.Seo;
using LogsDtoCloneTest.Application.Interfaces;
using LogsDtoCloneTest.Domain.Entities.Seo;
using LogsDtoCloneTest.Domain.Enums;
using LogsDtoCloneTest.SharedKernel.BaseTypes;

namespace LogsDtoCloneTest.Application.Commands.Seo;

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

