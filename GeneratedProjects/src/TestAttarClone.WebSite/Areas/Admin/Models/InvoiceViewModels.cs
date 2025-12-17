using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using TestAttarClone.Domain.Enums;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace TestAttarClone.WebSite.Areas.Admin.Models;

public sealed class InvoiceIndexViewModel
{
    public InvoiceSummaryViewModel Summary { get; set; } = new();

    public IReadOnlyCollection<InvoiceListItemViewModel> Invoices { get; set; } = Array.Empty<InvoiceListItemViewModel>();

    public InvoiceFilterViewModel Filter { get; set; } = new();

    public IReadOnlyCollection<SelectListItem> UserOptions { get; set; } = Array.Empty<SelectListItem>();

    public DateTimeOffset GeneratedAt { get; set; }
}

public sealed class InvoiceFilterViewModel
{
    public string? SearchTerm { get; set; }

    public InvoiceStatus? Status { get; set; }

    public string? UserId { get; set; }

    public string? UserDisplayName { get; set; }

    public string? IssueDateFrom { get; set; }

    public string? IssueDateTo { get; set; }

    public string? IssueDateFromDisplay { get; set; }

    public string? IssueDateToDisplay { get; set; }
}

public sealed class InvoiceSummaryViewModel
{
    public int TotalInvoices { get; set; }

    public int PendingInvoices { get; set; }

    public int DraftInvoices { get; set; }

    public int PaidInvoices { get; set; }

    public int PartiallyPaidInvoices { get; set; }

    public int CancelledInvoices { get; set; }

    public int OverdueInvoices { get; set; }

    public decimal TotalBilledAmount { get; set; }

    public decimal TotalOutstandingAmount { get; set; }

    public decimal TotalCollectedAmount { get; set; }
}

public sealed class InvoiceListItemViewModel
{
    public Guid Id { get; set; }

    public string InvoiceNumber { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public InvoiceStatus Status { get; set; }

    public string Currency { get; set; } = "IRT";

    public decimal GrandTotal { get; set; }

    public decimal PaidAmount { get; set; }

    public decimal OutstandingAmount { get; set; }

    public DateTimeOffset IssueDate { get; set; }

    public DateTimeOffset? DueDate { get; set; }

    public string? UserId { get; set; }

    public string? UserDisplayName { get; set; }

    public string? ExternalReference { get; set; }

    public bool IsOverdue => Status == InvoiceStatus.Overdue;

    public bool IsPaid => Status == InvoiceStatus.Paid;
}

public sealed class InvoiceFormViewModel
{
    public Guid? Id { get; set; }

    [Display(Name = "شماره فاکتور")]
    public string? InvoiceNumber { get; set; }

    [Required(ErrorMessage = "عنوان فاکتور الزامی است.")]
    [Display(Name = "عنوان")]
    public string Title { get; set; } = string.Empty;

    [Display(Name = "توضیحات")]
    [DataType(DataType.MultilineText)]
    public string? Description { get; set; }

    [Required(ErrorMessage = "واحد پول را مشخص کنید.")]
    [Display(Name = "واحد پول")]
    public string Currency { get; set; } = "IRT";

    [Display(Name = "شناسه کاربر")]
    public string? UserId { get; set; }

    [Display(Name = "تاریخ صدور (شمسی)")]
    public string? IssueDatePersian { get; set; }

    [Display(Name = "شماره ارجاع خارجی")]
    public string? ExternalReference { get; set; }

    [Display(Name = "تاریخ صدور")]
    [DataType(DataType.Date)]
    public DateTime IssueDate { get; set; } = DateTime.UtcNow.Date;

    [Display(Name = "تاریخ سررسید")]
    [DataType(DataType.Date)]
    public DateTime? DueDate { get; set; }

    [Display(Name = "تاریخ سررسید (شمسی)")]
    public string? DueDatePersian { get; set; }

    [Display(Name = "درصد مالیات")]
    [Range(0, 100, ErrorMessage = "درصد مالیات باید بین ۰ تا ۱۰۰ باشد.")]
    public decimal TaxRatePercent { get; set; }

    [Display(Name = "مبلغ مالیات")]
    [Range(0, double.MaxValue, ErrorMessage = "مالیات نمی‌تواند منفی باشد.")]
    public decimal TaxAmount { get; set; }

    [Display(Name = "تعدیل")]
    public decimal AdjustmentAmount { get; set; }

    public List<InvoiceItemFormViewModel> Items { get; set; } = new();

    public IReadOnlyCollection<SelectListItem> UserOptions { get; set; } = Array.Empty<SelectListItem>();
}

public sealed class InvoiceItemFormViewModel
{
    public Guid? Id { get; set; }

    [Required]
    [Display(Name = "عنوان آیتم")]
    public string Name { get; set; } = string.Empty;

    [Display(Name = "توضیحات")]
    public string? Description { get; set; }

    [Display(Name = "نوع آیتم")]
    public InvoiceItemType ItemType { get; set; } = InvoiceItemType.Product;

    [Display(Name = "شناسه مرجع")]
    public Guid? ReferenceId { get; set; }

    [Range(0.01, double.MaxValue, ErrorMessage = "تعداد باید بیشتر از صفر باشد.")]
    [Display(Name = "تعداد")]
    public decimal Quantity { get; set; } = 1;

    [Range(0, double.MaxValue, ErrorMessage = "قیمت واحد نامعتبر است.")]
    [Display(Name = "قیمت واحد")]
    public decimal UnitPrice { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "تخفیف نامعتبر است.")]
    [Display(Name = "تخفیف")]
    public decimal? DiscountAmount { get; set; }

    public List<InvoiceItemAttributeFormViewModel> Attributes { get; set; } = new();
}

public sealed class InvoiceItemAttributeFormViewModel
{
    public Guid? Id { get; set; }

    [Display(Name = "عنوان ویژگی")]
    public string Key { get; set; } = string.Empty;

    [Display(Name = "مقدار")]
    public string Value { get; set; } = string.Empty;
}

public sealed class InvoiceDetailViewModel
{
    public Guid Id { get; set; }

    public string InvoiceNumber { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public InvoiceStatus Status { get; set; }

    public string Currency { get; set; } = "IRT";

    public decimal Subtotal { get; set; }

    public decimal DiscountTotal { get; set; }

    public decimal TaxAmount { get; set; }

    public decimal AdjustmentAmount { get; set; }

    public decimal GrandTotal { get; set; }

    public decimal TaxRatePercent { get; set; }

    public decimal PaidAmount { get; set; }

    public decimal OutstandingAmount { get; set; }

    public DateTimeOffset IssueDate { get; set; }

    public DateTimeOffset? DueDate { get; set; }

    public string? UserId { get; set; }

    public string? UserDisplayName { get; set; }

    public string? UserPhoneNumber { get; set; }

    public string? ExternalReference { get; set; }

    public IReadOnlyCollection<InvoiceItemDetailViewModel> Items { get; set; } = Array.Empty<InvoiceItemDetailViewModel>();

    public IReadOnlyCollection<InvoiceTransactionViewModel> Transactions { get; set; } = Array.Empty<InvoiceTransactionViewModel>();

    public InvoiceTransactionFormViewModel NewTransaction { get; set; } = new();

    public IReadOnlyCollection<ShipmentTrackingViewModel> ShipmentTrackings { get; set; } = Array.Empty<ShipmentTrackingViewModel>();

    public Guid? ShippingAddressId { get; set; }

    public string? ShippingRecipientName { get; set; }

    public string? ShippingRecipientPhone { get; set; }

    public string? ShippingProvince { get; set; }

    public string? ShippingCity { get; set; }

    public string? ShippingPostalCode { get; set; }

    public string? ShippingAddressLine { get; set; }

    public string? ShippingPlaque { get; set; }

    public string? ShippingUnit { get; set; }
}

public sealed class InvoiceItemDetailViewModel
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public InvoiceItemType ItemType { get; set; }

    public Guid? ReferenceId { get; set; }

    public decimal Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal? DiscountAmount { get; set; }

    public decimal Subtotal { get; set; }

    public decimal Total { get; set; }

    public IReadOnlyCollection<InvoiceItemAttributeFormViewModel> Attributes { get; set; } = Array.Empty<InvoiceItemAttributeFormViewModel>();
}

public sealed class InvoiceTransactionViewModel
{
    public Guid Id { get; set; }

    public decimal Amount { get; set; }

    public PaymentMethod Method { get; set; }

    public TransactionStatus Status { get; set; }

    public string Reference { get; set; } = string.Empty;

    public string? GatewayName { get; set; }

    public string? Description { get; set; }

    public string? Metadata { get; set; }

    public DateTimeOffset OccurredAt { get; set; }
}

public sealed class InvoiceTransactionFormViewModel
{
    public Guid InvoiceId { get; set; }

    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "مبلغ تراکنش باید بیشتر از صفر باشد.")]
    [Display(Name = "مبلغ")]
    public decimal Amount { get; set; }

    [Display(Name = "روش پرداخت")]
    public PaymentMethod Method { get; set; } = PaymentMethod.OnlineGateway;

    [Display(Name = "وضعیت")]
    public TransactionStatus Status { get; set; } = TransactionStatus.Succeeded;

    [Required]
    [Display(Name = "شناسه مرجع")]
    public string Reference { get; set; } = string.Empty;

    [Display(Name = "درگاه پرداخت")]
    public string? GatewayName { get; set; }

    [Display(Name = "توضیحات")]
    public string? Description { get; set; }

    [Display(Name = "اطلاعات اضافی")]
    public string? Metadata { get; set; }

    [Display(Name = "تاریخ تراکنش")]
    public DateTime? OccurredAt { get; set; }

    [Display(Name = "تاریخ تراکنش (شمسی)")]
    public string? OccurredAtPersian { get; set; }
}

public sealed class ShipmentTrackingViewModel
{
    public Guid Id { get; set; }

    public Guid InvoiceItemId { get; set; }

    public string InvoiceItemName { get; set; } = string.Empty;

    public Guid? ProductId { get; set; }

    public string? ProductName { get; set; }

    public ShipmentStatus Status { get; set; }

    public string? TrackingNumber { get; set; }

    public string? Notes { get; set; }

    public DateTimeOffset StatusDate { get; set; }

    public string? UpdatedByName { get; set; }
}

public sealed class ShipmentTrackingFormViewModel
{
    public Guid? Id { get; set; }

    [Required]
    [Display(Name = "آیتم فاکتور")]
    public Guid InvoiceItemId { get; set; }

    [Required]
    [Display(Name = "وضعیت")]
    public ShipmentStatus Status { get; set; } = ShipmentStatus.Preparing;

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
