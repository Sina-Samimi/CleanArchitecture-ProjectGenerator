using System;
using System.Threading;
using System.Threading.Tasks;
using Attar.Application.Abstractions.Messaging;
using Attar.Application.Interfaces;
using Attar.Domain.Entities.Catalog;
using Attar.SharedKernel.BaseTypes;

namespace Attar.Application.Commands.Catalog;

public sealed record ReplyToProductCommentCommand(
    Guid ProductId,
    Guid ParentCommentId,
    string AuthorName,
    string Content,
    string SellerId) : ICommand
{
    public sealed class Handler : ICommandHandler<ReplyToProductCommentCommand>
    {
        private readonly IProductRepository _productRepository;
        private readonly IProductCommentRepository _productCommentRepository;

        public Handler(
            IProductRepository productRepository,
            IProductCommentRepository productCommentRepository)
        {
            _productRepository = productRepository;
            _productCommentRepository = productCommentRepository;
        }

        public async Task<Result> Handle(ReplyToProductCommentCommand request, CancellationToken cancellationToken)
        {
            if (request.ProductId == Guid.Empty)
            {
                return Result.Failure("شناسه محصول معتبر نیست.");
            }

            if (request.ParentCommentId == Guid.Empty)
            {
                return Result.Failure("شناسه کامنت والد معتبر نیست.");
            }

            if (string.IsNullOrWhiteSpace(request.AuthorName))
            {
                return Result.Failure("نام نویسنده نمی‌تواند خالی باشد.");
            }

            if (string.IsNullOrWhiteSpace(request.Content))
            {
                return Result.Failure("متن پاسخ نمی‌تواند خالی باشد.");
            }

            if (string.IsNullOrWhiteSpace(request.SellerId))
            {
                return Result.Failure("شناسه فروشنده معتبر نیست.");
            }

            // Verify product ownership (using lightweight query)
            var product = await _productRepository.GetByIdAsync(request.ProductId, cancellationToken);
            if (product is null || product.IsDeleted)
            {
                return Result.Failure("محصول مورد نظر یافت نشد.");
            }

            var isOwner = string.Equals(product.CreatorId, request.SellerId, StringComparison.Ordinal)
                || string.Equals(product.SellerId, request.SellerId, StringComparison.Ordinal);

            if (!isOwner)
            {
                return Result.Failure("شما اجازه پاسخ به کامنت‌های این محصول را ندارید.");
            }

            // Get parent comment
            var parentComment = await _productCommentRepository.GetByIdAsync(request.ParentCommentId, cancellationToken);
            if (parentComment is null || parentComment.IsDeleted)
            {
                return Result.Failure("کامنت والد یافت نشد.");
            }

            if (parentComment.ProductId != request.ProductId)
            {
                return Result.Failure("کامنت والد متعلق به این محصول نیست.");
            }

            // Create reply comment directly (approved by default for seller replies)
            var reply = new ProductComment(
                request.ProductId,
                request.AuthorName.Trim(),
                request.Content.Trim(),
                0, // No rating for replies
                parentComment,
                isApproved: true); // Seller replies are auto-approved

            await _productCommentRepository.AddAsync(reply, cancellationToken);

            return Result.Success();
        }
    }
}

