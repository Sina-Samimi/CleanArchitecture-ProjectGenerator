using System;
using System.Collections.Generic;

namespace LogsDtoCloneTest.WebSite.Areas.Admin.Models;

public sealed class ActiveCartIndexViewModel
{
    public IReadOnlyCollection<ActiveCartListItemViewModel> Carts { get; set; } = Array.Empty<ActiveCartListItemViewModel>();

    public int TotalCount { get; set; }

    public int PageNumber { get; set; } = 1;

    public int PageSize { get; set; } = 20;

    public int TotalPages { get; set; }

    public ActiveCartFilterViewModel Filter { get; set; } = new();
}

public sealed class ActiveCartFilterViewModel
{
    public string? UserId { get; set; }

    public string? FromDate { get; set; }

    public string? ToDate { get; set; }
}

public sealed class ActiveCartListItemViewModel
{
    public Guid Id { get; set; }

    public string? UserId { get; set; }

    public string? UserFullName { get; set; }

    public string? UserPhoneNumber { get; set; }

    public int ItemCount { get; set; }

    public decimal Subtotal { get; set; }

    public decimal DiscountTotal { get; set; }

    public decimal GrandTotal { get; set; }

    public DateTimeOffset UpdateDate { get; set; }

    public DateTimeOffset CreateDate { get; set; }
}

