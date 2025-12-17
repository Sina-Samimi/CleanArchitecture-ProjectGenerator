using System;
using System.Globalization;
using System.IO;
using System.Linq;
using LogsDtoCloneTest.Application.DTOs.Billing;
using LogsDtoCloneTest.Domain.Enums;
using LogsDtoCloneTest.SharedKernel.Extensions;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace LogsDtoCloneTest.WebSite.App;

public static class InvoicePdfBuilder
{
    private static readonly object Sync = new();
    private static bool _questPdfInitialized;
    private static bool _fontRegistered;
    private static string? _fontFamilyName;

    public static IDocument Build(InvoiceDetailDto invoice, string fontPath)
    {
        ArgumentNullException.ThrowIfNull(invoice);
        EnsureQuestPdf(fontPath);

        var culture = CultureInfo.GetCultureInfo("fa-IR");

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Margin(30);
                page.Size(PageSizes.A4);
                page.PageColor(Colors.White);

                page.DefaultTextStyle(style =>
                {
                    var textStyle = style
                        .DirectionFromRightToLeft()
                        .FontSize(11);

                    if (!string.IsNullOrWhiteSpace(_fontFamilyName))
                    {
                        textStyle = textStyle.FontFamily(_fontFamilyName);
                    }

                    return textStyle;
                });

                page.Content().Column(column =>
                {
                    column.Spacing(18);

                    column.Item().Text($"فاکتور {invoice.InvoiceNumber}")
                        .SemiBold()
                        .FontSize(18)
                        .AlignCenter();

                    column.Item().Text(text =>
                    {
                        text.Span("عنوان: ").SemiBold();
                        text.Span(invoice.Title);
                    });

                    column.Item().Row(row =>
                    {
                        row.Spacing(12);

                        row.RelativeItem().Column(col =>
                        {
                            col.Spacing(6);
                            col.Item().Text(text =>
                            {
                                text.Span("شماره فاکتور: ").SemiBold();
                                text.Span(invoice.InvoiceNumber);
                            });

                            col.Item().Text(text =>
                            {
                                text.Span("تاریخ صدور: ").SemiBold();
                                text.Span(invoice.IssueDate.ToPersianDateTimeString());
                            });

                            col.Item().Text(text =>
                            {
                                text.Span("تاریخ سررسید: ").SemiBold();
                                text.Span(invoice.DueDate?.ToPersianDateTimeString() ?? "-");
                            });

                            col.Item().Text(text =>
                            {
                                text.Span("وضعیت: ").SemiBold();
                                text.Span(invoice.Status.GetDisplayName());
                            });

                            if (!string.IsNullOrWhiteSpace(invoice.UserId))
                            {
                                col.Item().Text(text =>
                                {
                                    text.Span("شناسه کاربر: ").SemiBold();
                                    text.Span(invoice.UserId);
                                });
                            }

                            if (!string.IsNullOrWhiteSpace(invoice.ExternalReference))
                            {
                                col.Item().Text(text =>
                                {
                                    text.Span("ارجاع خارجی: ").SemiBold();
                                    text.Span(invoice.ExternalReference);
                                });
                            }
                        });

                        row.RelativeItem().Column(col =>
                        {
                            col.Spacing(6);
                            col.Item().Text(text =>
                            {
                                text.Span("جمع اقلام: ").SemiBold();
                                text.Span(FormatMoney(invoice.Subtotal, culture));
                            });

                            col.Item().Text(text =>
                            {
                                text.Span("تخفیف: ").SemiBold();
                                text.Span(FormatMoney(invoice.DiscountTotal, culture));
                            });

                            col.Item().Text(text =>
                            {
                                text.Span("مالیات: ").SemiBold();
                                text.Span(FormatMoney(invoice.TaxAmount, culture));
                            });

                            col.Item().Text(text =>
                            {
                                text.Span("تعدیل: ").SemiBold();
                                text.Span(FormatMoney(invoice.AdjustmentAmount, culture));
                            });

                            col.Item().Text(text =>
                            {
                                text.Span("مبلغ نهایی: ").SemiBold();
                                text.Span(FormatMoney(invoice.GrandTotal, culture));
                            });

                            col.Item().Text(text =>
                            {
                                text.Span("دریافتی: ").SemiBold();
                                text.Span(FormatMoney(invoice.PaidAmount, culture));
                            });

                            col.Item().Text(text =>
                            {
                                text.Span("مانده: ").SemiBold();
                                text.Span(FormatMoney(invoice.OutstandingAmount, culture));
                            });
                        });
                    });

                    if (!string.IsNullOrWhiteSpace(invoice.Description))
                    {
                        column.Item().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(10).Text(text =>
                        {
                            text.Span("توضیحات: ").SemiBold();
                            text.Span(invoice.Description);
                        });
                    }

                    column.Item().Component(new InvoiceItemsTableComponent(invoice, culture));
                    column.Item().Component(new InvoiceTransactionsTableComponent(invoice, culture));
                });
            });
        });
    }

    private static string FormatMoney(decimal value, CultureInfo culture)
        => string.Format(culture, "{0:N0} تومان", value);

    private static void EnsureQuestPdf(string fontPath)
    {
        lock (Sync)
        {
            if (!_questPdfInitialized)
            {
                QuestPDF.Settings.License = LicenseType.Community;
                _questPdfInitialized = true;
            }

            if (_fontRegistered)
            {
                return;
            }

            if (!string.IsNullOrWhiteSpace(fontPath))
            {
                var absolutePath = Path.IsPathRooted(fontPath)
                    ? fontPath
                    : Path.Combine(Directory.GetCurrentDirectory(), fontPath);

                if (File.Exists(absolutePath))
                {
                    using var stream = File.OpenRead(absolutePath);
                    QuestPDF.Drawing.FontManager.RegisterFont(stream);
                    _fontFamilyName = "Vazirmatn";
                    _fontRegistered = true;
                    return;
                }
            }

            _fontFamilyName = null;
            _fontRegistered = true;
        }
    }

    private sealed class InvoiceItemsTableComponent : IComponent
    {
        private readonly InvoiceDetailDto _invoice;
        private readonly CultureInfo _culture;

        public InvoiceItemsTableComponent(InvoiceDetailDto invoice, CultureInfo culture)
        {
            _invoice = invoice;
            _culture = culture;
        }

        public void Compose(IContainer container)
        {
            container.Column(column =>
            {
                column.Spacing(8);
                column.Item().Text("آیتم‌های فاکتور").SemiBold().FontSize(14);

                if (_invoice.Items.Count == 0)
                {
                    column.Item().Text("هیچ آیتمی ثبت نشده است.").Italic().FontColor(Colors.Grey.Medium);
                    return;
                }

                column.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn();
                        columns.RelativeColumn(0.8f);
                        columns.RelativeColumn(0.6f);
                        columns.RelativeColumn(0.8f);
                        columns.RelativeColumn(0.8f);
                        columns.RelativeColumn();
                    });

                    table.Header(header =>
                    {
                        header.Cell().Background(Colors.Grey.Lighten3).Padding(6).Text("عنوان").SemiBold();
                        header.Cell().Background(Colors.Grey.Lighten3).Padding(6).Text("نوع").SemiBold();
                        header.Cell().Background(Colors.Grey.Lighten3).Padding(6).Text("تعداد").SemiBold();
                        header.Cell().Background(Colors.Grey.Lighten3).Padding(6).Text("قیمت واحد").SemiBold();
                        header.Cell().Background(Colors.Grey.Lighten3).Padding(6).Text("تخفیف").SemiBold();
                        header.Cell().Background(Colors.Grey.Lighten3).Padding(6).Text("جمع خط").SemiBold();
                    });

                    foreach (var item in _invoice.Items)
                    {
                        table.Cell().PaddingVertical(4).Text(text =>
                        {
                            text.Span(item.Name).SemiBold();
                            if (!string.IsNullOrWhiteSpace(item.Description))
                            {
                                text.Line("");
                                text.Span(item.Description!).FontSize(10).FontColor(Colors.Grey.Darken2);
                            }

                            if (item.Attributes.Any())
                            {
                                text.Line("");
                                text.Span(string.Join("، ",
                                    item.Attributes
                                        .Where(attribute => !string.IsNullOrWhiteSpace(attribute.Key) || !string.IsNullOrWhiteSpace(attribute.Value))
                                        .Select(attribute => $"{attribute.Key}: {attribute.Value}")))
                                    .FontSize(9)
                                    .FontColor(Colors.Grey.Darken1);
                            }
                        });
                        table.Cell().PaddingVertical(4).Text(item.ItemType.GetDisplayName());
                        table.Cell().PaddingVertical(4).Text(item.Quantity.ToString("0.##", _culture));
                        table.Cell().PaddingVertical(4).Text(FormatMoney(item.UnitPrice, _culture));
                        table.Cell().PaddingVertical(4).Text(FormatMoney(item.DiscountAmount ?? 0m, _culture));
                        table.Cell().PaddingVertical(4).Text(FormatMoney(item.Total, _culture));
                    }
                });
            });
        }
    }

    private sealed class InvoiceTransactionsTableComponent : IComponent
    {
        private readonly InvoiceDetailDto _invoice;
        private readonly CultureInfo _culture;

        public InvoiceTransactionsTableComponent(InvoiceDetailDto invoice, CultureInfo culture)
        {
            _invoice = invoice;
            _culture = culture;
        }

        public void Compose(IContainer container)
        {
            container.Column(column =>
            {
                column.Spacing(8);
                column.Item().Text("تراکنش‌ها").SemiBold().FontSize(14);

                if (_invoice.Transactions.Count == 0)
                {
                    column.Item().Text("تراکنشی ثبت نشده است.").Italic().FontColor(Colors.Grey.Medium);
                    return;
                }

                column.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(0.9f);
                        columns.RelativeColumn(0.9f);
                        columns.RelativeColumn(0.8f);
                        columns.RelativeColumn(0.8f);
                        columns.RelativeColumn();
                    });

                    table.Header(header =>
                    {
                        header.Cell().Background(Colors.Grey.Lighten3).Padding(6).Text("تاریخ").SemiBold();
                        header.Cell().Background(Colors.Grey.Lighten3).Padding(6).Text("مبلغ").SemiBold();
                        header.Cell().Background(Colors.Grey.Lighten3).Padding(6).Text("روش").SemiBold();
                        header.Cell().Background(Colors.Grey.Lighten3).Padding(6).Text("وضعیت").SemiBold();
                        header.Cell().Background(Colors.Grey.Lighten3).Padding(6).Text("شناسه مرجع / توضیحات").SemiBold();
                    });

                    foreach (var transaction in _invoice.Transactions.OrderByDescending(t => t.OccurredAt))
                    {
                        table.Cell().PaddingVertical(4).Text(transaction.OccurredAt.ToPersianDateTimeString());
                        table.Cell().PaddingVertical(4).Text(FormatMoney(transaction.Amount, _culture));
                        table.Cell().PaddingVertical(4).Text(transaction.Method.GetDisplayName());
                        table.Cell().PaddingVertical(4).Text(transaction.Status.GetDisplayName());
                        table.Cell().PaddingVertical(4).Text(text =>
                        {
                            text.Span(transaction.Reference).SemiBold();
                            if (!string.IsNullOrWhiteSpace(transaction.GatewayName))
                            {
                                text.Line("");
                                text.Span($"درگاه: {transaction.GatewayName}");
                            }

                            if (!string.IsNullOrWhiteSpace(transaction.Description))
                            {
                                text.Line("");
                                text.Span(transaction.Description!).FontColor(Colors.Grey.Darken1);
                            }

                            if (!string.IsNullOrWhiteSpace(transaction.Metadata))
                            {
                                text.Line("");
                                text.Span(transaction.Metadata!).FontColor(Colors.Grey.Darken1).FontSize(9);
                            }
                        });
                    }
                });
            });
        }
    }
}
