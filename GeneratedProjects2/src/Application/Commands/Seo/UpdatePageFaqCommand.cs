using System;
using System.Threading;
using System.Threading.Tasks;
using LogsDtoCloneTest.Application.Abstractions.Messaging;
using LogsDtoCloneTest.Application.DTOs.Seo;
using LogsDtoCloneTest.Application.Interfaces;
using LogsDtoCloneTest.Domain.Enums;
using LogsDtoCloneTest.SharedKernel.BaseTypes;

namespace LogsDtoCloneTest.Application.Commands.Seo;

public sealed record UpdatePageFaqCommand(
    Guid Id,
    string Question,
    string Answer,
    int DisplayOrder) : ICommand<PageFaqDto>
{
    public sealed class Handler : ICommandHandler<UpdatePageFaqCommand, PageFaqDto>
    {
        private readonly IPageFaqRepository _repository;

        public Handler(IPageFaqRepository repository)
        {
            _repository = repository;
        }

        public async Task<Result<PageFaqDto>> Handle(UpdatePageFaqCommand request, CancellationToken cancellationToken)
        {
            var pageFaq = await _repository.GetByIdForUpdateAsync(request.Id, cancellationToken);

            if (pageFaq is null)
            {
                return Result<PageFaqDto>.Failure("سوال متداول مورد نظر یافت نشد.");
            }

            pageFaq.UpdateDetails(
                pageFaq.PageType,
                request.Question,
                request.Answer,
                request.DisplayOrder,
                pageFaq.PageIdentifier);

            await _repository.UpdateAsync(pageFaq, cancellationToken);

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

