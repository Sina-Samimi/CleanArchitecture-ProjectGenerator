using System;
using System.Collections.Generic;
using LogTableRenameTest.Domain.Enums;

namespace LogTableRenameTest.WebSite.Areas.Seller.Models;

public sealed class SellerOrderIndexViewModel
{
    public IReadOnlyCollection<SellerOrderListItemViewModel> Orders { get; set; } = Array.Empty<SellerOrderListItemViewModel>();

    public int TotalCount { get; set; }

    public int PageNumber { get; set; } = 1;

    public int PageSize { get; set; } = 20;

    public int TotalPages { get; set; }

    public SellerOrderFilterViewModel Filter { get; set; } = new();
}

public sealed class SellerOrderFilterViewModel
{
    public string? SearchTerm { get; set; }

    public InvoiceStatus? Status { get; set; }

    public string? FromDate { get; set; }

    public string? ToDate { get; set; }
}

public sealed class SellerOrderListItemViewModel
{
    public Guid InvoiceId { get; set; }

    public string InvoiceNumber { get; set; } = string.Empty;

    public DateTimeOffset OrderDate { get; set; }

    public InvoiceStatus Status { get; set; }

    public string? UserId { get; set; }

    public Guid InvoiceItemId { get; set; }

    public string ProductName { get; set; } = string.Empty;

    public decimal Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal Total { get; set; }

    public Guid? ProductId { get; set; }

    public Guid? VariantId { get; set; }

    public string? VariantInfo { get; set; } // اطلاعات variant برای نمایش (مثل: سایز: M, رنگ: قرمز)
}

