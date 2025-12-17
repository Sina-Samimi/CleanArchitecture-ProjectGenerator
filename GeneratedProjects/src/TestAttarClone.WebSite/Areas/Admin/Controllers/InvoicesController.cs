using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TestAttarClone.Application.Commands.Billing;
using TestAttarClone.Application.Commands.Orders;
using TestAttarClone.Application.DTOs;
using TestAttarClone.Application.DTOs.Billing;
using TestAttarClone.Application.Queries.Billing;
using TestAttarClone.Application.Queries.Identity.GetUserLookups;
using TestAttarClone.Application.Queries.Identity.GetUsersByIds;
using TestAttarClone.Application.Queries.Orders;
using TestAttarClone.Domain.Enums;
using TestAttarClone.WebSite.Areas.Admin.Models;
using TestAttarClone.WebSite.App;
using MediatR;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Logging;
using QuestPDF.Fluent;

namespace TestAttarClone.WebSite.Areas.Admin.Controllers;

[Area("Admin")]
    public sealed class InvoicesController : Controller
    {
        private readonly IMediator _mediator;
        private readonly ILogger<InvoicesController> _logger;
        private readonly IWebHostEnvironment _environment;

        public InvoicesController(IMediator mediator, ILogger<InvoicesController> logger, IWebHostEnvironment environment)
        {
            _mediator = mediator;
            _logger = logger;
            _environment = environment;
        }

    [HttpGet]
    public async Task<IActionResult> Index([FromQuery] InvoiceFilterInput filter)
    {
        var cancellationToken = HttpContext.RequestAborted;

        var filterViewModel = new InvoiceFilterViewModel();
        var filterErrors = new List<string>();
        var filterDto = BuildInvoiceListFilter(filter, filterViewModel, filterErrors);

        var result = await _mediator.Send(new GetInvoiceListQuery(filterDto), cancellationToken);

        if (!result.IsSuccess)
        {
            TempData["Error"] = string.IsNullOrWhiteSpace(result.Error)
                ? "در بارگذاری فاکتورها خطایی رخ داد."
                : result.Error;

            var userOptionsOnFailure = await BuildUserSelectOptionsAsync(filterViewModel.UserId, cancellationToken);

            return View(new InvoiceIndexViewModel
            {
                Filter = filterViewModel,
                UserOptions = userOptionsOnFailure
            });
        }

        var normalizedFilterUserId = string.IsNullOrWhiteSpace(filterViewModel.UserId)
            ? null
            : filterViewModel.UserId.Trim();

        var listResult = result.Value;
        if (listResult is null)
        {
            TempData["Error"] ??= "در بارگذاری فاکتورها خطایی رخ داد.";

            var fallbackOptions = await BuildUserSelectOptionsAsync(filterViewModel.UserId, cancellationToken);

            return View(new InvoiceIndexViewModel
            {
                Filter = filterViewModel,
                UserOptions = fallbackOptions
            });
        }

        var userIds = listResult.Items
            .Select(item => item.UserId)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Select(id => id!.Trim())
            .ToList();

        if (normalizedFilterUserId is { Length: > 0 } userIdForList)
        {
            userIds.Add(userIdForList);
        }

        Dictionary<string, UserLookupDto> userLookup = await ResolveUserLookupAsync(userIds, cancellationToken);

        if (normalizedFilterUserId is { Length: > 0 } userId &&
            userLookup.TryGetValue(userId, out var selectedUser))
        {
            filterViewModel.UserDisplayName = selectedUser.DisplayName;
        }

        var userOptions = await BuildUserSelectOptionsAsync(normalizedFilterUserId, cancellationToken);

        if (filterErrors.Count > 0 && !TempData.ContainsKey("Error"))
        {
            TempData["Error"] = string.Join(" ", filterErrors);
        }

        var viewModel = MapToIndexViewModel(listResult, filterViewModel, userLookup, userOptions);
        ViewData["Title"] = "فاکتورها";
        ViewData["Subtitle"] = "مدیریت فاکتورهای مالی و وضعیت تراکنش‌ها";
        return View(viewModel);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var model = new InvoiceFormViewModel
        {
            IssueDate = DateTime.Now.Date,
            Currency = "IRT",
            Items =
            {
                new InvoiceItemFormViewModel
                {
                    Quantity = 1,
                    UnitPrice = 0,
                    ItemType = TestAttarClone.Domain.Enums.InvoiceItemType.Product,
                    Attributes = new List<InvoiceItemAttributeFormViewModel>()
                }
            }
        };

        model.IssueDatePersian = FormatPersianDate(model.IssueDate);
        model.DueDatePersian = string.Empty;

        await PopulateUserOptionsAsync(model, HttpContext.RequestAborted);

        ViewData["Title"] = "ثبت فاکتور جدید";
        ViewData["IsEdit"] = false;
        return View("Form", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(InvoiceFormViewModel model)
    {
        var cancellationToken = HttpContext.RequestAborted;
        await PopulateUserOptionsAsync(model, cancellationToken);
        ResolveDates(model);
        NormalizeInvoiceForm(model);
        NormalizeMonetaryFields(model);

        if (!ModelState.IsValid)
        {
            ViewData["Title"] = "ثبت فاکتور جدید";
            ViewData["IsEdit"] = false;
            return View("Form", model);
        }

        var command = MapToCreateCommand(model);
        var result = await _mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "در ثبت فاکتور خطایی رخ داد.");
            ViewData["Title"] = "ثبت فاکتور جدید";
            ViewData["IsEdit"] = false;
            return View("Form", model);
        }

        TempData["Success"] = "فاکتور جدید با موفقیت ثبت شد.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(Guid id)
    {
        if (id == Guid.Empty)
        {
            TempData["Error"] = "شناسه فاکتور معتبر نیست.";
            return RedirectToAction(nameof(Index));
        }

        var cancellationToken = HttpContext.RequestAborted;
        var detailsResult = await _mediator.Send(new GetInvoiceDetailsQuery(id), cancellationToken);

        if (!detailsResult.IsSuccess || detailsResult.Value is null)
        {
            TempData["Error"] = string.IsNullOrWhiteSpace(detailsResult.Error)
                ? "فاکتور مورد نظر یافت نشد."
                : detailsResult.Error;
            return RedirectToAction(nameof(Index));
        }

        var formModel = MapToFormViewModel(detailsResult.Value);
        await PopulateUserOptionsAsync(formModel, HttpContext.RequestAborted);
        ViewData["Title"] = "ویرایش فاکتور";
        ViewData["IsEdit"] = true;
        return View("Form", formModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(InvoiceFormViewModel model)
    {
        var cancellationToken = HttpContext.RequestAborted;
        
        // Ensure model is not null
        if (model is null)
        {
            TempData["Error"] = "خطا در بارگذاری اطلاعات فاکتور.";
            return RedirectToAction(nameof(Index));
        }
        
        // Ensure Id is set
        if (!model.Id.HasValue || model.Id == Guid.Empty)
        {
            ModelState.AddModelError(string.Empty, "شناسه فاکتور معتبر نیست.");
            await PopulateUserOptionsAsync(model, cancellationToken);
            ViewData["Title"] = "ویرایش فاکتور";
            ViewData["IsEdit"] = true;
            return View("Form", model);
        }
        
        await PopulateUserOptionsAsync(model, cancellationToken);
        
        // Normalize and validate form data
        ResolveDates(model);
        NormalizeInvoiceForm(model);
        NormalizeMonetaryFields(model);

        // Check if there are any validation errors
        if (!ModelState.IsValid)
        {
            var aggregatedErrors = ModelState
                .Where(ms => ms.Value?.Errors.Count > 0)
                .SelectMany(ms => ms.Value!.Errors.Select(error => $"{ms.Key}:{error.ErrorMessage}"))
                .Take(10)
                .ToArray();

            // Ensure we have at least one error message if ModelState is invalid but no general errors
            if (!ModelState.ContainsKey(string.Empty))
            {
                var detailedErrors = ModelState
                    .Where(ms => ms.Value?.Errors.Count > 0)
                    .SelectMany(ms => ms.Value!.Errors.Select(e => e.ErrorMessage))
                    .ToList();
                
                if (detailedErrors.Count > 0)
                {
                    ModelState.AddModelError(string.Empty, "لطفاً خطاهای فرم را برطرف کنید: " + string.Join(" ", detailedErrors.Take(3)));
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "اطلاعات فرم معتبر نیست. لطفاً تمام فیلدهای الزامی را پر کنید.");
                }
            }
            
            ViewData["Title"] = "ویرایش فاکتور";
            ViewData["IsEdit"] = true;
            if (aggregatedErrors.Length > 0)
            {
                _logger.LogWarning(
                    "Invoice edit validation failed for InvoiceId={InvoiceId}. Errors: {Errors}",
                    model.Id,
                    string.Join(" | ", aggregatedErrors));
            }
            return View("Form", model);
        }

        // Ensure we have valid items
        if (model.Items is null || model.Items.Count == 0 || model.Items.All(item => string.IsNullOrWhiteSpace(item.Name)))
        {
            ModelState.AddModelError(string.Empty, "حداقل یک آیتم با نام معتبر برای فاکتور لازم است.");
            _logger.LogWarning(
                "Invoice edit failed: no valid items provided for InvoiceId={InvoiceId}.",
                model.Id);
            ViewData["Title"] = "ویرایش فاکتور";
            ViewData["IsEdit"] = true;
            return View("Form", model);
        }

        var command = MapToUpdateCommand(model);
        var result = await _mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "در به‌روزرسانی فاکتور خطایی رخ داد.");
            _logger.LogError(
                "Invoice update failed for InvoiceId={InvoiceId}. Error={Error}",
                model.Id,
                result.Error ?? "Unknown error");
            ViewData["Title"] = "ویرایش فاکتور";
            ViewData["IsEdit"] = true;
            return View("Form", model);
        }

        TempData["Success"] = "فاکتور با موفقیت به‌روزرسانی شد.";
        return RedirectToAction(nameof(Details), new { id = model.Id });
    }

    [HttpGet]
    public async Task<IActionResult> DownloadPdf(Guid id)
    {
        if (id == Guid.Empty)
        {
            TempData["Error"] = "شناسه فاکتور معتبر نیست.";
            return RedirectToAction(nameof(Index));
        }

        var cancellationToken = HttpContext.RequestAborted;
        var detailsResult = await _mediator.Send(new GetInvoiceDetailsQuery(id), cancellationToken);

        if (!detailsResult.IsSuccess || detailsResult.Value is null)
        {
            TempData["Error"] = string.IsNullOrWhiteSpace(detailsResult.Error)
                ? "فاکتور مورد نظر یافت نشد."
                : detailsResult.Error;
            return RedirectToAction(nameof(Index));
        }

        var detail = detailsResult.Value;
        var fontsRoot = _environment.WebRootPath ?? Path.Combine(_environment.ContentRootPath, "wwwroot");
        var fontPath = Path.Combine(fontsRoot, "fonts", "Vazirmatn-Regular.ttf");
        var document = InvoicePdfBuilder.Build(detail, fontPath);
        var pdfBytes = document.GeneratePdf();

        var rawFileName = string.IsNullOrWhiteSpace(detail.InvoiceNumber)
            ? $"invoice-{detail.Id:N}"
            : detail.InvoiceNumber;

        var safeFileName = string.Concat(rawFileName
            .Where(ch => !Path.GetInvalidFileNameChars().Contains(ch)))
            .Replace(" ", "-");

        if (string.IsNullOrWhiteSpace(safeFileName))
        {
            safeFileName = $"invoice-{detail.Id:N}";
        }

        return File(pdfBytes, "application/pdf", $"{safeFileName}.pdf");
    }

    [HttpGet]
    public async Task<IActionResult> Details(Guid id)
    {
        if (id == Guid.Empty)
        {
            TempData["Error"] = "شناسه فاکتور معتبر نیست.";
            return RedirectToAction(nameof(Index));
        }

        var cancellationToken = HttpContext.RequestAborted;
        var result = await _mediator.Send(new GetInvoiceDetailsQuery(id), cancellationToken);

        if (!result.IsSuccess || result.Value is null)
        {
            TempData["Error"] = string.IsNullOrWhiteSpace(result.Error)
                ? "فاکتور مورد نظر یافت نشد."
                : result.Error;
            return RedirectToAction(nameof(Index));
        }

        var model = MapToDetailViewModel(result.Value);
        
        // Load user information if UserId exists
        if (!string.IsNullOrWhiteSpace(model.UserId))
        {
            var userLookup = await ResolveUserLookupAsync(new[] { model.UserId }, cancellationToken);
            if (userLookup.TryGetValue(model.UserId, out var user))
            {
                model.UserDisplayName = user.DisplayName;
                model.UserPhoneNumber = user.PhoneNumber;
            }
        }
        
        // Load shipment trackings
        var trackingsResult = await _mediator.Send(new GetShipmentTrackingsByInvoiceQuery(id), cancellationToken);
        if (trackingsResult.IsSuccess && trackingsResult.Value is not null)
        {
            model.ShipmentTrackings = trackingsResult.Value.Trackings.Select(t => new ShipmentTrackingViewModel
            {
                Id = t.Id,
                InvoiceItemId = t.InvoiceItemId,
                InvoiceItemName = t.InvoiceItemName,
                ProductId = t.ProductId,
                ProductName = t.ProductName,
                Status = t.Status,
                TrackingNumber = t.TrackingNumber,
                Notes = t.Notes,
                StatusDate = t.StatusDate,
                UpdatedByName = t.UpdatedByName
            }).ToArray();
        }
        
        ViewData["Title"] = $"فاکتور {model.InvoiceNumber}";
        ViewData["Subtitle"] = "جزئیات فاکتور و تاریخچه پرداخت‌ها";
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(Guid id)
    {
        if (id == Guid.Empty)
        {
            TempData["Error"] = "شناسه فاکتور معتبر نیست.";
            return RedirectToAction(nameof(Index));
        }

        var cancellationToken = HttpContext.RequestAborted;
        var result = await _mediator.Send(new CancelInvoiceCommand(id), cancellationToken);

        TempData[result.IsSuccess ? "Success" : "Error"] = result.IsSuccess
            ? "فاکتور با موفقیت لغو شد."
            : result.Error ?? "در لغو فاکتور خطایی رخ داد.";

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reopen(Guid id)
    {
        if (id == Guid.Empty)
        {
            TempData["Error"] = "شناسه فاکتور معتبر نیست.";
            return RedirectToAction(nameof(Index));
        }

        var cancellationToken = HttpContext.RequestAborted;
        var result = await _mediator.Send(new ReopenInvoiceCommand(id), cancellationToken);

        TempData[result.IsSuccess ? "Success" : "Error"] = result.IsSuccess
            ? "فاکتور مجدداً فعال شد."
            : result.Error ?? "در فعال‌سازی مجدد فاکتور خطایی رخ داد.";

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RecordTransaction([Bind(Prefix = nameof(InvoiceDetailViewModel.NewTransaction))] InvoiceTransactionFormViewModel model)
    {
        if (model.InvoiceId == Guid.Empty)
        {
            TempData["Error"] = "شناسه فاکتور معتبر نیست.";
            return RedirectToAction(nameof(Index));
        }

        ResolveTransactionDate(model);

        if (!ModelState.IsValid)
        {
            TempData["Error"] = "اطلاعات تراکنش کامل نیست.";
            return RedirectToAction(nameof(Details), new { id = model.InvoiceId });
        }

        var occurredAt = model.OccurredAt is null
            ? (DateTimeOffset?)null
            : new DateTimeOffset(DateTime.SpecifyKind(model.OccurredAt.Value, DateTimeKind.Utc));

        var command = new RecordInvoiceTransactionCommand(
            model.InvoiceId,
            model.Amount,
            model.Method,
            model.Status,
            model.Reference,
            model.GatewayName,
            model.Description,
            model.Metadata,
            occurredAt);

        var cancellationToken = HttpContext.RequestAborted;
        var result = await _mediator.Send(command, cancellationToken);

        TempData[result.IsSuccess ? "Success" : "Error"] = result.IsSuccess
            ? "تراکنش با موفقیت ثبت شد."
            : result.Error ?? "در ثبت تراکنش خطایی رخ داد.";

        return RedirectToAction(nameof(Details), new { id = model.InvoiceId });
    }

    private InvoiceListFilterDto BuildInvoiceListFilter(
        InvoiceFilterInput filter,
        InvoiceFilterViewModel viewModel,
        List<string> errors)
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        ArgumentNullException.ThrowIfNull(errors);

        var searchTerm = string.IsNullOrWhiteSpace(filter?.SearchTerm)
            ? null
            : filter!.SearchTerm!.Trim();
        viewModel.SearchTerm = searchTerm;
        viewModel.Status = filter?.Status;

        var userId = string.IsNullOrWhiteSpace(filter?.UserId)
            ? null
            : filter!.UserId!.Trim();
        viewModel.UserId = userId;

        var issueDateFromSanitized = SanitizeDateInput(filter?.IssueDateFrom);
        var issueDateFromNormalized = NormalizePersianDateInput(filter?.IssueDateFrom);
        viewModel.IssueDateFrom = string.IsNullOrEmpty(issueDateFromNormalized)
            ? (string.IsNullOrEmpty(issueDateFromSanitized) ? null : issueDateFromSanitized)
            : issueDateFromNormalized;
        viewModel.IssueDateFromDisplay = string.IsNullOrEmpty(issueDateFromSanitized)
            ? null
            : issueDateFromSanitized;

        DateTimeOffset? issueDateFrom = null;
        if (!string.IsNullOrEmpty(issueDateFromNormalized))
        {
            if (TryCreateGregorianDate(issueDateFromNormalized, out var parsedFrom, out var fromError))
            {
                var utcFrom = DateTime.SpecifyKind(parsedFrom, DateTimeKind.Utc);
                issueDateFrom = new DateTimeOffset(utcFrom);
            }
            else if (!string.IsNullOrEmpty(fromError))
            {
                errors.Add(fromError);
            }
        }

        var issueDateToSanitized = SanitizeDateInput(filter?.IssueDateTo);
        var issueDateToNormalized = NormalizePersianDateInput(filter?.IssueDateTo);
        viewModel.IssueDateTo = string.IsNullOrEmpty(issueDateToNormalized)
            ? (string.IsNullOrEmpty(issueDateToSanitized) ? null : issueDateToSanitized)
            : issueDateToNormalized;
        viewModel.IssueDateToDisplay = string.IsNullOrEmpty(issueDateToSanitized)
            ? null
            : issueDateToSanitized;

        DateTimeOffset? issueDateTo = null;
        if (!string.IsNullOrEmpty(issueDateToNormalized))
        {
            if (TryCreateGregorianDate(issueDateToNormalized, out var parsedTo, out var toError))
            {
                var utcTo = DateTime.SpecifyKind(parsedTo, DateTimeKind.Utc).AddDays(1);
                issueDateTo = new DateTimeOffset(utcTo);
            }
            else if (!string.IsNullOrEmpty(toError))
            {
                errors.Add(toError);
            }
        }

        return new InvoiceListFilterDto(searchTerm, userId, filter?.Status, issueDateFrom, issueDateTo);
    }

    private async Task<Dictionary<string, UserLookupDto>> ResolveUserLookupAsync(
        IEnumerable<string?> userIds,
        CancellationToken cancellationToken)
    {
        if (userIds is null)
        {
            return new Dictionary<string, UserLookupDto>(StringComparer.Ordinal);
        }

        var normalizedIds = userIds
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Select(id => id!.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        if (normalizedIds.Length == 0)
        {
            return new Dictionary<string, UserLookupDto>(StringComparer.Ordinal);
        }

        var lookupResult = await _mediator.Send(new GetUsersByIdsQuery(normalizedIds), cancellationToken);

        if (!lookupResult.IsSuccess || lookupResult.Value is null)
        {
            return new Dictionary<string, UserLookupDto>(StringComparer.Ordinal);
        }

        return new Dictionary<string, UserLookupDto>(lookupResult.Value, StringComparer.Ordinal);
    }

    private async Task<IReadOnlyCollection<SelectListItem>> BuildUserSelectOptionsAsync(
        string? selectedUserId,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetUserLookupsQuery(), cancellationToken);

        if (!result.IsSuccess || result.Value is null)
        {
            if (string.IsNullOrWhiteSpace(selectedUserId))
            {
                return Array.Empty<SelectListItem>();
            }

            return new[]
            {
                new SelectListItem
                {
                    Value = selectedUserId,
                    Text = $"{selectedUserId} (کاربر یافت نشد یا غیرفعال است)",
                    Selected = true
                }
            };
        }

        var options = new List<SelectListItem>(result.Value.Count);

        foreach (var userLookup in result.Value)
        {
            var text = userLookup.DisplayName;

            if (!string.IsNullOrWhiteSpace(userLookup.Email) &&
                !string.Equals(userLookup.Email, userLookup.DisplayName, StringComparison.OrdinalIgnoreCase))
            {
                text = $"{userLookup.DisplayName} ({userLookup.Email})";
            }

            if (!userLookup.IsActive)
            {
                text = $"{text} (غیرفعال)";
            }

            options.Add(new SelectListItem
            {
                Value = userLookup.Id,
                Text = text,
                Selected = !string.IsNullOrWhiteSpace(selectedUserId) &&
                          string.Equals(userLookup.Id, selectedUserId, StringComparison.Ordinal)
            });
        }

        if (!string.IsNullOrWhiteSpace(selectedUserId) &&
            options.All(option => !string.Equals(option.Value, selectedUserId, StringComparison.Ordinal)))
        {
            options.Insert(0, new SelectListItem
            {
                Value = selectedUserId,
                Text = $"{selectedUserId} (کاربر یافت نشد یا غیرفعال است)",
                Selected = true
            });
        }

        return options;
    }

    private static InvoiceIndexViewModel MapToIndexViewModel(
        InvoiceListResultDto data,
        InvoiceFilterViewModel filter,
        IReadOnlyDictionary<string, UserLookupDto> userLookup,
        IReadOnlyCollection<SelectListItem> userOptions)
    {
        var summary = data.Summary ?? new InvoiceSummaryMetricsDto(0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
        var lookup = userLookup ?? new Dictionary<string, UserLookupDto>(StringComparer.Ordinal);

        var viewModel = new InvoiceIndexViewModel
        {
            GeneratedAt = data.GeneratedAt,
            Summary = new InvoiceSummaryViewModel
            {
                TotalInvoices = summary.TotalInvoices,
                DraftInvoices = summary.DraftInvoices,
                PendingInvoices = summary.PendingInvoices,
                PaidInvoices = summary.PaidInvoices,
                PartiallyPaidInvoices = summary.PartiallyPaidInvoices,
                CancelledInvoices = summary.CancelledInvoices,
                OverdueInvoices = summary.OverdueInvoices,
                TotalBilledAmount = summary.TotalBilledAmount,
                TotalOutstandingAmount = summary.TotalOutstandingAmount,
                TotalCollectedAmount = summary.TotalCollectedAmount
            },
            Invoices = data.Items
                .Select(item =>
                {
                    UserLookupDto? user = null;
                    if (!string.IsNullOrWhiteSpace(item.UserId))
                    {
                        lookup.TryGetValue(item.UserId, out user);
                    }

                    return new InvoiceListItemViewModel
                    {
                        Id = item.Id,
                        InvoiceNumber = item.InvoiceNumber,
                        Title = item.Title,
                        Status = item.Status,
                        Currency = item.Currency,
                        GrandTotal = item.GrandTotal,
                        PaidAmount = item.PaidAmount,
                        OutstandingAmount = item.OutstandingAmount,
                        IssueDate = item.IssueDate,
                        DueDate = item.DueDate,
                        UserId = item.UserId,
                        UserDisplayName = user?.DisplayName,
                        ExternalReference = item.ExternalReference
                    };
                })
                .ToArray(),
            Filter = new InvoiceFilterViewModel
            {
                SearchTerm = filter.SearchTerm,
                Status = filter.Status,
                UserId = filter.UserId,
                UserDisplayName = filter.UserDisplayName,
                IssueDateFrom = filter.IssueDateFrom,
                IssueDateTo = filter.IssueDateTo,
                IssueDateFromDisplay = filter.IssueDateFromDisplay,
                IssueDateToDisplay = filter.IssueDateToDisplay
            },
            UserOptions = userOptions ?? Array.Empty<SelectListItem>()
        };

        return viewModel;
    }

    private static InvoiceFormViewModel MapToFormViewModel(InvoiceDetailDto details)
    {
        var itemsTotal = Math.Max(0, details.Subtotal - details.DiscountTotal);
        var taxRate = itemsTotal <= 0
            ? 0m
            : decimal.Round(details.TaxAmount / itemsTotal * 100m, 2, MidpointRounding.AwayFromZero);

        var model = new InvoiceFormViewModel
        {
            Id = details.Id,
            InvoiceNumber = details.InvoiceNumber,
            Title = details.Title,
            Description = details.Description,
            Currency = details.Currency,
            UserId = details.UserId,
            ExternalReference = details.ExternalReference,
            IssueDate = details.IssueDate.LocalDateTime.Date,
            DueDate = details.DueDate?.LocalDateTime.Date,
            IssueDatePersian = FormatPersianDate(details.IssueDate.LocalDateTime.Date),
            DueDatePersian = details.DueDate is null
                ? string.Empty
                : FormatPersianDate(details.DueDate.Value.LocalDateTime.Date),
            TaxRatePercent = taxRate,
            TaxAmount = details.TaxAmount,
            AdjustmentAmount = details.AdjustmentAmount,
            Items = details.Items
                .Select(item => new InvoiceItemFormViewModel
                {
                    Id = item.Id,
                    Name = item.Name,
                    Description = item.Description,
                    ItemType = item.ItemType,
                    ReferenceId = item.ReferenceId,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice,
                    DiscountAmount = item.DiscountAmount,
                    Attributes = item.Attributes
                        .Select(attribute => new InvoiceItemAttributeFormViewModel
                        {
                            Id = attribute.Id,
                            Key = attribute.Key,
                            Value = attribute.Value
                        })
                        .ToList()
                })
                .DefaultIfEmpty(new InvoiceItemFormViewModel
                {
                    Quantity = 1,
                    UnitPrice = 0,
                    Attributes = new List<InvoiceItemAttributeFormViewModel>()
                })
                .ToList()
        };

        return model;
    }

    private static InvoiceDetailViewModel MapToDetailViewModel(InvoiceDetailDto details)
    {
        var culture = new CultureInfo("fa-IR");
        _ = culture; // kept for future formatting usage if needed

        var itemsTotal = Math.Max(0, details.Subtotal - details.DiscountTotal);
        var taxRate = itemsTotal <= 0
            ? 0m
            : decimal.Round(details.TaxAmount / itemsTotal * 100m, 2, MidpointRounding.AwayFromZero);

        return new InvoiceDetailViewModel
        {
            Id = details.Id,
            InvoiceNumber = details.InvoiceNumber,
            Title = details.Title,
            Description = details.Description,
            Status = details.Status,
            Currency = details.Currency,
            Subtotal = details.Subtotal,
            DiscountTotal = details.DiscountTotal,
            TaxAmount = details.TaxAmount,
            AdjustmentAmount = details.AdjustmentAmount,
            GrandTotal = details.GrandTotal,
            TaxRatePercent = taxRate,
            PaidAmount = details.PaidAmount,
            OutstandingAmount = details.OutstandingAmount,
            IssueDate = details.IssueDate,
            DueDate = details.DueDate,
            UserId = details.UserId,
            ExternalReference = details.ExternalReference,
            ShippingAddressId = details.ShippingAddressId,
            ShippingRecipientName = details.ShippingRecipientName,
            ShippingRecipientPhone = details.ShippingRecipientPhone,
            ShippingProvince = details.ShippingProvince,
            ShippingCity = details.ShippingCity,
            ShippingPostalCode = details.ShippingPostalCode,
            ShippingAddressLine = details.ShippingAddressLine,
            ShippingPlaque = details.ShippingPlaque,
            ShippingUnit = details.ShippingUnit,
            Items = details.Items.Select(item => new InvoiceItemDetailViewModel
            {
                Id = item.Id,
                Name = item.Name,
                Description = item.Description,
                ItemType = item.ItemType,
                ReferenceId = item.ReferenceId,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                DiscountAmount = item.DiscountAmount,
                Subtotal = item.Subtotal,
                Total = item.Total,
                Attributes = item.Attributes
                    .Select(attribute => new InvoiceItemAttributeFormViewModel
                    {
                        Id = attribute.Id,
                        Key = attribute.Key,
                        Value = attribute.Value
                    })
                    .ToArray()
            }).ToArray(),
            Transactions = details.Transactions.Select(transaction => new InvoiceTransactionViewModel
            {
                Id = transaction.Id,
                Amount = transaction.Amount,
                Method = transaction.Method,
                Status = transaction.Status,
                Reference = transaction.Reference,
                GatewayName = transaction.GatewayName,
                Description = transaction.Description,
                Metadata = transaction.Metadata,
                OccurredAt = transaction.OccurredAt
            }).ToArray(),
            NewTransaction = new InvoiceTransactionFormViewModel
            {
                InvoiceId = details.Id,
                Method = PaymentMethod.OnlineGateway,
                Status = TransactionStatus.Succeeded,
                Amount = details.OutstandingAmount > 0 ? details.OutstandingAmount : 0,
                OccurredAtPersian = FormatPersianDate(DateTime.Now.Date)
            }
        };
    }

    private static CreateInvoiceCommand MapToCreateCommand(InvoiceFormViewModel model)
    {
        var issueDate = new DateTimeOffset(DateTime.SpecifyKind(model.IssueDate, DateTimeKind.Utc));
        var dueDate = model.DueDate is null ? (DateTimeOffset?)null : new DateTimeOffset(DateTime.SpecifyKind(model.DueDate.Value, DateTimeKind.Utc));

        var items = model.Items.Select(item => new CreateInvoiceCommand.Item(
            item.Name,
            item.Description,
            item.ItemType,
            item.ReferenceId,
            item.Quantity,
            item.UnitPrice,
            item.DiscountAmount,
            item.Attributes
                .Where(attribute => !string.IsNullOrWhiteSpace(attribute.Key))
                .Select(attribute => new CreateInvoiceCommand.Attribute(attribute.Key, attribute.Value))
                .ToArray()))
            .ToArray();

        return new CreateInvoiceCommand(
            model.InvoiceNumber,
            model.Title,
            model.Description,
            model.Currency,
            model.UserId,
            issueDate,
            dueDate,
            model.TaxAmount,
            model.AdjustmentAmount,
            model.ExternalReference,
            items);
    }

    private static UpdateInvoiceCommand MapToUpdateCommand(InvoiceFormViewModel model)
    {
        var issueDate = new DateTimeOffset(DateTime.SpecifyKind(model.IssueDate, DateTimeKind.Utc));
        var dueDate = model.DueDate is null ? (DateTimeOffset?)null : new DateTimeOffset(DateTime.SpecifyKind(model.DueDate.Value, DateTimeKind.Utc));

        var items = model.Items.Select(item => new UpdateInvoiceCommand.Item(
            item.Id,
            item.Name,
            item.Description,
            item.ItemType,
            item.ReferenceId,
            item.Quantity,
            item.UnitPrice,
            item.DiscountAmount,
            item.Attributes
                .Where(attribute => !string.IsNullOrWhiteSpace(attribute.Key))
                .Select(attribute => new UpdateInvoiceCommand.Attribute(attribute.Id, attribute.Key, attribute.Value))
                .ToArray()))
            .ToArray();

        return new UpdateInvoiceCommand(
            model.Id!.Value,
            model.InvoiceNumber ?? string.Empty,
            model.Title,
            model.Description,
            model.Currency,
            model.UserId,
            issueDate,
            dueDate,
            model.TaxAmount,
            model.AdjustmentAmount,
            model.ExternalReference,
            items);
    }

    private static void NormalizeInvoiceForm(InvoiceFormViewModel model)
    {
        if (model.Items is null || model.Items.Count == 0)
        {
            model.Items = new List<InvoiceItemFormViewModel>
            {
                new()
                {
                    Quantity = 1,
                    UnitPrice = 0,
                    Attributes = new List<InvoiceItemAttributeFormViewModel>()
                }
            };
            return;
        }

        var validItems = model.Items
            .Where(item => !string.IsNullOrWhiteSpace(item.Name))
            .Select(item =>
            {
                item.Attributes ??= new List<InvoiceItemAttributeFormViewModel>();
                return item;
            })
            .ToList();

        if (validItems.Count == 0)
        {
            validItems.Add(new InvoiceItemFormViewModel
            {
                Quantity = 1,
                UnitPrice = 0,
                Attributes = new List<InvoiceItemAttributeFormViewModel>()
            });
        }

        model.Items = validItems;
    }

    private void NormalizeMonetaryFields(InvoiceFormViewModel model)
    {
        if (model is null)
        {
            return;
        }

        if (model.TaxRatePercent < 0)
        {
            model.TaxRatePercent = 0;
        }
        else if (model.TaxRatePercent > 100)
        {
            model.TaxRatePercent = 100;
        }

        model.TaxRatePercent = decimal.Round(model.TaxRatePercent, 2, MidpointRounding.AwayFromZero);
        model.AdjustmentAmount = decimal.Round(model.AdjustmentAmount, 2, MidpointRounding.AwayFromZero);

        model.TaxAmount = CalculateTaxAmountFromPercent(model);

        if (ModelState.ContainsKey(nameof(InvoiceFormViewModel.TaxAmount)))
        {
            ModelState.Remove(nameof(InvoiceFormViewModel.TaxAmount));
        }
    }

    private async Task PopulateUserOptionsAsync(InvoiceFormViewModel model, CancellationToken cancellationToken)
    {
        if (model is null)
        {
            return;
        }

        model.UserOptions = await BuildUserSelectOptionsAsync(model.UserId, cancellationToken);
    }

    public sealed class InvoiceFilterInput
    {
        public string? SearchTerm { get; set; }

        public InvoiceStatus? Status { get; set; }

        public string? UserId { get; set; }

        public string? IssueDateFrom { get; set; }

        public string? IssueDateTo { get; set; }
    }

    private void ResolveDates(InvoiceFormViewModel model)
    {
        if (model is null)
        {
            return;
        }

        var issueSanitized = SanitizeDateInput(model.IssueDatePersian);
        var issueNormalized = NormalizePersianDateInput(model.IssueDatePersian);
        model.IssueDatePersian = string.IsNullOrEmpty(issueNormalized) ? issueSanitized : issueNormalized;

        if (string.IsNullOrEmpty(issueNormalized))
        {
            var message = string.IsNullOrWhiteSpace(issueSanitized)
                ? "تاریخ صدور فاکتور را وارد کنید."
                : "تاریخ صدور وارد شده معتبر نیست.";
            ModelState.AddModelError(nameof(InvoiceFormViewModel.IssueDatePersian), message);
        }
        else if (TryCreateGregorianDate(issueNormalized, out var issueDate, out var issueError))
        {
            model.IssueDate = issueDate;
        }
        else
        {
            ModelState.AddModelError(nameof(InvoiceFormViewModel.IssueDatePersian), issueError ?? "تاریخ صدور وارد شده معتبر نیست.");
        }

        var dueSanitized = SanitizeDateInput(model.DueDatePersian);
        var dueNormalized = NormalizePersianDateInput(model.DueDatePersian);

        if (string.IsNullOrWhiteSpace(dueSanitized))
        {
            model.DueDatePersian = string.Empty;
            model.DueDate = null;
            return;
        }

        model.DueDatePersian = string.IsNullOrEmpty(dueNormalized) ? dueSanitized : dueNormalized;

        if (string.IsNullOrEmpty(dueNormalized))
        {
            ModelState.AddModelError(nameof(InvoiceFormViewModel.DueDatePersian), "تاریخ سررسید وارد شده معتبر نیست.");
        }
        else if (TryCreateGregorianDate(dueNormalized, out var dueDate, out var dueError))
        {
            model.DueDate = dueDate;
        }
        else
        {
            ModelState.AddModelError(nameof(InvoiceFormViewModel.DueDatePersian), dueError ?? "تاریخ سررسید وارد شده معتبر نیست.");
        }
    }

    private void ResolveTransactionDate(InvoiceTransactionFormViewModel model)
    {
        if (model is null)
        {
            return;
        }

        var sanitized = SanitizeDateInput(model.OccurredAtPersian);
        var normalized = NormalizePersianDateInput(model.OccurredAtPersian);

        if (string.IsNullOrWhiteSpace(sanitized))
        {
            model.OccurredAtPersian = string.Empty;
            model.OccurredAt = null;
            return;
        }

        model.OccurredAtPersian = string.IsNullOrEmpty(normalized) ? sanitized : normalized;

        if (string.IsNullOrEmpty(normalized))
        {
            ModelState.AddModelError(nameof(InvoiceTransactionFormViewModel.OccurredAtPersian), "تاریخ تراکنش وارد شده معتبر نیست.");
            return;
        }

        if (TryCreateGregorianDate(normalized, out var occurredAt, out var occurredError))
        {
            model.OccurredAt = occurredAt;
        }
        else
        {
            ModelState.AddModelError(nameof(InvoiceTransactionFormViewModel.OccurredAtPersian), occurredError ?? "تاریخ تراکنش وارد شده معتبر نیست.");
        }
    }

    private static decimal CalculateTaxAmountFromPercent(InvoiceFormViewModel model)
    {
        var totals = ComputeItemTotals(model);

        if (totals.ItemsTotal <= 0 || model.TaxRatePercent <= 0)
        {
            return 0m;
        }

        var tax = totals.ItemsTotal * (model.TaxRatePercent / 100m);
        return decimal.Round(tax, 2, MidpointRounding.AwayFromZero);
    }

    private static InvoiceItemTotals ComputeItemTotals(InvoiceFormViewModel model)
    {
        if (model?.Items is null || model.Items.Count == 0)
        {
            return new InvoiceItemTotals(0m, 0m, 0m);
        }

        decimal subtotal = 0m;
        decimal discount = 0m;

        foreach (var item in model.Items)
        {
            var quantity = item.Quantity < 0 ? 0 : item.Quantity;
            var unitPrice = item.UnitPrice < 0 ? 0 : item.UnitPrice;
            var lineSubtotal = quantity * unitPrice;
            subtotal += lineSubtotal;

            var itemDiscount = item.DiscountAmount ?? 0m;
            if (itemDiscount < 0)
            {
                itemDiscount = 0;
            }

            if (itemDiscount > lineSubtotal)
            {
                itemDiscount = lineSubtotal;
            }

            discount += itemDiscount;
        }

        subtotal = decimal.Round(subtotal, 2, MidpointRounding.AwayFromZero);
        discount = decimal.Round(discount, 2, MidpointRounding.AwayFromZero);

        var itemsTotal = subtotal - discount;
        if (itemsTotal < 0)
        {
            itemsTotal = 0;
        }

        itemsTotal = decimal.Round(itemsTotal, 2, MidpointRounding.AwayFromZero);

        return new InvoiceItemTotals(subtotal, discount, itemsTotal);
    }

    private readonly record struct InvoiceItemTotals(decimal Subtotal, decimal Discount, decimal ItemsTotal);

    private static string NormalizePersianDateInput(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var normalizedDigits = NormalizeDigits(value)
            .Replace("\u200f", string.Empty, StringComparison.Ordinal)
            .Replace("\u200e", string.Empty, StringComparison.Ordinal)
            .Replace(".", "/", StringComparison.Ordinal)
            .Replace("-", "/", StringComparison.Ordinal)
            .Replace("\\", "/", StringComparison.Ordinal)
            .Replace(" ", string.Empty, StringComparison.Ordinal);

        var parts = normalizedDigits.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 3)
        {
            return string.Empty;
        }

        var year = parts[0].PadLeft(4, '0');
        var month = parts[1].PadLeft(2, '0');
        var day = parts[2].PadLeft(2, '0');

        return string.Create(10, (year, month, day), static (span, state) =>
        {
            var (y, m, d) = state;
            y.AsSpan().CopyTo(span);
            span[4] = '-';
            m.AsSpan().CopyTo(span[5..]);
            span[7] = '-';
            d.AsSpan().CopyTo(span[8..]);
        });
    }

    private static string SanitizeDateInput(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return NormalizeDigits(value)
            .Replace("\u200f", string.Empty, StringComparison.Ordinal)
            .Replace("\u200e", string.Empty, StringComparison.Ordinal)
            .Replace(".", "/", StringComparison.Ordinal)
            .Replace("-", "/", StringComparison.Ordinal)
            .Replace("\\", "/", StringComparison.Ordinal)
            .Replace(" ", string.Empty, StringComparison.Ordinal);
    }

    private static string NormalizeDigits(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        var builder = new StringBuilder(value.Length);
        foreach (var character in value)
        {
            builder.Append(character switch
            {
                '۰' => '0',
                '۱' => '1',
                '۲' => '2',
                '۳' => '3',
                '۴' => '4',
                '۵' => '5',
                '۶' => '6',
                '۷' => '7',
                '۸' => '8',
                '۹' => '9',
                '٠' => '0',
                '١' => '1',
                '٢' => '2',
                '٣' => '3',
                '٤' => '4',
                '٥' => '5',
                '٦' => '6',
                '٧' => '7',
                '٨' => '8',
                '٩' => '9',
                _ => character
            });
        }

        return builder.ToString();
    }

    private static bool TryCreateGregorianDate(string value, out DateTime result, out string? error)
    {
        result = default;
        error = null;

        if (!TryExtractPersianDateParts(value, out var year, out var month, out var day))
        {
            error = "تاریخ انتخاب‌شده معتبر نیست.";
            return false;
        }

        try
        {
            var persianDateTime = new global::PersianDateTime(year, month, day, 0, 0, 0);
            var gregorian = persianDateTime.ToDateTime();
            result = DateTime.SpecifyKind(gregorian, DateTimeKind.Unspecified);
            return true;
        }
        catch
        {
            error = "تاریخ انتخاب‌شده معتبر نیست.";
            return false;
        }
    }

    private static bool TryExtractPersianDateParts(string value, out int year, out int month, out int day)
    {
        year = 0;
        month = 0;
        day = 0;

        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var parts = value.Split('-', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 3)
        {
            return false;
        }

        if (!int.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out year))
        {
            return false;
        }

        if (!int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out month))
        {
            return false;
        }

        if (!int.TryParse(parts[2], NumberStyles.None, CultureInfo.InvariantCulture, out day))
        {
            return false;
        }

        return true;
    }

    private static string FormatPersianDate(DateTime dateTime)
    {
        var calendar = new PersianCalendar();
        var year = calendar.GetYear(dateTime);
        var month = calendar.GetMonth(dateTime);
        var day = calendar.GetDayOfMonth(dateTime);

        return string.Create(10, (year, month, day), static (span, state) =>
        {
            var (y, m, d) = state;
            span[0] = (char)('0' + (y / 1000) % 10);
            span[1] = (char)('0' + (y / 100) % 10);
            span[2] = (char)('0' + (y / 10) % 10);
            span[3] = (char)('0' + y % 10);
            span[4] = '-';
            span[5] = (char)('0' + (m / 10) % 10);
            span[6] = (char)('0' + m % 10);
            span[7] = '-';
            span[8] = (char)('0' + (d / 10) % 10);
            span[9] = (char)('0' + d % 10);
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateShipmentTracking(ShipmentTrackingFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "لطفاً تمام فیلدهای الزامی را پر کنید.";
            var invoiceId = await GetInvoiceIdFromItemAsync(model.InvoiceItemId);
            return RedirectToAction(nameof(Details), new { id = invoiceId });
        }

        var cancellationToken = HttpContext.RequestAborted;
        
        var statusDate = model.StatusDate;
        if (!string.IsNullOrWhiteSpace(model.StatusDatePersian))
        {
            if (DateTime.TryParse(model.StatusDatePersian, out var parsedDate))
            {
                statusDate = parsedDate;
            }
        }

        var command = new CreateShipmentTrackingCommand(
            model.InvoiceItemId,
            model.Status,
            statusDate,
            model.TrackingNumber,
            model.Notes);

        var result = await _mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
        {
            TempData["Error"] = result.Error ?? "خطا در ایجاد پیگیری ارسال.";
        }
        else
        {
            TempData["Success"] = "پیگیری ارسال با موفقیت ایجاد شد.";
        }

        var invoiceId2 = await GetInvoiceIdFromItemAsync(model.InvoiceItemId);
        return RedirectToAction(nameof(Details), new { id = invoiceId2 });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateShipmentTracking(ShipmentTrackingFormViewModel model)
    {
        if (!model.Id.HasValue)
        {
            TempData["Error"] = "شناسه پیگیری ارسال معتبر نیست.";
            var invoiceId = await GetInvoiceIdFromItemAsync(model.InvoiceItemId);
            return RedirectToAction(nameof(Details), new { id = invoiceId });
        }

        if (!ModelState.IsValid)
        {
            TempData["Error"] = "لطفاً تمام فیلدهای الزامی را پر کنید.";
            var invoiceId = await GetInvoiceIdFromItemAsync(model.InvoiceItemId);
            return RedirectToAction(nameof(Details), new { id = invoiceId });
        }

        var cancellationToken = HttpContext.RequestAborted;
        
        var statusDate = model.StatusDate;
        if (!string.IsNullOrWhiteSpace(model.StatusDatePersian))
        {
            if (DateTime.TryParse(model.StatusDatePersian, out var parsedDate))
            {
                statusDate = parsedDate;
            }
        }

        var command = new UpdateShipmentTrackingCommand(
            model.Id.Value,
            model.Status,
            statusDate,
            model.TrackingNumber,
            model.Notes);

        var result = await _mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
        {
            TempData["Error"] = result.Error ?? "خطا در به‌روزرسانی پیگیری ارسال.";
        }
        else
        {
            TempData["Success"] = "پیگیری ارسال با موفقیت به‌روزرسانی شد.";
        }

        var invoiceId2 = await GetInvoiceIdFromItemAsync(model.InvoiceItemId);
        return RedirectToAction(nameof(Details), new { id = invoiceId2 });
    }

    private async Task<Guid> GetInvoiceIdFromItemAsync(Guid invoiceItemId)
    {
        var cancellationToken = HttpContext.RequestAborted;
        var invoicesResult = await _mediator.Send(new GetInvoiceListQuery(new InvoiceListFilterDto(null, null, null, null, null, 1, 1000)), cancellationToken);
        
        if (!invoicesResult.IsSuccess || invoicesResult.Value is null)
        {
            return Guid.Empty;
        }
        
        foreach (var invoice in invoicesResult.Value.Items)
        {
            var detail = await _mediator.Send(new GetInvoiceDetailsQuery(invoice.Id), cancellationToken);
            if (detail.IsSuccess && detail.Value?.Items.Any(item => item.Id == invoiceItemId) == true)
            {
                return invoice.Id;
            }
        }

        return Guid.Empty;
    }
}
