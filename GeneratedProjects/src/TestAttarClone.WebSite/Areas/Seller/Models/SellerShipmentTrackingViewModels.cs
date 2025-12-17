using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using TestAttarClone.Domain.Enums;

namespace TestAttarClone.WebSite.Areas.Seller.Models;

public sealed class SellerShipmentTrackingIndexViewModel
{
    public Guid ProductId { get; set; }

    public string ProductName { get; set; } = string.Empty;

    public IReadOnlyCollection<SellerShipmentTrackingItemViewModel> InvoiceItems { get; set; }
        = Array.Empty<SellerShipmentTrackingItemViewModel>();
}

public sealed class SellerShipmentTrackingItemViewModel
{
    public Guid InvoiceItemId { get; set; }

    public Guid InvoiceId { get; set; }

    public string InvoiceNumber { get; set; } = string.Empty;

    public string? CustomerName { get; set; }

    public string? CustomerPhone { get; set; }

    public int Quantity { get; set; }

    public ShipmentStatus? Status { get; set; }

    public string? TrackingNumber { get; set; }

    public DateTimeOffset? StatusDate { get; set; }

    public string? Notes { get; set; }

    public Guid? TrackingId { get; set; }

    public Guid? VariantId { get; set; }

    public string? VariantInfo { get; set; } // اطلاعات variant برای نمایش
}

public sealed class SellerShipmentTrackingFormViewModel
{
    public Guid? Id { get; set; }

    [Required]
    public Guid ProductId { get; set; }

    [Required]
    public Guid InvoiceItemId { get; set; }

    [Required]
    [Display(Name = "وضعیت")]
    public string StatusText { get; set; } = string.Empty;

    [Display(Name = "شماره پیگیری پست")]
    [MaxLength(100)]
    public string? TrackingNumber { get; set; }

    [Display(Name = "توضیحات")]
    [MaxLength(1000)]
    [DataType(DataType.MultilineText)]
    public string? Notes { get; set; }

    [Required]
    [Display(Name = "تاریخ")]
    public DateTime StatusDate { get; set; } = DateTime.UtcNow;

    [Display(Name = "تاریخ (شمسی)")]
    public string? StatusDatePersian { get; set; }
}

