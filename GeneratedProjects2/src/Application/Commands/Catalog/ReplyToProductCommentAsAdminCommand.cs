using System;
using System.Threading;
using System.Threading.Tasks;
using LogsDtoCloneTest.Application.Abstractions.Messaging;
using LogsDtoCloneTest.Application.Interfaces;
using LogsDtoCloneTest.Domain.Entities.Catalog;
using LogsDtoCloneTest.SharedKernel.BaseTypes;

namespace LogsDtoCloneTest.Application.Commands.Catalog;

public sealed record ReplyToProductCommentAsAdminCommand(
    Guid ProductId,
    Guid ParentCommentId,
    string AuthorName,
    string Content,
    string AdminId) : ICommand
{
    public sealed class Handler : ICommandHandler<ReplyToProductCommentAsAdminCommand>
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

        public async Task<Result> Handle(ReplyToProductCommentAsAdminCommand request, CancellationToken cancellationToken)
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

            if (string.IsNullOrWhiteSpace(request.AdminId))
            {
                return Result.Failure("شناسه ادمین معتبر نیست.");
            }

            // Verify product exists
            var product = await _productRepository.GetByIdAsync(request.ProductId, cancellationToken);
            if (product is null || product.IsDeleted)
            {
                return Result.Failure("محصول مورد نظر یافت نشد.");
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

            // Create reply comment directly (approved by default for admin replies)
            var reply = new ProductComment(
                request.ProductId,
                request.AuthorName.Trim(),
                request.Content.Trim(),
                0, // No rating for replies
                parentComment,
                isApproved: true); // Admin replies are auto-approved

            await _productCommentRepository.AddAsync(reply, cancellationToken);

            return Result.Success();
        }
    }
}

