using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LogTableRenameTest.Application.Abstractions.Messaging;
using LogTableRenameTest.Application.DTOs.Seo;
using LogTableRenameTest.Application.Interfaces;
using LogTableRenameTest.Domain.Enums;
using LogTableRenameTest.SharedKernel.BaseTypes;

namespace LogTableRenameTest.Application.Queries.Seo;

public sealed record GetPageFaqsQuery(SeoPageType PageType, string? PageIdentifier = null) : IQuery<PageFaqListDto>
{
    public sealed class Handler : IQueryHandler<GetPageFaqsQuery, PageFaqListDto>
    {
        private readonly IPageFaqRepository _repository;

        public Handler(IPageFaqRepository repository)
        {
            _repository = repository;
        }

        public async Task<Result<PageFaqListDto>> Handle(GetPageFaqsQuery request, CancellationToken cancellationToken)
        {
            var faqs = await _repository.GetByPageTypeAndIdentifierAsync(request.PageType, request.PageIdentifier, cancellationToken);

            var faqDtos = faqs
                .Select(faq => new PageFaqDto(
                    faq.Id,
                    faq.PageType,
                    faq.PageIdentifier,
                    faq.Question,
                    faq.Answer,
                    faq.DisplayOrder,
                    faq.CreateDate,
                    faq.UpdateDate))
                .ToList();

            var result = new PageFaqListDto(faqDtos);
            return Result<PageFaqListDto>.Success(result);
        }
    }
}

