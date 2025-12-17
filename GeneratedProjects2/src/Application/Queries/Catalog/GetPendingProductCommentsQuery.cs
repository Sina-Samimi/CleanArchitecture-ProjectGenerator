using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LogsDtoCloneTest.Application.Abstractions.Messaging;
using LogsDtoCloneTest.Application.DTOs.Catalog;
using LogsDtoCloneTest.Application.Interfaces;
using LogsDtoCloneTest.SharedKernel.BaseTypes;

namespace LogsDtoCloneTest.Application.Queries.Catalog;

public sealed record GetPendingProductCommentsQuery() : IQuery<PendingProductCommentListResultDto>;

public sealed class GetPendingProductCommentsQueryHandler : IQueryHandler<GetPendingProductCommentsQuery, PendingProductCommentListResultDto>
{
    private readonly IProductCommentRepository _commentRepository;

    public GetPendingProductCommentsQueryHandler(IProductCommentRepository commentRepository)
    {
        _commentRepository = commentRepository;
    }

    public async Task<Result<PendingProductCommentListResultDto>> Handle(GetPendingProductCommentsQuery request, CancellationToken cancellationToken)
    {
        var comments = await _commentRepository.GetPendingAsync(cancellationToken);

        var items = comments.Select(c => new PendingProductCommentDto(
            c.Id,
            c.ProductId,
            c.Product?.Name ?? "نامشخص",
            c.Product?.SeoSlug,
            c.AuthorName,
            (c.Content?.Length > 200) ? c.Content.Substring(0, 200) + "..." : (c.Content ?? string.Empty),
            c.CreateDate)).ToArray();

        var result = new PendingProductCommentListResultDto(items, items.Length);

        return Result<PendingProductCommentListResultDto>.Success(result);
    }
}
