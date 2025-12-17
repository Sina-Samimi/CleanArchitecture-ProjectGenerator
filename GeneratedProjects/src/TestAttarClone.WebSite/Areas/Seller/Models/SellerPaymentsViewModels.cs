using System;
using System.Collections.Generic;

namespace TestAttarClone.WebSite.Areas.Seller.Models;

public sealed class SellerPaymentsViewModel
{
    public decimal TotalRevenue { get; set; }
    public decimal PaidRevenue { get; set; }
    public decimal PendingRevenue { get; set; }
    public int TotalInvoices { get; set; }
    public int PaidInvoices { get; set; }
    public int PendingInvoices { get; set; }
    public List<SellerInvoiceViewModel> Invoices { get; set; } = new();
    public DateTimeOffset GeneratedAt { get; set; }
}

public sealed class SellerInvoiceViewModel
{
    public Guid InvoiceId { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal PendingAmount { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTimeOffset IssueDate { get; set; }
    public DateTimeOffset? DueDate { get; set; }
    public int ProductCount { get; set; }
}

