using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Attar.WebSite.Areas.Seller.Models;

public sealed class SellerProductCommentsViewModel
{
    public Guid ProductId { get; init; }

    public string ProductName { get; init; } = string.Empty;

    public string DefaultAuthorName { get; init; } = string.Empty;

    public IReadOnlyCollection<SellerProductCommentViewModel> Comments { get; init; }
        = Array.Empty<SellerProductCommentViewModel>();
}

public sealed class SellerProductCommentViewModel
{
    public Guid Id { get; init; }

    public Guid? ParentId { get; init; }

    public string AuthorName { get; init; } = string.Empty;

    public string Content { get; init; } = string.Empty;

    public double Rating { get; init; }

    public bool IsApproved { get; init; }

    public DateTimeOffset CreatedAt { get; init; }

    public DateTimeOffset UpdatedAt { get; init; }

    public IReadOnlyCollection<SellerProductCommentViewModel> Replies { get; init; }
        = Array.Empty<SellerProductCommentViewModel>();
}

public sealed class SellerProductCommentReplyViewModel
{
    [Display(Name = "نام پاسخ‌دهنده")]
    [MaxLength(200, ErrorMessage = "نام نمی‌تواند بیش از ۲۰۰ کاراکتر باشد.")]
    public string? AuthorName { get; set; }

    [Display(Name = "متن پاسخ")]
    [Required(ErrorMessage = "متن پاسخ را وارد کنید.")]
    [MaxLength(2000, ErrorMessage = "متن پاسخ نمی‌تواند بیش از ۲۰۰۰ کاراکتر باشد.")]
    public string Content { get; set; } = string.Empty;
}

