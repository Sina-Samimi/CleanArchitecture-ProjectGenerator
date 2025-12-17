using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LogsDtoCloneTest.Application.Commands.Sellers;
using LogsDtoCloneTest.Application.Interfaces;
using LogsDtoCloneTest.Application.Queries.Identity.GetUserLookups;
using LogsDtoCloneTest.Application.Queries.Sellers;
using LogsDtoCloneTest.SharedKernel.Extensions;
using LogsDtoCloneTest.WebSite.Areas.Admin.Models;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace LogsDtoCloneTest.WebSite.Areas.Admin.Controllers;

[Area("Admin")]
public sealed class SellersController : Controller
{
    private readonly IMediator _mediator;
    private readonly IFormFileSettingServices _fileSettingServices;

    private const int MaxAvatarFileSizeKb = 2 * 1024;
    private static readonly string[] AllowedAvatarContentTypes =
    {
        "image/jpeg",
        "image/png",
        "image/webp"
    };

    private const string SellerAvatarUploadFolder = "seller-profiles";

    public SellersController(IMediator mediator, IFormFileSettingServices fileSettingServices)
    {
        _mediator = mediator;
        _fileSettingServices = fileSettingServices;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        int page = 1,
        string? name = null,
        string? phone = null,
        string? dateFrom = null,
        string? dateTo = null,
        CancellationToken cancellationToken = default)
    {
        ConfigurePageMetadata("مدیریت فروشندگان", "تعریف و ویرایش اطلاعات فروشندگان سایت");

        var pageNumber = page < 1 ? 1 : page;
        var pageSize = 20;

        // Parse Persian date strings using shared parser (expects yyyy-MM-dd normalized)
        DateTimeOffset? parsedDateFrom = null;
        DateTimeOffset? parsedDateTo = null;

        string? parsedDateFromNormalized = null;
        string? parsedDateToNormalized = null;

        if (!string.IsNullOrWhiteSpace(dateFrom))
        {
            var normalized = dateFrom.Replace('/', '-');
            var parsed = UserFilterFormatting.ParsePersianDate(normalized, toExclusiveEnd: false, out var normalizedOut);
            parsedDateFrom = parsed;
            parsedDateFromNormalized = normalizedOut?.Replace('-', '/');
        }

        if (!string.IsNullOrWhiteSpace(dateTo))
        {
            var normalized = dateTo.Replace('/', '-');
            var parsed = UserFilterFormatting.ParsePersianDate(normalized, toExclusiveEnd: true, out var normalizedOut);
            parsedDateTo = parsed;
            parsedDateToNormalized = normalizedOut?.Replace('-', '/');
        }

        var cancellation = HttpContext.RequestAborted;
        var result = await _mediator.Send(new GetSellerProfilesQuery(), cancellation);

        if (!result.IsSuccess || result.Value is null)
        {
            TempData["Sellers.Error"] = result.Error ?? "دریافت لیست فروشندگان با خطا مواجه شد.";
            return View(new SellerProfilesIndexViewModel
            {
                Sellers = Array.Empty<SellerProfileListItemViewModel>(),
                ActiveCount = 0,
                InactiveCount = 0,
                ErrorMessage = result.Error ?? TempData["Sellers.Error"] as string,
                SuccessMessage = TempData["Sellers.Success"] as string,
                TotalCount = 0,
                PageNumber = 1,
                PageSize = pageSize,
                TotalPages = 1
            });
        }

        var dto = result.Value;

        // Apply filters in-memory (query returns all items)
        var filtered = dto.Items.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(name))
        {
            filtered = filtered.Where(i => !string.IsNullOrWhiteSpace(i.DisplayName) && i.DisplayName.IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0);
        }

        if (!string.IsNullOrWhiteSpace(phone))
        {
            filtered = filtered.Where(i => !string.IsNullOrWhiteSpace(i.ContactPhone) && i.ContactPhone.IndexOf(phone, StringComparison.OrdinalIgnoreCase) >= 0);
        }

        if (parsedDateFrom.HasValue)
        {
            filtered = filtered.Where(i => i.CreatedAt >= parsedDateFrom.Value);
        }

        if (parsedDateTo.HasValue)
        {
            filtered = filtered.Where(i => i.CreatedAt <= parsedDateTo.Value);
        }

        // Order and paginate
        filtered = filtered.OrderByDescending(i => i.UpdatedAt);

        var totalCount = filtered.Count();
        var totalPages = totalCount == 0 ? 1 : (int)Math.Ceiling(totalCount / (double)pageSize);
        var safePage = pageNumber > totalPages ? totalPages : pageNumber;

        var pageItems = filtered.Skip((safePage - 1) * pageSize).Take(pageSize).ToArray();

        var items = pageItems
            .Select(item => new SellerProfileListItemViewModel(
                item.Id,
                item.DisplayName,
                item.LicenseNumber,
                item.LicenseIssueDate,
                item.LicenseExpiryDate,
                item.ShopAddress,
                item.WorkingHours,
                item.ExperienceYears,
                item.Bio,
                item.ContactEmail,
                item.ContactPhone,
                item.UserId,
                item.IsActive,
                item.CreatedAt,
                item.UpdatedAt))
            .ToArray();

        var viewModel = new SellerProfilesIndexViewModel
        {
            Sellers = items,
            ActiveCount = dto.ActiveCount,
            InactiveCount = dto.InactiveCount,
            SuccessMessage = TempData["Sellers.Success"] as string,
            ErrorMessage = TempData["Sellers.Error"] as string,
            TotalCount = totalCount,
            PageNumber = safePage,
            PageSize = pageSize,
            TotalPages = totalPages,
            SelectedName = name,
            SelectedPhone = phone,
            SelectedDateFrom = parsedDateFrom,
            SelectedDateTo = parsedDateTo,
            SelectedDateFromPersian = parsedDateFromNormalized,
            SelectedDateToPersian = parsedDateToNormalized
        };

        return View(viewModel);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        ConfigurePageMetadata("افزودن مدرس جدید", "ثبت اطلاعات مدرسان برای استفاده در محصولات و پنل مدرس");

        var model = new SellerProfileFormViewModel();
        await PopulateUserOptionsAsync(model, HttpContext.RequestAborted);
        return View("Form", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(SellerProfileFormViewModel model)
    {
        ConfigurePageMetadata("افزودن مدرس جدید", "ثبت اطلاعات مدرسان برای استفاده در محصولات و پنل مدرس");

        NormalizeForm(model);

        var cancellationToken = HttpContext.RequestAborted;
        await PopulateUserOptionsAsync(model, cancellationToken);

        ValidateAvatar(model.AvatarFile, nameof(model.AvatarFile));

        if (!ModelState.IsValid)
        {
            return View("Form", model);
        }

        var uploadedAvatarPath = await SaveAvatarAsync(model.AvatarFile, nameof(model.AvatarFile));

        if (!ModelState.IsValid)
        {
            return View("Form", model);
        }

        var avatarPath = uploadedAvatarPath ?? model.AvatarUrl;

        var command = new CreateSellerProfileCommand(
            model.DisplayName,
            model.LicenseNumber,
            model.LicenseIssueDate,
            model.LicenseExpiryDate,
            model.ShopAddress,
            model.WorkingHours,
            model.ExperienceYears,
            model.Bio,
            avatarPath,
            model.ContactEmail,
            model.ContactPhone,
            model.UserId,
            model.IsActive,
            model.SellerSharePercentage);

        var result = await _mediator.Send(command, cancellationToken);
        if (!result.IsSuccess)
        {
            if (!string.IsNullOrWhiteSpace(uploadedAvatarPath))
            {
                DeleteAvatarFile(uploadedAvatarPath);
            }

            ModelState.AddModelError(string.Empty, result.Error ?? "ثبت مدرس با خطا مواجه شد.");
            return View("Form", model);
        }

        TempData["Sellers.Success"] = "فروشنده جدید با موفقیت ثبت شد.";
        TempData["Alert.Message"] = "پروفایل مدرس ایجاد شد.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(Guid id)
    {
        ConfigurePageMetadata("ویرایش اطلاعات مدرس", "به‌روزرسانی مشخصات و اطلاعات تماس مدرس");

        var cancellationToken = HttpContext.RequestAborted;
        var result = await _mediator.Send(new GetSellerProfileDetailQuery(id), cancellationToken);
        if (!result.IsSuccess || result.Value is null)
        {
            TempData["Sellers.Error"] = result.Error ?? "فروشنده مورد نظر یافت نشد.";
            return RedirectToAction(nameof(Index));
        }

        var dto = result.Value;
        var model = new SellerProfileFormViewModel
        {
            Id = dto.Id,
            DisplayName = dto.DisplayName,
            LicenseNumber = dto.LicenseNumber,
            LicenseIssueDate = dto.LicenseIssueDate,
            LicenseExpiryDate = dto.LicenseExpiryDate,
            ShopAddress = dto.ShopAddress,
            WorkingHours = dto.WorkingHours,
            ExperienceYears = dto.ExperienceYears,
            Bio = dto.Bio,
            AvatarUrl = dto.AvatarUrl,
            OriginalAvatarUrl = dto.AvatarUrl,
            ContactEmail = dto.ContactEmail,
            ContactPhone = dto.ContactPhone,
            UserId = dto.UserId,
            IsActive = dto.IsActive,
            SellerSharePercentage = dto.SellerSharePercentage
        };

        await PopulateUserOptionsAsync(model, HttpContext.RequestAborted);
        return View("Form", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, SellerProfileFormViewModel model)
    {
        ConfigurePageMetadata("ویرایش اطلاعات مدرس", "به‌روزرسانی مشخصات و اطلاعات تماس مدرس");

        if (id != model.Id)
        {
            ModelState.AddModelError(string.Empty, "شناسه فروشنده معتبر نیست.");
        }

        NormalizeForm(model);

        var cancellationToken = HttpContext.RequestAborted;
        await PopulateUserOptionsAsync(model, cancellationToken);

        ValidateAvatar(model.AvatarFile, nameof(model.AvatarFile));

        if (!ModelState.IsValid)
        {
            return View("Form", model);
        }

        var uploadedAvatarPath = await SaveAvatarAsync(model.AvatarFile, nameof(model.AvatarFile));

        if (!ModelState.IsValid)
        {
            return View("Form", model);
        }

        var previousAvatarPath = model.OriginalAvatarUrl;
        var avatarPath = uploadedAvatarPath ?? model.AvatarUrl;

        var command = new UpdateSellerProfileCommand(
            id,
            model.DisplayName,
            model.LicenseNumber,
            model.LicenseIssueDate,
            model.LicenseExpiryDate,
            model.ShopAddress,
            model.WorkingHours,
            model.ExperienceYears,
            model.Bio,
            avatarPath,
            model.ContactEmail,
            model.ContactPhone,
            model.UserId,
            model.IsActive,
            model.SellerSharePercentage);

        var result = await _mediator.Send(command, cancellationToken);
        if (!result.IsSuccess)
        {
            if (!string.IsNullOrWhiteSpace(uploadedAvatarPath))
            {
                DeleteAvatarFile(uploadedAvatarPath);
            }

            ModelState.AddModelError(string.Empty, result.Error ?? "ویرایش مدرس با خطا مواجه شد.");
            return View("Form", model);
        }

        if (!string.IsNullOrWhiteSpace(uploadedAvatarPath) &&
            !string.IsNullOrWhiteSpace(previousAvatarPath) &&
            !string.Equals(previousAvatarPath, uploadedAvatarPath, StringComparison.OrdinalIgnoreCase))
        {
            DeleteAvatarFile(previousAvatarPath);
        }

        TempData["Sellers.Success"] = "اطلاعات فروشنده با موفقیت به‌روزرسانی شد.";
        TempData["Alert.Message"] = "پروفایل مدرس ویرایش شد.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Activate(Guid id)
    {
        var cancellationToken = HttpContext.RequestAborted;
        var result = await _mediator.Send(new ActivateSellerCommand(id), cancellationToken);

        if (!result.IsSuccess)
        {
            TempData["Sellers.Error"] = result.Error ?? "فعال‌سازی فروشنده با خطا مواجه شد.";
        }
        else
        {
            TempData["Sellers.Success"] = "پروفایل فروشنده فعال شد.";
            TempData["Alert.Title"] = "وضعیت مدرس";
            TempData["Alert.Message"] = "دسترسی مدرس دوباره فعال شد و امکان ورود برای او فراهم است.";
            TempData["Alert.Type"] = "success";
            TempData["Alert.ConfirmText"] = "باشه";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Deactivate(Guid id)
    {
        var cancellationToken = HttpContext.RequestAborted;
        var result = await _mediator.Send(new DeactivateSellerCommand(id, null), cancellationToken);

        if (!result.IsSuccess)
        {
            TempData["Sellers.Error"] = result.Error ?? "غیرفعال‌سازی فروشنده با خطا مواجه شد.";
        }
        else
        {
            TempData["Sellers.Success"] = "پروفایل فروشنده غیرفعال شد.";
            TempData["Alert.Title"] = "وضعیت مدرس";
            TempData["Alert.Message"] = "دسترسی مدرس غیرفعال شد. برای فعال‌سازی مجدد از همین بخش اقدام کنید.";
            TempData["Alert.Type"] = "warning";
            TempData["Alert.ConfirmText"] = "متوجه شدم";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Profile(Guid id)
    {
        ConfigurePageMetadata("پروفایل فروشنده", "اطلاعات کامل فروشنده");

        var cancellationToken = HttpContext.RequestAborted;
        var result = await _mediator.Send(new GetSellerProfileDetailQuery(id), cancellationToken);
        if (!result.IsSuccess || result.Value is null)
        {
            TempData["Sellers.Error"] = result.Error ?? "فروشنده مورد نظر یافت نشد.";
            return RedirectToAction(nameof(Index));
        }

        var sellerDto = result.Value;

        // Get user information if UserId exists
        string? userFullName = null;
        string? userEmail = null;
        string? userPhoneNumber = null;
        if (!string.IsNullOrWhiteSpace(sellerDto.UserId))
        {
            var userResult = await _mediator.Send(new Application.Queries.Identity.GetUserById.GetUserByIdQuery(sellerDto.UserId), cancellationToken);
            if (userResult.IsSuccess && userResult.Value is not null)
            {
                userFullName = userResult.Value.FullName;
                userEmail = userResult.Value.Email;
                userPhoneNumber = userResult.Value.PhoneNumber;
            }
        }

        // Get seller statistics
        var productOfferRepository = HttpContext.RequestServices.GetRequiredService<IProductOfferRepository>();
        var offers = await productOfferRepository.GetBySellerIdAsync(sellerDto.UserId ?? string.Empty, includeInactive: true, cancellationToken);
        var totalOffers = offers.Count;
        var activeOffers = offers.Count(o => o.IsActive && o.IsPublished && !o.IsDeleted);
        
        // Get products count (products that have this seller as main seller)
        var productRepository = HttpContext.RequestServices.GetRequiredService<IProductRepository>();
        int totalProducts = 0;
        if (!string.IsNullOrWhiteSpace(sellerDto.UserId))
        {
            var products = await productRepository.GetBySellerAsync(sellerDto.UserId, cancellationToken);
            totalProducts = products.Count;
        }

        // Calculate total sales (this would require invoice/order data)
        decimal totalSales = 0; // TODO: Calculate from invoices/orders

        var viewModel = new SellerProfileViewModel
        {
            Id = sellerDto.Id,
            DisplayName = sellerDto.DisplayName,
            LicenseNumber = sellerDto.LicenseNumber,
            LicenseIssueDate = sellerDto.LicenseIssueDate,
            LicenseExpiryDate = sellerDto.LicenseExpiryDate,
            ShopAddress = sellerDto.ShopAddress,
            WorkingHours = sellerDto.WorkingHours,
            ExperienceYears = sellerDto.ExperienceYears,
            Bio = sellerDto.Bio,
            AvatarUrl = sellerDto.AvatarUrl,
            ContactEmail = sellerDto.ContactEmail,
            ContactPhone = sellerDto.ContactPhone,
            UserId = sellerDto.UserId,
            IsActive = sellerDto.IsActive,
            SellerSharePercentage = sellerDto.SellerSharePercentage,
            CreatedAt = sellerDto.CreatedAt,
            UpdatedAt = sellerDto.UpdatedAt,
            UserFullName = userFullName,
            UserEmail = userEmail,
            UserPhoneNumber = userPhoneNumber,
            TotalProducts = totalProducts,
            TotalOffers = totalOffers,
            TotalSales = totalSales
        };

        return View(viewModel);
    }

    private async Task PopulateUserOptionsAsync(SellerProfileFormViewModel model, CancellationToken cancellationToken)
    {
        if (model is null)
        {
            return;
        }

        var result = await _mediator.Send(new GetUserLookupsQuery(), cancellationToken);
        if (!result.IsSuccess || result.Value is null)
        {
            model.UserOptions = Array.Empty<SelectListItem>();
            return;
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
                Text = text
            });
        }

        if (!string.IsNullOrWhiteSpace(model.UserId) && options.All(option => option.Value != model.UserId))
        {
            options.Insert(0, new SelectListItem
            {
                Value = model.UserId,
                Text = $"{model.UserId} (کاربر یافت نشد یا غیرفعال است)"
            });
        }

        model.UserOptions = options;
    }

    private static void NormalizeForm(SellerProfileFormViewModel model)
    {
        if (model is null)
        {
            return;
        }

        model.DisplayName = model.DisplayName?.Trim() ?? string.Empty;
        model.LicenseNumber = string.IsNullOrWhiteSpace(model.LicenseNumber) ? null : model.LicenseNumber.Trim();
        model.ShopAddress = string.IsNullOrWhiteSpace(model.ShopAddress) ? null : model.ShopAddress.Trim();
        model.WorkingHours = string.IsNullOrWhiteSpace(model.WorkingHours) ? null : model.WorkingHours.Trim();
        model.Bio = string.IsNullOrWhiteSpace(model.Bio) ? null : model.Bio.Trim();
        model.AvatarUrl = string.IsNullOrWhiteSpace(model.AvatarUrl) ? null : model.AvatarUrl.Trim();
        model.ContactEmail = string.IsNullOrWhiteSpace(model.ContactEmail) ? null : model.ContactEmail.Trim();
        model.ContactPhone = string.IsNullOrWhiteSpace(model.ContactPhone) ? null : model.ContactPhone.Trim();
        model.UserId = string.IsNullOrWhiteSpace(model.UserId) ? null : model.UserId.Trim();
        model.OriginalAvatarUrl = string.IsNullOrWhiteSpace(model.OriginalAvatarUrl) ? null : model.OriginalAvatarUrl.Trim();
    }

    private void ConfigurePageMetadata(string title, string subtitle)
    {
        ViewData["Title"] = title;
        ViewData["Subtitle"] = subtitle;
        ViewData["SearchPlaceholder"] = "جستجوی مدرس";
        ViewData["ShowSearch"] = false;
    }

    private bool ValidateAvatar(IFormFile? avatarFile, string propertyName)
    {
        if (avatarFile is null || avatarFile.Length == 0)
        {
            return true;
        }

        if (!_fileSettingServices.IsFileSizeValid(avatarFile, MaxAvatarFileSizeKb))
        {
            ModelState.AddModelError(propertyName, "حجم تصویر باید کمتر از ۲ مگابایت باشد.");
            return false;
        }

        var contentType = avatarFile.ContentType ?? string.Empty;
        if (!AllowedAvatarContentTypes.Contains(contentType))
        {
            ModelState.AddModelError(propertyName, "تنها فرمت‌های PNG، JPG و WEBP مجاز هستند.");
            return false;
        }

        return true;
    }

    private Task<string?> SaveAvatarAsync(IFormFile? avatarFile, string propertyName)
    {
        if (avatarFile is null || avatarFile.Length == 0)
        {
            return Task.FromResult<string?>(null);
        }

        try
        {
            var response = _fileSettingServices.UploadImage(SellerAvatarUploadFolder, avatarFile, Guid.NewGuid().ToString("N"));

            if (!response.Success)
            {
                var errorMessage = response.Messages.FirstOrDefault()?.message ?? "ذخیره‌سازی تصویر انجام نشد.";
                ModelState.AddModelError(propertyName, errorMessage);
                return Task.FromResult<string?>(null);
            }

            return Task.FromResult(response.Data);
        }
        catch
        {
            ModelState.AddModelError(propertyName, "ذخیره‌سازی تصویر انجام نشد.");
            return Task.FromResult<string?>(null);
        }
    }

    private void DeleteAvatarFile(string? relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            return;
        }

        _fileSettingServices.DeleteFile(relativePath);
    }
}
