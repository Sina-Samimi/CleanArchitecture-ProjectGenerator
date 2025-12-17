using System;
using System.Collections.Generic;
using LogsDtoCloneTest.Domain.Enums;

namespace LogsDtoCloneTest.WebSite.Areas.Admin.Models;

public sealed class OrderIndexViewModel
{
    public IReadOnlyCollection<OrderListItemViewModel> Orders { get; set; } = Array.Empty<OrderListItemViewModel>();

    public int TotalCount { get; set; }

    public int PageNumber { get; set; } = 1;

    public int PageSize { get; set; } = 20;

    public int TotalPages { get; set; }

    public OrderFilterViewModel Filter { get; set; } = new();
}

public sealed class OrderFilterViewModel
{
    public string? SearchTerm { get; set; }

    public string? UserId { get; set; }

    public InvoiceStatus? Status { get; set; }

    public string? FromDate { get; set; }

    public string? ToDate { get; set; }
}

public sealed class OrderListItemViewModel
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

    public string? SellerId { get; set; }
}

