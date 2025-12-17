using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using LogTableRenameTest.Application.Commands.Catalog;
using LogTableRenameTest.Application.Queries.Catalog;
using LogTableRenameTest.Application.Interfaces;
using LogTableRenameTest.Domain.Entities;
using LogTableRenameTest.WebSite.Models.Product;
using LogTableRenameTest.WebSite.Services;
using LogTableRenameTest.WebSite.Services.Products;
using LogTableRenameTest.WebSite.Services.Session;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LogTableRenameTest.WebSite.Controllers;

public class ProductController : Controller
{
    private static readonly IReadOnlyList<(string Value, double Threshold, string Label)> RatingOptions = new List<(string, double, string)>
    {
        ("4.5", 4.5, "امتیاز ۴.۵ به بالا"),
        ("4", 4.0, "امتیاز ۴ به بالا"),
        ("3.5", 3.5, "امتیاز ۳.۵ به بالا")
    };

    private static readonly IReadOnlyList<(string Value, string Label)> SortOptions = new List<(string, string)>
    {
        ("newest", "جدیدترین"),
        ("price-asc", "ارزان‌ترین"),
        ("price-desc", "گران‌ترین"),
        ("rating", "بیشترین امتیاز")
    };

    private readonly IProductCatalogService _productCatalogService;
    private readonly IMediator _mediator;
    private readonly IProductRequestRepository _productRequestRepository;
    private readonly ISellerProfileRepository _sellerProfileRepository;
    private readonly IProductRepository _productRepository;
    private readonly IProductBackInStockSubscriptionRepository _backInStockSubscriptionRepository;
    private readonly IPhoneVerificationService _phoneVerificationService;
    private readonly ISmsSender _smsSender;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IUserSessionRepository _userSessionRepository;
    private readonly ISessionCookieService _sessionCookieService;

    public ProductController(
        IProductCatalogService productCatalogService, 
        IMediator mediator,
        IProductRequestRepository productRequestRepository,
        ISellerProfileRepository sellerProfileRepository,
        IProductRepository productRepository,
        IProductBackInStockSubscriptionRepository backInStockSubscriptionRepository,
        IPhoneVerificationService phoneVerificationService,
        ISmsSender smsSender,
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IUserSessionRepository userSessionRepository,
        ISessionCookieService sessionCookieService)
    {
        _productCatalogService = productCatalogService;
        _mediator = mediator;
        _productRequestRepository = productRequestRepository;
        _sellerProfileRepository = sellerProfileRepository;
        _productRepository = productRepository;
        _backInStockSubscriptionRepository = backInStockSubscriptionRepository;
        _phoneVerificationService = phoneVerificationService;
        _smsSender = smsSender;
        _userManager = userManager;
        _signInManager = signInManager;
        _userSessionRepository = userSessionRepository;
        _sessionCookieService = sessionCookieService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        string? search,
        string? category,
        string? format,
        decimal? minPrice,
        decimal? maxPrice,
        double? rating,
        string? sort,
        CancellationToken cancellationToken)
    {
        var selectedSort = string.IsNullOrWhiteSpace(sort) ? "newest" : sort;
        var filterOptions = new ProductFilterOptions(
            search,
            category,
            format,
            minPrice,
            maxPrice,
            rating,
            selectedSort);

        var result = await _productCatalogService.GetProductsAsync(filterOptions, cancellationToken);
        var summaries = result.Products.Select(MapToSummary).ToList();

        var filters = new ProductFilterViewModel
        {
            SearchTerm = search,
            SelectedCategory = category,
            SelectedDeliveryFormat = format,
            SelectedSort = selectedSort,
            MinPrice = minPrice,
            MaxPrice = maxPrice,
            MinRating = rating,
            PriceRangeMin = result.PriceRangeMin,
            PriceRangeMax = result.PriceRangeMax,
            Categories = result.Categories
                .Select(value => new ProductFilterOptionViewModel
                {
                    Value = value,
                    Label = value,
                    Count = result.Products.Count(product => string.Equals(product.Category, value, StringComparison.OrdinalIgnoreCase)),
                    IsSelected = string.Equals(value, category, StringComparison.OrdinalIgnoreCase)
                })
                .ToList(),
            DeliveryFormats = result.DeliveryFormats
                .Select(value => new ProductFilterOptionViewModel
                {
                    Value = value,
                    Label = value,
                    Count = result.Products.Count(product => string.Equals(product.DeliveryFormat, value, StringComparison.OrdinalIgnoreCase)),
                    IsSelected = string.Equals(value, format, StringComparison.OrdinalIgnoreCase)
                })
                .ToList(),
            RatingOptions = RatingOptions
                .Select(option => new ProductFilterOptionViewModel
                {
                    Value = option.Value,
                    Label = option.Label,
                    Count = result.Products.Count(product => product.Rating >= option.Threshold),
                    IsSelected = rating.HasValue && Math.Abs(rating.Value - option.Threshold) < 0.01
                })
                .ToList(),
            SortOptions = SortOptions
                .Select(option => new ProductFilterOptionViewModel
                {
                    Value = option.Value,
                    Label = option.Label,
                    Count = 0,
                    IsSelected = string.Equals(option.Value, selectedSort, StringComparison.OrdinalIgnoreCase)
                })
                .ToList()
        };

        var viewModel = new ProductListViewModel
        {
            Products = summaries,
            Filters = filters,
            TotalCount = result.TotalCount
        };

        // متغیرها برای Template SEO
        ViewData["TotalCount"] = result.TotalCount;
        HttpContext.Items["TotalCount"] = result.TotalCount;
        HttpContext.Items["category"] = category ?? "";
        HttpContext.Items["search"] = search ?? "";
        HttpContext.Items["minPrice"] = minPrice?.ToString() ?? "";
        HttpContext.Items["maxPrice"] = maxPrice?.ToString() ?? "";

        // Fallback SEO (اگر در سیستم SEO تنظیم نشده باشد)
        ViewData["Title"] = "محصولات تخصصی آرسیس";
        ViewData["MetaDescription"] = "پکیج‌ها و محصولات تخصصی آرسیس برای تحلیل استعداد، توسعه رهبری و استقرار تیم‌های چابک.";
        ViewData["CanonicalUrl"] = Url.Action("Index", "Product", null, Request.Scheme);

        return View(viewModel);
    }

    [HttpGet("/product/{slug}")]
    public async Task<IActionResult> Details(string slug, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(slug))
        {
            return NotFound();
        }

        var product = await _productCatalogService.GetBySlugAsync(slug, cancellationToken);
        if (product is null)
        {
            return NotFound();
        }

        var detailViewModel = await BuildDetailViewModelAsync(product, cancellationToken);

        ViewData["Title"] = product.Name;
        ViewData["MetaDescription"] = product.ShortDescription;
        ViewData["MetaOgImage"] = product.HeroImageUrl;
        ViewData["MetaOgType"] = "product";
        ViewData["MetaOgUrl"] = Url.Action("Details", "Product", new { slug }, Request.Scheme);

        return View(detailViewModel);
    }

    [HttpPost("/product/{slug}/comments")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddComment(
        string slug,
        [Bind(Prefix = "NewComment")] ProductCommentFormModel form,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(slug))
        {
            return NotFound();
        }

        var product = await _productCatalogService.GetBySlugAsync(slug.Trim(), cancellationToken);
        if (product is null)
        {
            return NotFound();
        }

        form ??= new ProductCommentFormModel();
        form.ProductId = product.Id;

        ViewData["Title"] = product.Name;
        ViewData["MetaDescription"] = product.ShortDescription;
        ViewData["MetaOgImage"] = product.HeroImageUrl;
        ViewData["MetaOgType"] = "product";
        ViewData["MetaOgUrl"] = Url.Action("Details", "Product", new { slug }, Request.Scheme);

        // Only main comments (without ParentId) can have rating
        if (form.ParentId.HasValue)
        {
            form.Rating = 0;
        }

        if (!ModelState.IsValid)
        {
            var invalidViewModel = await BuildDetailViewModelAsync(product, cancellationToken, form);
            return View("Details", invalidViewModel);
        }

        var success = await _productCatalogService.AddCommentAsync(
            product.Id,
            form.AuthorName,
            form.Content,
            form.ParentId.HasValue ? 0 : form.Rating, // Only main comments can have rating
            form.ParentId,
            cancellationToken);

        if (!success)
        {
            ModelState.AddModelError(string.Empty, "ثبت نظر با مشکل مواجه شد. لطفاً دوباره تلاش کنید.");
            var failureViewModel = await BuildDetailViewModelAsync(product, cancellationToken, form);
            return View("Details", failureViewModel);
        }

        TempData["ProductCommentSuccess"] = true;
        var redirectUrl = Url.Action(nameof(Details), new { slug });
        if (string.IsNullOrWhiteSpace(redirectUrl))
        {
            return RedirectToAction(nameof(Details), new { slug });
        }

        return Redirect(redirectUrl + "#comments");
    }

    [HttpPost("/product/{slug}/custom-request")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SubmitCustomRequest(
        string slug,
        ProductCustomRequestFormModel form,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(slug))
        {
            return NotFound();
        }

        var product = await _productCatalogService.GetBySlugAsync(slug, cancellationToken);
        if (product is null)
        {
            return NotFound();
        }

        if (!product.IsCustomOrder)
        {
            TempData["CustomRequestError"] = "این محصول حالت سفارشی ندارد.";
            return RedirectToAction(nameof(Details), new { slug });
        }

        if (!ModelState.IsValid)
        {
            TempData["CustomRequestError"] = "لطفاً تمام فیلدهای الزامی را پر کنید.";
            return RedirectToAction(nameof(Details), new { slug });
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var command = new CreateProductCustomRequestCommand(
            product.Id,
            form.FullName,
            form.Phone,
            form.Email,
            form.Message,
            userId);

        var result = await _mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
        {
            TempData["CustomRequestError"] = result.Error ?? "ثبت درخواست با خطا مواجه شد. لطفاً دوباره تلاش کنید.";
        }
        else
        {
            TempData["CustomRequestSuccess"] = true;
        }

        return RedirectToAction(nameof(Details), new { slug });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> StartBackInStockVerification(Guid productId, string phoneNumber, CancellationToken cancellationToken)
    {
        if (productId == Guid.Empty)
        {
            return Json(new { success = false, message = "شناسه محصول نامعتبر است." });
        }

        var normalizedPhone = (phoneNumber ?? string.Empty).Trim();
        var digits = new string(normalizedPhone.Where(char.IsDigit).ToArray());

        // اعتبارسنجی ساده شماره موبایل ایرانی
        if (digits.Length != 11 || !digits.StartsWith("09", StringComparison.Ordinal))
        {
            return Json(new { success = false, message = "لطفاً شماره موبایل معتبر ایرانی وارد کنید (مثال: 09123456789)." });
        }

        // تولید کد تأیید و ذخیره در حافظه
        var issuance = _phoneVerificationService.GenerateCode(digits);

        // ارسال پیامک از طریق Hangfire (در پس‌زمینه)
        await _smsSender.SendVerificationCodeAsync(digits, issuance.Code, _phoneVerificationService.CodeLifetime, cancellationToken);

        // فعلاً برای تست، خودِ کد را هم برمی‌گردانیم
        return Json(new { success = true, message = "کد تایید ارسال شد.", code = issuance.Code });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfirmBackInStock(Guid productId, string phoneNumber, string code, CancellationToken cancellationToken)
    {
        if (productId == Guid.Empty)
        {
            return Json(new { success = false, message = "شناسه محصول نامعتبر است." });
        }

        var normalizedPhone = (phoneNumber ?? string.Empty).Trim();
        var digits = new string(normalizedPhone.Where(char.IsDigit).ToArray());

        if (digits.Length != 11 || !digits.StartsWith("09", StringComparison.Ordinal))
        {
            return Json(new { success = false, message = "شماره موبایل نامعتبر است." });
        }

        var validation = _phoneVerificationService.ValidateCode(digits, code ?? string.Empty);
        if (!validation.Succeeded)
        {
            string errorMessage = validation.Error switch
            {
                PhoneVerificationError.Expired => "کد تایید منقضی شده است. لطفاً دوباره تلاش کنید.",
                PhoneVerificationError.Incorrect => "کد تایید اشتباه است.",
                _ => "کد تایید نامعتبر است."
            };

            return Json(new { success = false, message = errorMessage });
        }

        // بررسی آیا کاربر قبلاً لاگین است
        var currentUserId = User?.FindFirstValue(ClaimTypes.NameIdentifier);
        string userId;

        if (!string.IsNullOrWhiteSpace(currentUserId))
        {
            // کاربر لاگین است، از userId فعلی استفاده کن
            userId = currentUserId;
        }
        else
        {
            // کاربر لاگین نیست، باید پیدا یا ایجاد شود
            var existingUser = await _userManager.Users
                .FirstOrDefaultAsync(u => u.PhoneNumber == digits, cancellationToken);

            if (existingUser is not null)
            {
                // کاربر وجود دارد، لاگینش کن
                if (existingUser.IsDeleted || !existingUser.IsActive)
                {
                    return Json(new { success = false, message = "حساب کاربری شما غیرفعال است. لطفاً با پشتیبانی تماس بگیرید." });
                }

                if (!existingUser.PhoneNumberConfirmed)
                {
                    existingUser.PhoneNumberConfirmed = true;
                    existingUser.LastModifiedOn = DateTimeOffset.UtcNow;
                    await _userManager.UpdateAsync(existingUser);
                }

                await _signInManager.SignInAsync(existingUser, isPersistent: false);
                userId = existingUser.Id;

                // ایجاد سشن کاربر
                var userAgent = Request.Headers["User-Agent"].ToString();
                var session = UserSession.Start(
                    existingUser.Id,
                    HttpContext.Connection.RemoteIpAddress,
                    DetectDeviceType(userAgent),
                    DetectClientName(userAgent),
                    userAgent);
                await _userSessionRepository.AddAsync(session, cancellationToken);
                _sessionCookieService.SetCurrentSessionId(session.Id);
            }
            else
            {
                // کاربر وجود ندارد، ثبت‌نام کن
                var newUser = new ApplicationUser
                {
                    UserName = digits,
                    PhoneNumber = digits,
                    PhoneNumberConfirmed = true,
                    FullName = string.Empty,
                    IsActive = true,
                    CreatedOn = DateTimeOffset.UtcNow,
                    LastModifiedOn = DateTimeOffset.UtcNow
                };

                var createResult = await _userManager.CreateAsync(newUser);
                if (!createResult.Succeeded)
                {
                    var errors = string.Join(", ", createResult.Errors.Select(e => e.Description));
                    return Json(new { success = false, message = $"خطا در ایجاد حساب کاربری: {errors}" });
                }

                // لاگین کاربر جدید
                await _signInManager.SignInAsync(newUser, isPersistent: false);
                userId = newUser.Id;

                // ایجاد سشن کاربر
                var userAgent = Request.Headers["User-Agent"].ToString();
                var session = UserSession.Start(
                    newUser.Id,
                    HttpContext.Connection.RemoteIpAddress,
                    DetectDeviceType(userAgent),
                    DetectClientName(userAgent),
                    userAgent);
                await _userSessionRepository.AddAsync(session, cancellationToken);
                _sessionCookieService.SetCurrentSessionId(session.Id);
            }
        }

        // پاک کردن کد تأیید
        _phoneVerificationService.ClearCode(digits);

        // ثبت در لیست خبرم کن
        var command = new CreateBackInStockSubscriptionCommand(
            ProductId: productId,
            ProductOfferId: null,
            PhoneNumber: digits,
            UserId: userId);

        var response = await _mediator.Send(command, cancellationToken);
        var message = response.Messages.FirstOrDefault()?.message ?? "درخواست شما ثبت شد. پس از موجود شدن محصول از طریق پیامک به شما اطلاع داده می‌شود.";

        return Json(new { success = response.Success, message });
    }

    private static string DetectDeviceType(string userAgent)
    {
        if (string.IsNullOrWhiteSpace(userAgent))
            return "Unknown";

        var ua = userAgent.ToLowerInvariant();
        if (ua.Contains("mobile") || ua.Contains("android") || ua.Contains("iphone"))
            return "Mobile";
        if (ua.Contains("tablet") || ua.Contains("ipad"))
            return "Tablet";
        return "Desktop";
    }

    private static string DetectClientName(string userAgent)
    {
        if (string.IsNullOrWhiteSpace(userAgent))
            return "Unknown";

        var ua = userAgent.ToLowerInvariant();
        if (ua.Contains("chrome") && !ua.Contains("edg"))
            return "Chrome";
        if (ua.Contains("firefox"))
            return "Firefox";
        if (ua.Contains("safari") && !ua.Contains("chrome"))
            return "Safari";
        if (ua.Contains("edg"))
            return "Edge";
        if (ua.Contains("opera") || ua.Contains("opr"))
            return "Opera";
        return "Other";
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UnsubscribeBackInStock(Guid productId, CancellationToken cancellationToken)
    {
        if (productId == Guid.Empty)
        {
            return Json(new { success = false, message = "شناسه محصول نامعتبر است." });
        }

        var userId = User?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Json(new { success = false, message = "برای لغو درخواست باید وارد حساب کاربری شوید." });
        }

        var subscription = await _backInStockSubscriptionRepository
            .GetForUserAsync(productId, userId, cancellationToken);

        if (subscription is null)
        {
            return Json(new { success = false, message = "برای این محصول درخواست فعالی ثبت نشده است." });
        }

        subscription.IsDeleted = true;
        subscription.RemoveDate = DateTimeOffset.UtcNow;
        subscription.UpdateDate = DateTimeOffset.UtcNow;

        await _backInStockSubscriptionRepository.UpdateAsync(subscription, cancellationToken);

        return Json(new { success = true, message = "درخواست اطلاع‌رسانی شما لغو شد." });
    }

    private async Task<ProductDetailViewModel> BuildDetailViewModelAsync(
        Models.Product.Product product,
        CancellationToken cancellationToken,
        ProductCommentFormModel? form = null)
    {
        // Get product entity to check SellerId
        var productEntityResult = await _mediator.Send(new GetProductDetailQuery(product.Id), cancellationToken);
        var productEntity = productEntityResult.IsSuccess && productEntityResult.Value is not null
            ? productEntityResult.Value
            : null;

        var relatedProducts = await _productCatalogService.GetRelatedProductsAsync(product.Id, 4, cancellationToken);

        var commentForm = form ?? new ProductCommentFormModel { ProductId = product.Id };
        if (commentForm.ProductId == Guid.Empty)
        {
            commentForm.ProductId = product.Id;
        }

        // Get product offers
        var offersResult = await _mediator.Send(
            new GetProductOffersQuery(
                ProductId: product.Id,
                IncludeInactive: false,
                PageNumber: 1,
                PageSize: 100),
            cancellationToken);

        // Get seller profiles for offers to check if they are active
        var offerSellerIds = offersResult.IsSuccess && offersResult.Value is not null
            ? offersResult.Value.Offers
                .Where(o => o.IsPublished && o.IsActive)
                .Select(o => o.SellerId)
                .Distinct()
                .ToList()
            : new List<string>();

        var offerSellerProfiles = await Task.WhenAll(
            offerSellerIds.Select(async id =>
            {
                var profile = await _sellerProfileRepository.GetByUserIdAsync(id, cancellationToken);
                return (id, profile);
            }));
        var offerSellerMap = offerSellerProfiles
            .Where(s => s.profile is not null && s.profile.IsActive && !s.profile.IsDeleted)
            .ToDictionary(s => s.id, s => s.profile!);

        var activeOfferDtos = offersResult.IsSuccess && offersResult.Value is not null
            ? offersResult.Value.Offers
                .Where(o => o.IsPublished && o.IsActive && !string.IsNullOrWhiteSpace(o.SellerId))
                .ToList()
            : new List<ProductOfferDto>();

        var offers = activeOfferDtos
            .OrderBy(o => o.Price ?? decimal.MaxValue)
            .Select(o => new ProductOfferViewModel
            {
                Id = o.Id,
                SellerId = o.SellerId,
                SellerName = o.SellerName ?? "نامشخص",
                Price = o.Price,
                CompareAtPrice = o.CompareAtPrice,
                TrackInventory = o.TrackInventory,
                StockQuantity = o.StockQuantity
            })
            .ToList();

        var offerByRequestId = activeOfferDtos
            .Where(o => o.ApprovedFromRequestId.HasValue)
            .ToDictionary(o => o.ApprovedFromRequestId!.Value, o => o);

        var offerBySellerId = activeOfferDtos
            .GroupBy(o => o.SellerId)
            .ToDictionary(
                group => group.Key,
                group => group
                    .OrderBy(o => o.Price ?? decimal.MaxValue)
                    .First());

        // Get suggested product offers (ProductRequests with TargetProductId)
        var suggestedOffers = await _productRequestRepository.GetByTargetProductIdAsync(product.Id, cancellationToken);
        
        var sellerIds = suggestedOffers.Select(r => r.SellerId).Distinct().ToList();
        var sellerProfiles = await Task.WhenAll(
            sellerIds.Select(async id =>
            {
                var profile = await _sellerProfileRepository.GetByUserIdAsync(id, cancellationToken);
                return (id, profile);
            }));
        var sellerMap = sellerProfiles
            .Where(s => s.profile is not null && s.profile.IsActive && !s.profile.IsDeleted)
            .ToDictionary(s => s.id, s => s.profile!);

        var suggestedProductOffers = suggestedOffers
            .Where(r => !string.IsNullOrWhiteSpace(r.SellerId) && sellerMap.ContainsKey(r.SellerId))
            .Select(r =>
            {
                var sellerProfile = sellerMap.TryGetValue(r.SellerId, out var profile) ? profile : null;
                var imageUrl = !string.IsNullOrWhiteSpace(r.FeaturedImagePath) 
                    ? r.FeaturedImagePath 
                    : product.HeroImageUrl;
                var matchingOffer = offerByRequestId.TryGetValue(r.Id, out var offerFromRequest)
                    ? offerFromRequest
                    : (offerBySellerId.TryGetValue(r.SellerId, out var sellerOffer) ? sellerOffer : null);

                if (matchingOffer is null)
                {
                    return null;
                }

                var finalPrice = matchingOffer.Price ?? r.Price;
                var trackInventory = matchingOffer.TrackInventory;
                var stockQuantity = matchingOffer.StockQuantity;

                return new SuggestedProductOfferViewModel
                {
                    Id = matchingOffer.Id,
                    SellerId = r.SellerId,
                    SellerName = sellerProfile?.DisplayName ?? "نامشخص",
                    ProductName = r.Name,
                    CategoryName = r.Category?.Name ?? "نامشخص",
                    ImageUrl = imageUrl,
                    ShopAddress = sellerProfile?.ShopAddress,
                    Price = finalPrice,
                    TrackInventory = trackInventory,
                    StockQuantity = stockQuantity,
                    CreatedAt = r.CreateDate
                };
            })
            .Where(offer => offer is not null)
            .Select(offer => offer!)
            .ToList();

        // Determine if product can be added to cart
        // Product can be added to cart if:
        // 1. It has active offers from active sellers, OR
        // 2. It has suggested offers from active sellers, OR
        // 3. It has no main seller (SellerId is null/empty), OR
        // 4. It has a main seller that is active
        var canAddToCart = false;
        
        // Check if there are active offers or suggested offers from active sellers
        if (offers.Any() || suggestedProductOffers.Any())
        {
            // Product has active offers, so it can be added to cart (via offers)
            canAddToCart = true;
        }
        else if (productEntity is not null)
        {
            // If no offers, check if the main product can be added directly
            // (i.e., check if the main product's seller is active)
            if (string.IsNullOrWhiteSpace(productEntity.SellerId))
            {
                // Product has no seller, so it can be added to cart
                canAddToCart = true;
            }
            else
            {
                // Product has a seller, check if seller is active
                var mainProductSeller = await _sellerProfileRepository.GetByUserIdAsync(productEntity.SellerId, cancellationToken);
                if (mainProductSeller is not null && !mainProductSeller.IsDeleted && mainProductSeller.IsActive)
                {
                    canAddToCart = true;
                }
                // If seller is inactive or not found, canAddToCart remains false
                // This means the product can only be added via offers (which are already filtered to active sellers)
            }
        }
        else
        {
            // If productEntity is null, default to true (shouldn't happen, but safe fallback)
            canAddToCart = true;
        }

        var currentUserId = User?.FindFirstValue(ClaimTypes.NameIdentifier);
        var hasBackInStockSubscription = false;

        if (!string.IsNullOrWhiteSpace(currentUserId)
            && productEntity is not null
            && productEntity.TrackInventory
            && productEntity.StockQuantity <= 0)
        {
            var existingSubscription = await _backInStockSubscriptionRepository
                .GetForUserAsync(product.Id, currentUserId, cancellationToken);

            hasBackInStockSubscription = existingSubscription is not null;
        }

        return new ProductDetailViewModel
        {
            Product = MapToSummary(product),
            HeroImageUrl = product.HeroImageUrl,
            Description = product.Description,
            DifficultyLevel = product.DifficultyLevel,
            Duration = product.Duration,
            StockQuantity = productEntity?.StockQuantity ?? 0,
            TrackInventory = productEntity?.TrackInventory ?? false,
            Highlights = product.Highlights,
            Modules = product.Modules.Select(module => new ProductModuleViewModel
            {
                Title = module.Title,
                Description = module.Description,
                Duration = module.Duration
            }).ToList(),
            Statistics = product.Statistics.Select(stat => new ProductStatisticViewModel
            {
                Label = stat.Label,
                Value = stat.Value,
                Tooltip = stat.Tooltip
            }).ToList(),
            FaqItems = product.FaqItems.Select(faq => new ProductFaqItemViewModel
            {
                Question = faq.Question,
                Answer = faq.Answer
            }).ToList(),
            Attributes = product.Attributes.Select(attr => new ProductAttributeViewModel
            {
                Key = attr.Key,
                Value = attr.Value
            }).ToList(),
            VariantAttributes = productEntity?.VariantAttributes
                .OrderBy(attr => attr.DisplayOrder)
                .ThenBy(attr => attr.Name)
                .Select(attr => new ProductVariantAttributeViewModel
                {
                    Id = attr.Id,
                    Name = attr.Name,
                    Options = attr.Options.ToList(),
                    DisplayOrder = attr.DisplayOrder
                })
                .ToList() ?? new List<ProductVariantAttributeViewModel>(),
            Variants = productEntity?.Variants
                .Select(v => new ProductVariantViewModel
                {
                    Id = v.Id,
                    Price = v.Price,
                    CompareAtPrice = v.CompareAtPrice,
                    StockQuantity = v.StockQuantity,
                    Sku = v.Sku,
                    ImagePath = v.ImagePath,
                    IsActive = v.IsActive,
                    Options = v.Options
                        .Select(opt => new ProductVariantOptionViewModel
                        {
                            Id = opt.Id,
                            VariantAttributeId = opt.VariantAttributeId,
                            Value = opt.Value
                        })
                        .ToList()
                })
                .ToList() ?? new List<ProductVariantViewModel>(),
            Comments = BuildCommentTree(product.Comments),
            RelatedProducts = relatedProducts.Select(MapToSummary).ToList(),
            NewComment = commentForm,
            Offers = offers,
            SuggestedProductOffers = suggestedProductOffers,
            IsSiteProduct = productEntity is null || string.IsNullOrWhiteSpace(productEntity.SellerId),
            HasBackInStockSubscription = hasBackInStockSubscription,
            Gallery = product.Gallery
                .Select(image => new ProductGalleryItemViewModel
                {
                    Id = image.Id,
                    ImagePath = image.ImagePath,
                    DisplayOrder = image.DisplayOrder
                })
                .ToList(),
            CanAddToCart = canAddToCart
        };
    }

    private static ProductSummaryViewModel MapToSummary(Models.Product.Product product)
    {
        return new ProductSummaryViewModel
        {
            Id = product.Id,
            Slug = product.Slug,
            Name = product.Name,
            ShortDescription = product.ShortDescription,
            Price = product.Price,
            OriginalPrice = product.OriginalPrice,
            IsCustomOrder = product.IsCustomOrder,
            ThumbnailUrl = product.ThumbnailUrl,
            Category = product.Category,
            DeliveryFormat = product.DeliveryFormat,
            Rating = product.Rating,
            ReviewCount = product.ReviewCount,
            Badge = product.Duration,
            Tags = product.Tags
        };
    }

    private static IReadOnlyList<ProductCommentViewModel> BuildCommentTree(IReadOnlyList<ProductComment> comments)
    {
        if (comments.Count == 0)
        {
            return Array.Empty<ProductCommentViewModel>();
        }

        var grouped = comments
            .GroupBy(comment => comment.ParentId ?? Guid.Empty)
            .ToDictionary(group => group.Key, group => group.ToList());

        List<ProductCommentViewModel> BuildLevel(Guid parentId)
        {
            if (!grouped.TryGetValue(parentId, out var children))
            {
                return new List<ProductCommentViewModel>();
            }

            return children
                .OrderByDescending(comment => comment.CreatedAt)
                .Select(comment => new ProductCommentViewModel
                {
                    Id = comment.Id,
                    ParentId = comment.ParentId,
                    AuthorName = comment.AuthorName,
                    Content = comment.Content,
                    CreatedAt = comment.CreatedAt,
                    Rating = comment.Rating,
                    Replies = BuildLevel(comment.Id)
                })
                .ToList();
        }

        return BuildLevel(Guid.Empty);
    }

    [HttpPost("/product/{slug}/violation-report")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ReportViolation(
        string slug,
        [FromForm] ProductViolationReportFormModel model,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(slug))
        {
            return NotFound();
        }

        var product = await _productCatalogService.GetBySlugAsync(slug.Trim(), cancellationToken);
        if (product is null)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            TempData["ViolationReportError"] = "لطفاً تمام فیلدهای الزامی را پر کنید.";
            return RedirectToAction(nameof(Details), new { slug });
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var command = new CreateProductViolationReportCommand(
            product.Id,
            model.Subject!,
            model.Message!,
            model.ReporterPhone!,
            model.ProductOfferId,
            string.IsNullOrWhiteSpace(model.SellerId) ? null : model.SellerId,
            userId);

        var result = await _mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
        {
            TempData["ViolationReportError"] = result.Error ?? "ثبت گزارش تخلف با خطا مواجه شد.";
        }
        else
        {
            TempData["ViolationReportSuccess"] = "گزارش تخلف شما با موفقیت ثبت شد. تیم ما در اسرع وقت آن را بررسی خواهد کرد.";
        }

        return RedirectToAction(nameof(Details), new { slug });
    }
}
