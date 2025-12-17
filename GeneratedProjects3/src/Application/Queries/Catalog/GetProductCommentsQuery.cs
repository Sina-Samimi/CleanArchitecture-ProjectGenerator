using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LogTableRenameTest.Application.Abstractions.Messaging;
using LogTableRenameTest.Application.DTOs.Catalog;
using LogTableRenameTest.Application.Interfaces;
using LogTableRenameTest.SharedKernel.BaseTypes;

namespace LogTableRenameTest.Application.Queries.Catalog;

public sealed record GetProductCommentsQuery(Guid ProductId) : IQuery<ProductCommentListResultDto>
{
    public sealed class Handler : IQueryHandler<GetProductCommentsQuery, ProductCommentListResultDto>
    {
        private readonly IProductRepository _productRepository;
        private readonly IProductCommentRepository _productCommentRepository;

        public Handler(IProductRepository productRepository, IProductCommentRepository productCommentRepository)
        {
            _productRepository = productRepository;
            _productCommentRepository = productCommentRepository;
        }

        public async Task<Result<ProductCommentListResultDto>> Handle(GetProductCommentsQuery request, CancellationToken cancellationToken)
        {
            if (request.ProductId == Guid.Empty)
            {
                return Result<ProductCommentListResultDto>.Failure("شناسه محصول معتبر نیست.");
            }

            var product = await _productRepository.GetByIdAsync(request.ProductId, cancellationToken);
            if (product is null)
            {
                return Result<ProductCommentListResultDto>.Failure("محصول مورد نظر یافت نشد.");
            }

            var comments = await _productCommentRepository.GetByProductIdAsync(request.ProductId, cancellationToken);

            var commentDtos = comments
                .OrderBy(comment => comment.CreateDate)
                .Select(comment => new ProductCommentDto(
                    comment.Id,
                    comment.ProductId,
                    comment.ParentId,
                    comment.AuthorName,
                    comment.Content,
                    comment.Rating,
                    comment.IsApproved,
                    comment.CreateDate,
                    comment.UpdateDate,
                    comment.ApprovedById,
                    comment.ApprovedBy?.FullName,
                    comment.ApprovedAt))
                .ToArray();

            var productSummary = new ProductSummaryDto(product.Id, product.Name, product.SeoSlug);

            return Result<ProductCommentListResultDto>.Success(new ProductCommentListResultDto(productSummary, commentDtos));
        }
    }
}
