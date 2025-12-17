using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using LogTableRenameTest.Application.Commands.Blogs;
using LogTableRenameTest.Application.DTOs;
using LogTableRenameTest.Application.DTOs.Blogs;
using LogTableRenameTest.Application.Interfaces;
using LogTableRenameTest.Application.Queries.Blogs;
using LogTableRenameTest.Application.Queries.Identity.GetUsers;
using LogTableRenameTest.Domain.Enums;
using LogTableRenameTest.WebSite.Areas.Admin.Models;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace LogTableRenameTest.WebSite.Areas.Admin.Controllers;

[Area("Admin")]
public sealed class BlogController : Controller
{
    private const string ContentUploadFolder = "blogs/content";
    private const int MaxEditorImageSizeKb = 5 * 1024;
    private const string FeaturedUploadFolder = "blogs/featured";
    private const int MaxFeaturedImageSizeKb = 5 * 1024;
    private const int MaxTagFieldLength = 1000;
    private const string DefaultRobots = "index,follow";
    private static readonly HashSet<string> AllowedImageContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/png",
        "image/jpeg",
        "image/webp",
        "image/gif"
    };
    private static readonly string[] RobotsOptions =
    {
        "index,follow",
        "index,nofollow",
        "noindex,follow",
        "noindex,nofollow"
    };

    private static readonly char[] TagSeparators = { ',', '،', ';', '|', '\n', '\r' };

    private readonly IMediator _mediator;
    private readonly IFormFileSettingServices _fileSettingServices;

    public BlogController(IMediator mediator, IFormFileSettingServices fileSettingServices)
    {
        _mediator = mediator;
        _fileSettingServices = fileSettingServices;
    }

    [HttpGet]
    public async Task<IActionResult> Index([FromQuery] BlogIndexRequest? request)
    {
        request ??= new BlogIndexRequest();
        var cancellationToken = HttpContext.RequestAborted;

        var lookupsResult = await _mediator.Send(new GetBlogLookupsQuery(), cancellationToken);
        if (!lookupsResult.IsSuccess && !string.IsNullOrWhiteSpace(lookupsResult.Error))
        {
            TempData["Error"] = lookupsResult.Error;
        }

        var listResult = await _mediator.Send(new GetBlogListQuery(
            request.Search,
            request.CategoryId,
            request.AuthorId,
            request.Status,
            request.FromDate,
            request.ToDate,
            request.Page,
            request.PageSize), cancellationToken);

        if (!listResult.IsSuccess && !string.IsNullOrWhiteSpace(listResult.Error))
        {
            TempData["Error"] = listResult.Error;
        }

        var lookups = lookupsResult.IsSuccess && lookupsResult.Value is not null
            ? lookupsResult.Value
            : new BlogLookupsDto(Array.Empty<BlogCategoryDto>(), Array.Empty<BlogAuthorDto>());

        var list = listResult.IsSuccess && listResult.Value is not null
            ? listResult.Value
            : new BlogListResultDto(
                Array.Empty<BlogListItemDto>(),
                0,
                0,
                request.Page <= 0 ? 1 : request.Page,
                request.PageSize <= 0 ? 10 : request.PageSize,
                1,
                new BlogStatisticsDto(0, 0, 0, 0, 0, 0, 0, 0));

        var filters = new BlogIndexFilterViewModel
        {
            Search = request.Search,
            CategoryId = request.CategoryId,
            AuthorId = request.AuthorId,
            Status = request.Status,
            FromDate = request.FromDate,
            ToDate = request.ToDate
        };

        var blogs = list.Items
            .Select(MapListItem)
            .ToList();

        var firstItemIndex = list.FilteredCount == 0 ? 0 : ((list.PageNumber - 1) * list.PageSize) + 1;
        var lastItemIndex = list.FilteredCount == 0 ? 0 : firstItemIndex + blogs.Count - 1;

        var viewModel = new BlogIndexViewModel
        {
            Blogs = blogs,
            Statistics = MapStatistics(list.Statistics),
            Filters = filters,
            CategoryOptions = BuildCategoryOptions(lookups.Categories ?? Array.Empty<BlogCategoryDto>(), request.CategoryId, includeAllOption: true),
            AuthorOptions = BuildAuthorOptions(lookups.Authors ?? Array.Empty<BlogAuthorDto>(), request.AuthorId, includeAllOption: true),
            StatusOptions = BuildStatusOptions(request.Status, includeAllOption: true),
            TotalCount = list.TotalCount,
            FilteredCount = list.FilteredCount,
            PageNumber = list.PageNumber,
            PageSize = list.PageSize,
            TotalPages = list.TotalPages,
            FirstItemIndex = firstItemIndex,
            LastItemIndex = lastItemIndex
        };

        ViewData["Title"] = "مدیریت وبلاگ";
        ViewData["Subtitle"] = "لیست مقالات، فیلتر و آمار عملکرد";

        return View(viewModel);
    }

    [HttpGet]
    public async Task<IActionResult> Comments(Guid id)
    {
        if (id == Guid.Empty)
        {
            TempData["Error"] = "شناسه بلاگ معتبر نیست.";
            return RedirectToAction(nameof(Index));
        }

        var cancellationToken = HttpContext.RequestAborted;
        var result = await _mediator.Send(new GetBlogCommentsQuery(id), cancellationToken);

        if (!result.IsSuccess || result.Value is null)
        {
            TempData["Error"] = result.Error ?? "امکان بارگذاری نظرات این بلاگ وجود ندارد.";
            return RedirectToAction(nameof(Index));
        }

        var viewModel = MapCommentList(result.Value);

        ViewData["Title"] = "مدیریت نظرات بلاگ";
        ViewData["Subtitle"] = $"رسیدگی به دیدگاه‌های ثبت شده برای «{viewModel.BlogTitle}»";

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ModerateComment(Guid id, Guid commentId, bool approve)
    {
        if (id == Guid.Empty || commentId == Guid.Empty)
        {
            TempData["Error"] = "درخواست ارسال شده معتبر نیست.";
            return RedirectToAction(nameof(Index));
        }

        var cancellationToken = HttpContext.RequestAborted;
        var result = await _mediator.Send(new SetBlogCommentApprovalCommand(id, commentId, approve), cancellationToken);

        if (!result.IsSuccess)
        {
            TempData["Error"] = result.Error ?? "امکان بروزرسانی وضعیت نظر وجود ندارد.";
        }
        else
        {
            TempData["Success"] = approve
                ? "نظر مورد نظر با موفقیت تایید شد."
                : "وضعیت نظر به رد شده تغییر کرد.";
        }

        return RedirectToAction(nameof(Comments), new { id });
    }

    [HttpGet]
    public async Task<IActionResult> Categories()
    {
        var lookups = await LoadLookupsAsync(HttpContext.RequestAborted);
        var categories = BuildCategoryManagementViewModel(lookups.Categories ?? Array.Empty<BlogCategoryDto>());

        ViewData["Title"] = "مدیریت دسته‌بندی‌های وبلاگ";
        ViewData["Subtitle"] = "ایجاد، ویرایش و سازمان‌دهی ساختار سلسله‌مراتبی دسته‌بندی‌ها";

        return View("Categories", categories);
    }

    [HttpGet]
    public async Task<IActionResult> Authors()
    {
        var cancellationToken = HttpContext.RequestAborted;

        var authorsResult = await _mediator.Send(new GetBlogAuthorsQuery(), cancellationToken);
        if (!authorsResult.IsSuccess && !string.IsNullOrWhiteSpace(authorsResult.Error))
        {
            TempData["Error"] = authorsResult.Error;
        }

        var authors = authorsResult.IsSuccess && authorsResult.Value is not null
            ? authorsResult.Value
            : new BlogAuthorListResultDto(Array.Empty<BlogAuthorListItemDto>(), 0, 0);

        var usersResult = await _mediator.Send(new GetUsersQuery(new UserFilterCriteria(
            IncludeDeactivated: false,
            IncludeDeleted: false,
            FullName: null,
            PhoneNumber: null,
            Role: null,
            Status: UserStatusFilter.Active,
            RegisteredFrom: null,
            RegisteredTo: null)), cancellationToken);

        if (!usersResult.IsSuccess && !string.IsNullOrWhiteSpace(usersResult.Error))
        {
            TempData["Error"] = usersResult.Error;
        }

        var users = usersResult.IsSuccess && usersResult.Value is not null
            ? usersResult.Value
            : Array.Empty<UserDto>();

        var assignedUserIds = authors.Authors
            .Where(author => !string.IsNullOrWhiteSpace(author.UserId))
            .Select(author => author.UserId!)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var viewModel = new BlogAuthorsViewModel
        {
            Authors = authors.Authors.Select(MapAuthor).ToArray(),
            UserOptions = BuildAuthorUserOptions(users, assignedUserIds),
            TotalCount = authors.Authors.Count,
            ActiveCount = authors.ActiveCount,
            InactiveCount = authors.InactiveCount
        };

        ViewData["Title"] = "مدیریت نویسندگان وبلاگ";
        ViewData["Subtitle"] = "تعریف نویسندگان، اتصال به کاربران و مدیریت وضعیت";

        return View("Authors", viewModel);
    }

    [HttpGet]
    public async Task<IActionResult> AuthorOptions()
    {
        var cancellationToken = HttpContext.RequestAborted;
        var lookupsResult = await _mediator.Send(new GetBlogLookupsQuery(), cancellationToken);

        if (!lookupsResult.IsSuccess || lookupsResult.Value is null)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                error = lookupsResult.Error ?? "امکان بارگذاری فهرست نویسندگان وجود ندارد."
            });
        }

        var authors = lookupsResult.Value.Authors
            ?.Where(author => author.IsActive)
            .OrderBy(author => author.DisplayName, StringComparer.CurrentCulture)
            .Select(author => new
            {
                id = author.Id,
                name = author.DisplayName
            })
            .ToArray() ?? Array.Empty<object>();

        return Json(new { authors });
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var lookups = await LoadLookupsAsync(HttpContext.RequestAborted);
        var model = new BlogFormViewModel
        {
            ReadingTimeMinutes = 5,
            Status = BlogStatus.Draft,
            Robots = DefaultRobots,
            PublishedAtPersian = string.Empty,
            PublishedAtTime = string.Empty,
            TagItems = Array.Empty<string>(),
            Selections = BuildFormSelections(lookups, BlogStatus.Draft, DefaultRobots)
        };

        PopulatePublishedAtFields(model);
        PrepareFormView("افزودن بلاگ جدید", "ایجاد و انتشار مقاله تازه");
        return View("Form", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(BlogFormViewModel model)
    {
        var cancellationToken = HttpContext.RequestAborted;
        var normalizedRobots = NormalizeRobots(model.Robots);
        var parsedTags = NormalizeTags(model);

        model.Robots = normalizedRobots;

        ModelState.Remove(nameof(BlogFormViewModel.PublishedAt));
        if (!TryConvertPublishedAt(model, out DateTimeOffset? convertedPublishedAt, out var publishError))
        {
            ModelState.AddModelError(nameof(BlogFormViewModel.PublishedAt), publishError ?? "تاریخ انتشار وارد شده معتبر نیست.");
        }
        else
        {
            model.PublishedAt = convertedPublishedAt;
        }

        if (!IsRobotsOptionValid(normalizedRobots))
        {
            ModelState.AddModelError(nameof(BlogFormViewModel.Robots), "گزینه انتخاب شده معتبر نیست.");
        }

        ValidateFeaturedImage(model.FeaturedImage, nameof(BlogFormViewModel.FeaturedImage));

        if (!ModelState.IsValid)
        {
            var selections = await LoadLookupsAsync(cancellationToken);
            model.Selections = BuildFormSelections(selections, model.Status, normalizedRobots);
            PopulatePublishedAtFields(model);
            PrepareFormView("افزودن بلاگ جدید", "ایجاد و انتشار مقاله تازه");
            return View("Form", model);
        }

        string? uploadedImagePath = null;
        if (model.FeaturedImage is not null)
        {
            uploadedImagePath = SaveFeaturedImage(model.FeaturedImage, nameof(BlogFormViewModel.FeaturedImage));
        }

        if (!ModelState.IsValid)
        {
            var selections = await LoadLookupsAsync(cancellationToken);
            model.Selections = BuildFormSelections(selections, model.Status, normalizedRobots);
            PopulatePublishedAtFields(model);
            PrepareFormView("افزودن بلاگ جدید", "ایجاد و انتشار مقاله تازه");
            if (!string.IsNullOrWhiteSpace(uploadedImagePath))
            {
                _fileSettingServices.DeleteFile(uploadedImagePath);
            }

            return View("Form", model);
        }

        if (model.CategoryId is null || model.AuthorId is null)
        {
            ModelState.AddModelError(string.Empty, "لطفاً دسته‌بندی و نویسنده را مشخص کنید.");
            var selections = await LoadLookupsAsync(cancellationToken);
            model.Selections = BuildFormSelections(selections, model.Status, normalizedRobots);
            PopulatePublishedAtFields(model);
            PrepareFormView("افزودن بلاگ جدید", "ایجاد و انتشار مقاله تازه");

            if (!string.IsNullOrWhiteSpace(uploadedImagePath))
            {
                _fileSettingServices.DeleteFile(uploadedImagePath);
            }

            return View("Form", model);
        }

        var categoryId = model.CategoryId.Value;
        var authorId = model.AuthorId.Value;
        var publishedAt = model.Status == BlogStatus.Published ? convertedPublishedAt : null;
        var seoSlug = string.IsNullOrWhiteSpace(model.SeoSlug) ? GenerateSlug(model.Title) : model.SeoSlug;

        var command = new CreateBlogCommand(
            model.Title,
            model.Summary,
            model.Content,
            categoryId,
            authorId,
            model.Status,
            model.ReadingTimeMinutes,
            publishedAt,
            model.SeoTitle,
            model.SeoDescription,
            model.SeoKeywords,
            seoSlug,
            uploadedImagePath,
            normalizedRobots,
            parsedTags);

        var result = await _mediator.Send(command, cancellationToken);
        if (!result.IsSuccess)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "امکان ثبت بلاگ وجود ندارد.");
            var selections = await LoadLookupsAsync(cancellationToken);
            model.Selections = BuildFormSelections(selections, model.Status, normalizedRobots);
            PopulatePublishedAtFields(model);
            PrepareFormView("افزودن بلاگ جدید", "ایجاد و انتشار مقاله تازه");

            if (!string.IsNullOrWhiteSpace(uploadedImagePath))
            {
                _fileSettingServices.DeleteFile(uploadedImagePath);
            }

            return View("Form", model);
        }

        TempData["Success"] = "بلاگ جدید با موفقیت ایجاد شد.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(Guid id)
    {
        if (id == Guid.Empty)
        {
            TempData["Error"] = "شناسه بلاگ معتبر نیست.";
            return RedirectToAction(nameof(Index));
        }

        var cancellationToken = HttpContext.RequestAborted;
        var detailResult = await _mediator.Send(new GetBlogDetailQuery(id), cancellationToken);
        if (!detailResult.IsSuccess || detailResult.Value is null)
        {
            TempData["Error"] = detailResult.Error ?? "بلاگ مورد نظر یافت نشد.";
            return RedirectToAction(nameof(Index));
        }

        var lookups = await LoadLookupsAsync(cancellationToken);
        var dto = detailResult.Value;
        var normalizedRobots = NormalizeRobots(dto.Robots);
        var tagItems = ParseTags(dto.TagList);
        var model = new BlogFormViewModel
        {
            Id = dto.Id,
            Title = dto.Title,
            Summary = dto.Summary,
            Content = dto.Content,
            CategoryId = dto.CategoryId,
            AuthorId = dto.AuthorId,
            ReadingTimeMinutes = dto.ReadingTimeMinutes,
            Status = dto.Status,
            PublishedAt = dto.PublishedAt,
            SeoTitle = dto.SeoTitle,
            SeoDescription = dto.SeoDescription,
            SeoKeywords = dto.SeoKeywords,
            SeoSlug = dto.SeoSlug,
            FeaturedImagePath = dto.FeaturedImagePath,
            Robots = normalizedRobots,
            Tags = string.Join(", ", tagItems),
            TagItems = tagItems,
            Selections = BuildFormSelections(lookups, dto.Status, normalizedRobots)
        };

        PopulatePublishedAtFields(model);
        PrepareFormView("ویرایش بلاگ", "به‌روزرسانی اطلاعات و تنظیمات سئو");
        return View("Form", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, BlogFormViewModel model)
    {
        if (id == Guid.Empty)
        {
            TempData["Error"] = "شناسه بلاگ معتبر نیست.";
            return RedirectToAction(nameof(Index));
        }

        var cancellationToken = HttpContext.RequestAborted;
        var normalizedRobots = NormalizeRobots(model.Robots);
        var parsedTags = NormalizeTags(model);

        model.Robots = normalizedRobots;

        ModelState.Remove(nameof(BlogFormViewModel.PublishedAt));
        if (!TryConvertPublishedAt(model, out DateTimeOffset? convertedPublishedAt, out var publishError))
        {
            ModelState.AddModelError(nameof(BlogFormViewModel.PublishedAt), publishError ?? "تاریخ انتشار وارد شده معتبر نیست.");
        }
        else
        {
            model.PublishedAt = convertedPublishedAt;
        }

        if (!IsRobotsOptionValid(normalizedRobots))
        {
            ModelState.AddModelError(nameof(BlogFormViewModel.Robots), "گزینه انتخاب شده معتبر نیست.");
        }

        ValidateFeaturedImage(model.FeaturedImage, nameof(BlogFormViewModel.FeaturedImage));

        if (!ModelState.IsValid)
        {
            var selections = await LoadLookupsAsync(cancellationToken);
            model.Selections = BuildFormSelections(selections, model.Status, normalizedRobots);
            PopulatePublishedAtFields(model);
            PrepareFormView("ویرایش بلاگ", "به‌روزرسانی اطلاعات و تنظیمات سئو");
            return View("Form", model);
        }

        var previousImagePath = model.FeaturedImagePath;
        string? uploadedImagePath = null;
        if (model.FeaturedImage is not null)
        {
            uploadedImagePath = SaveFeaturedImage(model.FeaturedImage, nameof(BlogFormViewModel.FeaturedImage));
        }

        if (!ModelState.IsValid)
        {
            var selections = await LoadLookupsAsync(cancellationToken);
            model.Selections = BuildFormSelections(selections, model.Status, normalizedRobots);
            PopulatePublishedAtFields(model);
            PrepareFormView("ویرایش بلاگ", "به‌روزرسانی اطلاعات و تنظیمات سئو");

            if (!string.IsNullOrWhiteSpace(uploadedImagePath))
            {
                _fileSettingServices.DeleteFile(uploadedImagePath);
                model.FeaturedImagePath = previousImagePath;
            }

            return View("Form", model);
        }

        var nextImagePath = model.RemoveFeaturedImage ? null : (uploadedImagePath ?? previousImagePath);
        model.FeaturedImagePath = nextImagePath;

        if (model.CategoryId is null || model.AuthorId is null)
        {
            ModelState.AddModelError(string.Empty, "لطفاً دسته‌بندی و نویسنده را مشخص کنید.");
            var selections = await LoadLookupsAsync(cancellationToken);
            model.Selections = BuildFormSelections(selections, model.Status, normalizedRobots);
            PopulatePublishedAtFields(model);
            PrepareFormView("ویرایش بلاگ", "به‌روزرسانی اطلاعات و تنظیمات سئو");

            if (!string.IsNullOrWhiteSpace(uploadedImagePath))
            {
                _fileSettingServices.DeleteFile(uploadedImagePath);
                model.FeaturedImagePath = previousImagePath;
            }

            return View("Form", model);
        }

        var categoryId = model.CategoryId.Value;
        var authorId = model.AuthorId.Value;
        var publishedAt = model.Status == BlogStatus.Published ? convertedPublishedAt : null;
        var seoSlug = string.IsNullOrWhiteSpace(model.SeoSlug) ? GenerateSlug(model.Title) : model.SeoSlug;

        var command = new UpdateBlogCommand(
            id,
            model.Title,
            model.Summary,
            model.Content,
            categoryId,
            authorId,
            model.Status,
            model.ReadingTimeMinutes,
            publishedAt,
            model.SeoTitle,
            model.SeoDescription,
            model.SeoKeywords,
            seoSlug,
            nextImagePath,
            normalizedRobots,
            parsedTags);

        var result = await _mediator.Send(command, cancellationToken);
        if (!result.IsSuccess)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "امکان ویرایش بلاگ وجود ندارد.");
            var selections = await LoadLookupsAsync(cancellationToken);
            model.Selections = BuildFormSelections(selections, model.Status, normalizedRobots);
            PopulatePublishedAtFields(model);
            PrepareFormView("ویرایش بلاگ", "به‌روزرسانی اطلاعات و تنظیمات سئو");

            if (!string.IsNullOrWhiteSpace(uploadedImagePath))
            {
                _fileSettingServices.DeleteFile(uploadedImagePath);
                model.FeaturedImagePath = previousImagePath;
            }

            return View("Form", model);
        }

        if (!string.IsNullOrWhiteSpace(previousImagePath))
        {
            var replacedWithNew = !string.IsNullOrWhiteSpace(uploadedImagePath) &&
                !string.Equals(previousImagePath, uploadedImagePath, StringComparison.OrdinalIgnoreCase);

            if (model.RemoveFeaturedImage || replacedWithNew)
            {
                _fileSettingServices.DeleteFile(previousImagePath);
            }
        }

        TempData["Success"] = "تغییرات بلاگ با موفقیت ذخیره شد.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id)
    {
        if (id == Guid.Empty)
        {
            TempData["Error"] = "شناسه بلاگ معتبر نیست.";
            return RedirectToAction(nameof(Index));
        }

        var result = await _mediator.Send(new DeleteBlogCommand(id), HttpContext.RequestAborted);
        if (!result.IsSuccess)
        {
            TempData["Error"] = result.Error ?? "امکان حذف بلاگ وجود ندارد.";
        }
        else
        {
            TempData["Success"] = "بلاگ انتخاب‌شده به زباله‌دان منتقل شد.";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateAuthor(BlogAuthorFormModel model)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = ExtractFirstValidationError(ModelState);
            return RedirectToAction(nameof(Authors));
        }

        var command = new CreateBlogAuthorCommand(model.DisplayName, model.Bio, model.AvatarUrl, model.IsActive, model.UserId);
        var result = await _mediator.Send(command, HttpContext.RequestAborted);

        if (!result.IsSuccess)
        {
            TempData["Error"] = result.Error ?? "امکان ثبت نویسنده وجود ندارد.";
        }
        else
        {
            TempData["Success"] = "نویسنده جدید با موفقیت ایجاد شد.";
        }

        return RedirectToAction(nameof(Authors));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateAuthor(BlogAuthorUpdateFormModel model)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = ExtractFirstValidationError(ModelState);
            return RedirectToAction(nameof(Authors));
        }

        var command = new UpdateBlogAuthorCommand(model.Id, model.DisplayName, model.Bio, model.AvatarUrl, model.IsActive, model.UserId);
        var result = await _mediator.Send(command, HttpContext.RequestAborted);

        if (!result.IsSuccess)
        {
            TempData["Error"] = result.Error ?? "امکان ویرایش نویسنده وجود ندارد.";
        }
        else
        {
            TempData["Success"] = "تغییرات نویسنده با موفقیت ذخیره شد.";
        }

        return RedirectToAction(nameof(Authors));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteAuthor(Guid id)
    {
        if (id == Guid.Empty)
        {
            TempData["Error"] = "شناسه نویسنده معتبر نیست.";
            return RedirectToAction(nameof(Authors));
        }

        var result = await _mediator.Send(new DeleteBlogAuthorCommand(id), HttpContext.RequestAborted);

        if (!result.IsSuccess)
        {
            TempData["Error"] = result.Error ?? "امکان حذف نویسنده وجود ندارد.";
        }
        else
        {
            TempData["Success"] = "نویسنده موردنظر حذف شد.";
        }

        return RedirectToAction(nameof(Authors));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateCategory(BlogCategoryFormModel model)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = ExtractFirstValidationError(ModelState);
            return RedirectToAction(nameof(Categories));
        }

        var command = new CreateBlogCategoryCommand(model.Name, model.Slug, model.Description, model.ParentId);
        var result = await _mediator.Send(command, HttpContext.RequestAborted);

        if (!result.IsSuccess)
        {
            TempData["Error"] = result.Error ?? "امکان ثبت دسته‌بندی وجود ندارد.";
        }
        else
        {
            TempData["Success"] = "دسته‌بندی جدید با موفقیت ایجاد شد.";
        }

        return RedirectToAction(nameof(Categories));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateCategory(BlogCategoryUpdateFormModel model)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = ExtractFirstValidationError(ModelState);
            return RedirectToAction(nameof(Categories));
        }

        var command = new UpdateBlogCategoryCommand(model.Id, model.Name, model.Slug, model.Description, model.ParentId);
        var result = await _mediator.Send(command, HttpContext.RequestAborted);

        if (!result.IsSuccess)
        {
            TempData["Error"] = result.Error ?? "امکان ویرایش دسته‌بندی وجود ندارد.";
        }
        else
        {
            TempData["Success"] = "تغییرات دسته‌بندی با موفقیت ذخیره شد.";
        }

        return RedirectToAction(nameof(Categories));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteCategory(Guid id)
    {
        if (id == Guid.Empty)
        {
            TempData["Error"] = "شناسه دسته‌بندی معتبر نیست.";
            return RedirectToAction(nameof(Categories));
        }

        var result = await _mediator.Send(new DeleteBlogCategoryCommand(id), HttpContext.RequestAborted);

        if (!result.IsSuccess)
        {
            TempData["Error"] = result.Error ?? "امکان حذف دسته‌بندی وجود ندارد.";
        }
        else
        {
            TempData["Success"] = "دسته‌بندی موردنظر حذف شد.";
        }

        return RedirectToAction(nameof(Categories));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult UploadContentImage(IFormFile? file)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest(new { error = "فایلی برای آپلود ارسال نشده است." });
        }

        if (!_fileSettingServices.IsFileSizeValid(file, MaxEditorImageSizeKb))
        {
            return BadRequest(new { error = "حجم تصویر باید کمتر از ۵ مگابایت باشد." });
        }

        var contentType = file.ContentType ?? string.Empty;
        if (!AllowedImageContentTypes.Contains(contentType))
        {
            return BadRequest(new { error = "فرمت تصویر پشتیبانی نمی‌شود." });
        }

        var response = _fileSettingServices.UploadImage(ContentUploadFolder, file, Guid.NewGuid().ToString("N"));
        if (!response.Success || string.IsNullOrWhiteSpace(response.Data))
        {
            return BadRequest(new { error = response.Messages.FirstOrDefault()?.message ?? "امکان ذخیره‌سازی تصویر وجود ندارد." });
        }

        var normalizedPath = response.Data.Replace("\\", "/");
        if (!normalizedPath.StartsWith('/'))
        {
            normalizedPath = "/" + normalizedPath;
        }

        return Json(new { url = normalizedPath });
    }

    private static void PopulatePublishedAtFields(BlogFormViewModel model)
    {
        if (model is null)
        {
            return;
        }

        if (model.PublishedAt.HasValue)
        {
            var local = model.PublishedAt.Value.ToLocalTime();
            var calendar = new PersianCalendar();
            var persianDate = string.Format(
                CultureInfo.InvariantCulture,
                "{0:0000}-{1:00}-{2:00}",
                calendar.GetYear(local.DateTime),
                calendar.GetMonth(local.DateTime),
                calendar.GetDayOfMonth(local.DateTime));

            if (string.IsNullOrWhiteSpace(model.PublishedAtPersian))
            {
                model.PublishedAtPersian = persianDate;
            }

            if (string.IsNullOrWhiteSpace(model.PublishedAtTime))
            {
                model.PublishedAtTime = local.ToString("HH:mm", CultureInfo.InvariantCulture);
            }
        }
        else
        {
            if (string.IsNullOrWhiteSpace(model.PublishedAtPersian))
            {
                model.PublishedAtPersian = string.Empty;
            }

            if (string.IsNullOrWhiteSpace(model.PublishedAtTime))
            {
                model.PublishedAtTime = string.Empty;
            }
        }
    }

    private static bool TryConvertPublishedAt(
        BlogFormViewModel model,
        out DateTimeOffset? publishedAt,
        out string? errorMessage)
    {
        publishedAt = null;
        errorMessage = null;

        if (model is null)
        {
            return true;
        }

        var rawDateInput = model.PublishedAtPersian;
        var normalizedDate = NormalizePersianDateInput(rawDateInput);
        var dateProvided = !string.IsNullOrWhiteSpace(rawDateInput);
        model.PublishedAtPersian = normalizedDate;

        var rawTimeInput = model.PublishedAtTime;
        if (!TryNormalizeTimeInput(rawTimeInput, out var hour, out var minute, out var normalizedTime, out var timeError))
        {
            model.PublishedAtTime = normalizedTime;
            errorMessage = timeError ?? "ساعت انتشار وارد شده معتبر نیست.";
            return false;
        }

        model.PublishedAtTime = normalizedTime;

        if (model.Status != BlogStatus.Published)
        {
            return true;
        }

        if (string.IsNullOrEmpty(normalizedDate))
        {
            if (dateProvided || !string.IsNullOrWhiteSpace(rawTimeInput))
            {
                errorMessage = "تاریخ انتشار وارد شده معتبر نیست.";
                return false;
            }

            return true;
        }

        if (!TryExtractPersianDateParts(normalizedDate, out var year, out var month, out var day))
        {
            errorMessage = "تاریخ انتشار وارد شده معتبر نیست.";
            return false;
        }

        global::PersianDateTime persianDateTime;

        try
        {
            persianDateTime = new global::PersianDateTime(year, month, day, hour, minute, 0);
        }
        catch (ArgumentOutOfRangeException)
        {
            errorMessage = "تاریخ انتشار وارد شده معتبر نیست.";
            return false;
        }
        catch (Exception)
        {
            errorMessage = "تاریخ انتشار وارد شده معتبر نیست.";
            return false;
        }

        var gregorian = persianDateTime.ToDateTime();
        var offset = GetIranOffset(gregorian);
        publishedAt = new DateTimeOffset(DateTime.SpecifyKind(gregorian, DateTimeKind.Unspecified), offset);

        return true;
    }

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

        return year > 0 && month is >= 1 and <= 12 && day is >= 1 and <= 31;
    }

    private static bool TryNormalizeTimeInput(
        string? value,
        out int hour,
        out int minute,
        out string normalizedValue,
        out string? errorMessage)
    {
        hour = 0;
        minute = 0;
        normalizedValue = string.Empty;
        errorMessage = null;

        if (string.IsNullOrWhiteSpace(value))
        {
            return true;
        }

        var sanitized = NormalizeDigits(value)
            .Replace("\u200f", string.Empty, StringComparison.Ordinal)
            .Replace("\u200e", string.Empty, StringComparison.Ordinal)
            .Replace(" ", string.Empty, StringComparison.Ordinal)
            .Replace(".", ":", StringComparison.Ordinal)
            .Replace("-", ":", StringComparison.Ordinal)
            .Trim();

        if (sanitized.Length is 3 or 4 && !sanitized.Contains(':', StringComparison.Ordinal))
        {
            var insertIndex = sanitized.Length - 2;
            sanitized = sanitized.Insert(insertIndex, ":");
        }

        if (!TimeSpan.TryParseExact(
                sanitized,
                new[] { "hh\\:mm", "h\\:mm", "HH\\:mm", "H\\:mm" },
                CultureInfo.InvariantCulture,
                out var timeSpan))
        {
            errorMessage = "ساعت انتشار وارد شده معتبر نیست.";
            return false;
        }

        if (timeSpan.TotalHours >= 24)
        {
            errorMessage = "ساعت انتشار وارد شده معتبر نیست.";
            return false;
        }

        hour = timeSpan.Hours;
        minute = timeSpan.Minutes;
        normalizedValue = string.Format(CultureInfo.InvariantCulture, "{0:00}:{1:00}", hour, minute);

        return true;
    }

    private static string NormalizeDigits(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        var builder = new StringBuilder(value.Length);
        foreach (var ch in value)
        {
            builder.Append(ch switch
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
                _ => ch
            });
        }

        return builder.ToString();
    }

    private static BlogCommentListViewModel MapCommentList(BlogCommentListResultDto dto)
    {
        var comments = dto.Comments?.ToArray() ?? Array.Empty<BlogCommentDto>();
        var commentLookup = comments.ToDictionary(comment => comment.Id, comment => comment);

        static string? BuildExcerpt(string? content)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                return null;
            }

            var normalized = content.Trim();
            if (normalized.Length <= 80)
            {
                return normalized;
            }

            return normalized[..77] + "…";
        }

        var items = comments
            .Select(comment =>
            {
                BlogCommentDto? parent = null;
                if (comment.ParentId.HasValue)
                {
                    commentLookup.TryGetValue(comment.ParentId.Value, out parent);
                }

                return new BlogCommentItemViewModel
                {
                    Id = comment.Id,
                    ParentId = comment.ParentId,
                    AuthorName = comment.AuthorName,
                    AuthorEmail = comment.AuthorEmail,
                    Content = comment.Content,
                    IsApproved = comment.IsApproved,
                    CreatedAt = comment.CreatedAt,
                    UpdatedAt = comment.UpdatedAt,
                    ApprovedByName = comment.ApprovedByName,
                    ApprovedAt = comment.ApprovedAt,
                    ParentAuthorName = parent?.AuthorName,
                    ParentExcerpt = BuildExcerpt(parent?.Content)
                };
            })
            .OrderByDescending(comment => comment.CreatedAt)
            .ToArray();

        var approvedCount = items.Count(comment => comment.IsApproved);

        return new BlogCommentListViewModel
        {
            BlogId = dto.Blog.Id,
            BlogTitle = dto.Blog.Title,
            BlogSlug = dto.Blog.SeoSlug,
            TotalCount = items.Length,
            ApprovedCount = approvedCount,
            PendingCount = items.Length - approvedCount,
            Comments = items
        };
    }

    private static TimeSpan GetIranOffset(DateTime dateTime)
    {
        foreach (var timeZoneId in new[] { "Iran Standard Time", "Asia/Tehran" })
        {
            try
            {
                var timeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
                return timeZone.GetUtcOffset(dateTime);
            }
            catch (TimeZoneNotFoundException)
            {
                // Try next identifier.
            }
            catch (InvalidTimeZoneException)
            {
                // Try next identifier.
            }
        }

        return TimeSpan.FromHours(3.5);
    }

    private async Task<BlogLookupsDto> LoadLookupsAsync(CancellationToken cancellationToken)
    {
        var lookups = await _mediator.Send(new GetBlogLookupsQuery(), cancellationToken);
        if (!lookups.IsSuccess)
        {
            TempData["Error"] = lookups.Error ?? "امکان بارگذاری داده‌های کمکی وجود ندارد.";
            return new BlogLookupsDto(Array.Empty<BlogCategoryDto>(), Array.Empty<BlogAuthorDto>());
        }

        return lookups.Value ?? new BlogLookupsDto(Array.Empty<BlogCategoryDto>(), Array.Empty<BlogAuthorDto>());
    }

    private static BlogListItemViewModel MapListItem(BlogListItemDto dto)
    {
        var robots = NormalizeRobots(dto.Robots);
        var tags = ParseTags(dto.TagList);

        return new BlogListItemViewModel(
            dto.Id,
            dto.Title,
            dto.Category,
            dto.CategoryId,
            dto.Author,
            dto.AuthorId,
            dto.Status,
            dto.PublishedAt,
            dto.ReadingTimeMinutes,
            dto.LikeCount,
            dto.DislikeCount,
            dto.CommentCount,
            dto.ViewCount,
            dto.UpdatedAt,
            dto.FeaturedImagePath,
            robots,
            tags);
    }

    private static BlogStatisticsViewModel MapStatistics(BlogStatisticsDto dto)
        => new(dto.TotalBlogs, dto.PublishedBlogs, dto.DraftBlogs, dto.TrashBlogs, dto.TotalLikes, dto.TotalDislikes,
            dto.TotalViews, dto.AverageReadingTimeMinutes);

    private static BlogAuthorListItemViewModel MapAuthor(BlogAuthorListItemDto dto)
        => new(dto.Id, dto.DisplayName, dto.Bio, dto.AvatarUrl, dto.IsActive, dto.UserId, dto.UserFullName, dto.UserEmail,
            dto.UserPhoneNumber, dto.CreatedAt, dto.UpdatedAt);

    private static BlogCategoriesViewModel BuildCategoryManagementViewModel(
        IReadOnlyCollection<BlogCategoryDto> categories)
        => new()
        {
            Categories = MapCategoryTree(categories),
            ParentOptions = BuildCategoryParentOptions(categories)
        };

    private static IReadOnlyCollection<BlogCategoryTreeItemViewModel> MapCategoryTree(
        IReadOnlyCollection<BlogCategoryDto> categories)
    {
        if (categories.Count == 0)
        {
            return Array.Empty<BlogCategoryTreeItemViewModel>();
        }

        return categories
            .OrderBy(category => category.Name, StringComparer.CurrentCulture)
            .Select(MapCategoryNode)
            .ToArray();
    }

    private static BlogCategoryTreeItemViewModel MapCategoryNode(BlogCategoryDto category)
    {
        var childDtos = category.Children ?? Array.Empty<BlogCategoryDto>();
        var children = childDtos
            .OrderBy(child => child.Name, StringComparer.CurrentCulture)
            .Select(MapCategoryNode)
            .ToArray();

        var descendantIds = children
            .SelectMany(child => child.DescendantIds.Concat(new[] { child.Id }))
            .Distinct()
            .ToArray();

        return new BlogCategoryTreeItemViewModel
        {
            Id = category.Id,
            Name = category.Name,
            Slug = category.Slug,
            Description = category.Description,
            ParentId = category.ParentId,
            Depth = category.Depth,
            Children = children,
            DescendantIds = descendantIds
        };
    }

    private static IReadOnlyCollection<SelectListItem> BuildCategoryOptions(
        IReadOnlyCollection<BlogCategoryDto> categories,
        Guid? selectedId,
        bool includeAllOption)
    {
        var items = new List<SelectListItem>();
        if (includeAllOption)
        {
            items.Add(new SelectListItem("همه دسته‌بندی‌ها", string.Empty, selectedId is null));
        }

        foreach (var category in categories.OrderBy(c => c.Name, StringComparer.CurrentCulture))
        {
            AppendCategory(items, category, selectedId, 0);
        }

        return items;
    }

    private static IReadOnlyCollection<SelectListItem> BuildCategoryParentOptions(
        IReadOnlyCollection<BlogCategoryDto> categories)
    {
        var items = new List<SelectListItem>
        {
            new("بدون والد", string.Empty, true)
        };

        items.AddRange(BuildCategoryOptions(categories, null, includeAllOption: false));

        return items;
    }

    private static void AppendCategory(List<SelectListItem> items, BlogCategoryDto category, Guid? selectedId, int depth)
    {
        var prefix = depth == 0 ? string.Empty : new string('›', depth).PadLeft(depth * 2, ' ');
        var text = string.IsNullOrWhiteSpace(prefix) ? category.Name : $"{prefix} {category.Name}";
        items.Add(new SelectListItem(text, category.Id.ToString(), selectedId == category.Id));

        if (category.Children is null)
        {
            return;
        }

        foreach (var child in category.Children.OrderBy(c => c.Name, StringComparer.CurrentCulture))
        {
            AppendCategory(items, child, selectedId, depth + 1);
        }
    }

    private static IReadOnlyCollection<SelectListItem> BuildAuthorOptions(
        IReadOnlyCollection<BlogAuthorDto> authors,
        Guid? selectedId,
        bool includeAllOption)
    {
        var items = new List<SelectListItem>();
        if (includeAllOption)
        {
            items.Add(new SelectListItem("همه نویسندگان", string.Empty, selectedId is null));
        }

        foreach (var author in authors.OrderBy(author => author.DisplayName, StringComparer.CurrentCulture))
        {
            items.Add(new SelectListItem(author.DisplayName, author.Id.ToString(), selectedId == author.Id));
        }

        return items;
    }

    private static IReadOnlyCollection<BlogAuthorUserOptionViewModel> BuildAuthorUserOptions(
        IReadOnlyCollection<UserDto> users,
        ISet<string> assignedUserIds)
    {
        if (users.Count == 0)
        {
            return Array.Empty<BlogAuthorUserOptionViewModel>();
        }

        return users
            .OrderBy(user => user.FullName, StringComparer.CurrentCulture)
            .Select(user =>
            {
                var email = string.IsNullOrWhiteSpace(user.Email) ? null : user.Email;
                var phone = string.IsNullOrWhiteSpace(user.PhoneNumber) ? null : user.PhoneNumber;
                var isAssigned = assignedUserIds.Contains(user.Id);
                return new BlogAuthorUserOptionViewModel(user.Id, user.FullName, email, phone, isAssigned);
            })
            .ToArray();
    }

    private static IReadOnlyCollection<SelectListItem> BuildStatusOptions(BlogStatus? selectedStatus, bool includeAllOption)
    {
        var items = new List<SelectListItem>();
        if (includeAllOption)
        {
            items.Add(new SelectListItem("همه وضعیت‌ها", string.Empty, selectedStatus is null));
        }

        foreach (var status in Enum.GetValues<BlogStatus>())
        {
            items.Add(new SelectListItem(GetStatusDisplay(status), status.ToString(), selectedStatus == status));
        }

        return items;
    }

    private static IReadOnlyCollection<SelectListItem> BuildRobotsOptions(string? selectedRobots)
    {
        var selected = NormalizeRobots(selectedRobots);

        return RobotsOptions
            .Select(option => new SelectListItem(
                GetRobotsDisplay(option),
                option,
                string.Equals(option, selected, StringComparison.OrdinalIgnoreCase)))
            .ToArray();
    }

    private static string ExtractFirstValidationError(ModelStateDictionary modelState)
    {
        foreach (var entry in modelState.Values)
        {
            foreach (var error in entry.Errors)
            {
                if (!string.IsNullOrWhiteSpace(error.ErrorMessage))
                {
                    return error.ErrorMessage;
                }
            }
        }

        return "اطلاعات وارد شده معتبر نیست.";
    }

    private static BlogFormSelectionsViewModel BuildFormSelections(
        BlogLookupsDto lookups,
        BlogStatus selectedStatus,
        string? selectedRobots)
        => new()
        {
            CategoryOptions = BuildCategoryOptions(lookups.Categories, null, includeAllOption: false),
            AuthorOptions = BuildAuthorOptions(lookups.Authors.Where(author => author.IsActive).ToArray(), null, includeAllOption: false),
            StatusOptions = BuildStatusOptions(selectedStatus, includeAllOption: false),
            RobotsOptions = BuildRobotsOptions(selectedRobots)
        };

    private static string GetStatusDisplay(BlogStatus status)
        => status switch
        {
            BlogStatus.Published => "منتشر شده",
            BlogStatus.Trash => "زباله‌دان",
            _ => "پیش‌نویس"
        };

    private static string GetRobotsDisplay(string robots)
        => robots switch
        {
            "index,follow" => "ایندکس و دنبال",
            "index,nofollow" => "ایندکس و عدم دنبال",
            "noindex,follow" => "عدم ایندکس و دنبال",
            "noindex,nofollow" => "عدم ایندکس و عدم دنبال",
            _ => robots
        };

    private static string NormalizeRobots(string? robots)
    {
        if (string.IsNullOrWhiteSpace(robots))
        {
            return DefaultRobots;
        }

        var sanitized = robots.Replace(" ", string.Empty, StringComparison.Ordinal).Trim();
        return RobotsOptions.FirstOrDefault(option => string.Equals(option, sanitized, StringComparison.OrdinalIgnoreCase))
            ?? sanitized.ToLowerInvariant();
    }

    private static bool IsRobotsOptionValid(string robots)
        => RobotsOptions.Any(option => string.Equals(option, robots, StringComparison.OrdinalIgnoreCase));

    private void ValidateFeaturedImage(IFormFile? file, string fieldName)
    {
        if (file is null || file.Length == 0)
        {
            return;
        }

        if (!_fileSettingServices.IsFileSizeValid(file, MaxFeaturedImageSizeKb))
        {
            ModelState.AddModelError(fieldName, "حجم تصویر باید کمتر از ۵ مگابایت باشد.");
        }

        var contentType = file.ContentType ?? string.Empty;
        if (!AllowedImageContentTypes.Contains(contentType))
        {
            ModelState.AddModelError(fieldName, "فرمت تصویر پشتیبانی نمی‌شود.");
        }
    }

    private string? SaveFeaturedImage(IFormFile file, string fieldName)
    {
        var response = _fileSettingServices.UploadImage(FeaturedUploadFolder, file, Guid.NewGuid().ToString("N"));

        if (!response.Success || string.IsNullOrWhiteSpace(response.Data))
        {
            ModelState.AddModelError(fieldName, response.Messages.FirstOrDefault()?.message ?? "امکان ذخیره‌سازی تصویر وجود ندارد.");
            return null;
        }

        return response.Data.Replace("\\", "/");
    }

    private IReadOnlyCollection<string> NormalizeTags(BlogFormViewModel model)
    {
        var parsedTags = ParseTags(model.Tags);
        model.TagItems = parsedTags;
        model.Tags = string.Join(", ", parsedTags);

        ModelState.Remove(nameof(BlogFormViewModel.Tags));

        if (!string.IsNullOrEmpty(model.Tags) && model.Tags.Length > MaxTagFieldLength)
        {
            ModelState.AddModelError(nameof(BlogFormViewModel.Tags), "برچسب‌ها نمی‌تواند بیش از ۱۰۰۰ کاراکتر باشد.");
        }

        return parsedTags;
    }

    private static IReadOnlyCollection<string> ParseTags(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return Array.Empty<string>();
        }

        const int MaxTagLength = 50;
        var unique = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var ordered = new List<string>();

        foreach (var raw in input.Split(TagSeparators, StringSplitOptions.RemoveEmptyEntries))
        {
            var tag = raw.Trim();
            if (string.IsNullOrWhiteSpace(tag))
            {
                continue;
            }

            if (tag.Length > MaxTagLength)
            {
                tag = tag[..MaxTagLength];
            }

            if (unique.Add(tag))
            {
                ordered.Add(tag);
            }
        }

        return ordered;
    }

    private static string GenerateSlug(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return string.Empty;
        }

        var normalized = title
            .Trim()
            .ToLower(CultureInfo.InvariantCulture)
            .Replace(' ', '-');

        return normalized;
    }

    private void PrepareFormView(string title, string subtitle)
    {
        ViewData["Title"] = title;
        ViewData["Subtitle"] = subtitle;
    }
}
