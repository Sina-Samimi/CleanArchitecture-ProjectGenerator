using System;
using System.Linq;
using System.Threading.Tasks;
using Arsis.Application.Commands.Tests;
using Arsis.Application.DTOs.Tests;
using Arsis.Application.Interfaces;
using Arsis.Application.Queries.Tests;
using Arsis.Domain.Enums;
using EndPoint.WebSite.Areas.Admin.Models.Tests;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace EndPoint.WebSite.Areas.Admin.Controllers;

[Area("Admin")]
public sealed class TestsController : Controller
{
    private readonly IMediator _mediator;
    private readonly ISiteCategoryRepository _categoryRepository;

    public TestsController(IMediator mediator, ISiteCategoryRepository categoryRepository)
    {
        _mediator = mediator;
        _categoryRepository = categoryRepository;
    }

    [HttpGet]
    public async Task<IActionResult> Index([FromQuery] TestIndexRequest? request)
    {
        request ??= new TestIndexRequest();
        var cancellationToken = HttpContext.RequestAborted;

        var result = await _mediator.Send(new GetTestListQuery(
            request.Type,
            request.Status,
            request.Search,
            request.Page,
            request.PageSize), cancellationToken);

        if (!result.IsSuccess)
        {
            TempData["Error"] = result.Error;
            return View(new TestIndexViewModel());
        }

        // Calculate statistics from all tests
        var allTestsResult = await _mediator.Send(new GetTestListQuery(
            null,
            null,
            null,
            1,
            int.MaxValue), cancellationToken);

        var statistics = new TestStatistics();
        if (allTestsResult.IsSuccess && allTestsResult.Value.Items.Any())
        {
            var allTests = allTestsResult.Value.Items;
            statistics.TotalTests = allTests.Count;
            statistics.PublishedTests = allTests.Count(t => t.Status == TestStatus.Published);
            statistics.DraftTests = allTests.Count(t => t.Status == TestStatus.Draft);
            statistics.ArchivedTests = allTests.Count(t => t.Status == TestStatus.Archived);
            statistics.AveragePrice = allTests.Any() ? allTests.Average(t => t.Price) : 0;
            statistics.HighestPrice = allTests.Any() ? allTests.Max(t => t.Price) : 0;
            statistics.LowestPrice = allTests.Any() ? allTests.Min(t => t.Price) : 0;
            statistics.TotalParticipants = allTests.Sum(t => t.AttemptsCount);
            statistics.TotalQuestions = allTests.Sum(t => t.QuestionsCount);
        }

        var viewModel = new TestIndexViewModel
        {
            Tests = result.Value.Items,
            TotalCount = result.Value.TotalCount,
            Page = result.Value.Page,
            PageSize = result.Value.PageSize,
            TotalPages = result.Value.TotalPages,
            Search = request.Search,
            Type = request.Type,
            Status = request.Status,
            TestTypes = GetTestTypeSelectList(),
            TestStatuses = GetTestStatusSelectList(),
            Statistics = statistics
        };

        return View(viewModel);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var cancellationToken = HttpContext.RequestAborted;
        // بارگذاری همه دسته‌بندی‌های سایت
        var categories = await _categoryRepository.GetAllAsync(cancellationToken);

        var viewModel = new CreateTestViewModel
        {
            TestTypes = GetTestTypeSelectList(),
            Categories = new SelectList(categories, "Id", "Name"),
            ShowResultsImmediately = true,
            RandomizeQuestions = false,
            RandomizeOptions = false
        };

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateTestViewModel model)
    {
        var cancellationToken = HttpContext.RequestAborted;

        if (!ModelState.IsValid)
        {
            model.TestTypes = GetTestTypeSelectList();
            var categories = await _categoryRepository.GetAllAsync(cancellationToken);
            model.Categories = new SelectList(categories, "Id", "Name");
            return View(model);
        }

        var command = new CreateTestCommand(
            model.Title,
            model.Description ?? string.Empty,
            model.Type,
            model.CategoryId,
            model.Price,
            string.IsNullOrWhiteSpace(model.Currency) ? "IRT" : model.Currency,
            model.DurationMinutes,
            model.MaxAttempts,
            model.ShowResultsImmediately,
            model.ShowCorrectAnswers,
            model.RandomizeQuestions,
            model.RandomizeOptions,
            model.AvailableFrom,
            model.AvailableUntil,
            model.NumberOfQuestionsToShow,
            model.PassingScore);

        var result = await _mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
        {
            ModelState.AddModelError(string.Empty, result.Error);
            model.TestTypes = GetTestTypeSelectList();
            var categories = await _categoryRepository.GetAllAsync(cancellationToken);
            model.Categories = new SelectList(categories, "Id", "Name");
            return View(model);
        }

        TempData["Success"] = "تست با موفقیت ایجاد شد.";
        return RedirectToAction(nameof(Edit), new { id = result.Value });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(Guid id)
    {
        var cancellationToken = HttpContext.RequestAborted;

        var result = await _mediator.Send(new GetTestByIdQuery(id), cancellationToken);

        if (!result.IsSuccess)
        {
            TempData["Error"] = result.Error;
            return RedirectToAction(nameof(Index));
        }

        var test = result.Value;

        var viewModel = new EditTestViewModel
        {
            Id = test.Id,
            Title = test.Title,
            Description = test.Description,
            Type = test.Type,
            Status = test.Status,
            CategoryId = test.CategoryId,
            Price = test.Price,
            Currency = test.Currency ?? "IRT",
            DurationMinutes = test.DurationMinutes,
            MaxAttempts = test.MaxAttempts,
            ShowResultsImmediately = test.ShowResultsImmediately,
            ShowCorrectAnswers = test.ShowCorrectAnswers,
            RandomizeQuestions = test.RandomizeQuestions,
            RandomizeOptions = test.RandomizeOptions,
            AvailableFrom = test.AvailableFrom,
            AvailableUntil = test.AvailableUntil,
            NumberOfQuestionsToShow = test.NumberOfQuestionsToShow,
            PassingScore = test.PassingScore,
            Questions = test.Questions.OrderBy(q => q.Order).ToList()
        };

        await PopulateEditViewModelAsync(viewModel, cancellationToken);

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(EditTestViewModel model)
    {
        var cancellationToken = HttpContext.RequestAborted;

        if (!ModelState.IsValid)
        {
            await PopulateEditViewModelAsync(model, cancellationToken);
            return View(model);
        }

        var command = new UpdateTestCommand(
            model.Id,
            model.Title,
            model.Description ?? string.Empty,
            model.CategoryId,
            model.Price,
            string.IsNullOrWhiteSpace(model.Currency) ? "IRT" : model.Currency,
            model.DurationMinutes,
            model.MaxAttempts,
            model.ShowResultsImmediately,
            model.ShowCorrectAnswers,
            model.RandomizeQuestions,
            model.RandomizeOptions,
            model.AvailableFrom,
            model.AvailableUntil,
            model.NumberOfQuestionsToShow,
            model.PassingScore,
            model.Type,
            model.Status);

        var result = await _mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
        {
            ModelState.AddModelError(string.Empty, result.Error);
            await PopulateEditViewModelAsync(model, cancellationToken);
            return View(model);
        }

        TempData["Success"] = "تست با موفقیت به‌روزرسانی شد.";
        return RedirectToAction(nameof(Edit), new { id = model.Id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddQuestion(AddQuestionViewModel model)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "اطلاعات سوال معتبر نیست.";
            return RedirectToAction(nameof(Edit), new { id = model.TestId });
        }

        var cancellationToken = HttpContext.RequestAborted;

        var options = model.Options?
            .Where(o => !string.IsNullOrWhiteSpace(o.Text))
            .Select(o => new QuestionOptionInput(
                null,
                o.Text,
                o.IsCorrect,
                o.Score,
                o.ImageUrl))
            .ToList();

        var command = new AddTestQuestionCommand(
            model.TestId,
            model.Text,
            model.QuestionType,
            model.Order,
            model.Score,
            model.IsRequired,
            model.ImageUrl,
            model.Explanation,
            options);

        var result = await _mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
        {
            TempData["Error"] = result.Error;
            return RedirectToAction(nameof(Edit), new { id = model.TestId });
        }

        TempData["Success"] = "سوال با موفقیت اضافه شد.";
        return RedirectToAction(nameof(Edit), new { id = model.TestId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateQuestion(EditQuestionViewModel model)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "اطلاعات سوال معتبر نیست.";
            return RedirectToAction(nameof(Edit), new { id = model.TestId });
        }

        var cancellationToken = HttpContext.RequestAborted;

        var options = model.Options?
            .Where(o => !string.IsNullOrWhiteSpace(o.Text))
            .Select(o => new QuestionOptionInput(
                o.Id,
                o.Text,
                o.IsCorrect,
                o.Score,
                o.ImageUrl))
            .ToList();

        var command = new UpdateTestQuestionCommand(
            model.TestId,
            model.QuestionId,
            model.Text,
            model.QuestionType,
            model.Order,
            model.Score,
            model.IsRequired,
            model.ImageUrl,
            model.Explanation,
            options);

        var result = await _mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
        {
            TempData["Error"] = result.Error;
            return RedirectToAction(nameof(Edit), new { id = model.TestId });
        }

        TempData["Success"] = "سوال با موفقیت بروزرسانی شد.";
        return RedirectToAction(nameof(Edit), new { id = model.TestId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteQuestion(Guid testId, Guid questionId)
    {
        var cancellationToken = HttpContext.RequestAborted;

        var command = new DeleteTestQuestionCommand(testId, questionId);
        var result = await _mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
        {
            TempData["Error"] = result.Error;
        }
        else
        {
            TempData["Success"] = "سوال با موفقیت حذف شد.";
        }

        return RedirectToAction(nameof(Edit), new { id = testId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Publish(Guid id)
    {
        var cancellationToken = HttpContext.RequestAborted;

        var command = new PublishTestCommand(id);
        var result = await _mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
        {
            TempData["Error"] = result.Error;
        }
        else
        {
            TempData["Success"] = "تست با موفقیت منتشر شد.";
        }

        return RedirectToAction(nameof(Edit), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id)
    {
        var cancellationToken = HttpContext.RequestAborted;

        var command = new DeleteTestCommand(id);
        var result = await _mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
        {
            TempData["Error"] = result.Error;
        }
        else
        {
            TempData["Success"] = "تست با موفقیت حذف شد.";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Attempts([FromQuery] TestAttemptsRequest? request)
    {
        request ??= new TestAttemptsRequest();
        var cancellationToken = HttpContext.RequestAborted;

        var attemptsResult = await _mediator.Send(new GetTestAttemptListQuery(
            request.TestId,
            request.UserId,
            request.Status,
            request.Search,
            request.StartedFrom,
            request.StartedTo,
            request.Page,
            request.PageSize), cancellationToken);

        if (!attemptsResult.IsSuccess)
        {
            TempData["Error"] = attemptsResult.Error;
            return View(new TestAttemptsViewModel
            {
                Page = request.Page,
                PageSize = request.PageSize,
                Statuses = GetAttemptStatusSelectList(request.Status)
            });
        }

        var testsLookup = await _mediator.Send(new GetTestListQuery(
            null,
            null,
            null,
            1,
            100), cancellationToken);

        SelectList? testsSelect = null;
        if (testsLookup.IsSuccess && testsLookup.Value.Items.Any())
        {
            testsSelect = new SelectList(testsLookup.Value.Items, "Id", "Title", request.TestId);
        }

        var statistics = attemptsResult.Value.Statistics ?? TestAttemptStatisticsDto.Empty;

        var viewModel = new TestAttemptsViewModel
        {
            Attempts = attemptsResult.Value.Items,
            TotalCount = attemptsResult.Value.TotalCount,
            Page = attemptsResult.Value.Page,
            PageSize = attemptsResult.Value.PageSize,
            TotalPages = attemptsResult.Value.TotalPages,
            TestId = request.TestId,
            UserId = request.UserId,
            Status = request.Status,
            Search = request.Search,
            StartedFrom = request.StartedFrom,
            StartedTo = request.StartedTo,
            Tests = testsSelect,
            Statuses = GetAttemptStatusSelectList(request.Status),
            Statistics = new TestAttemptStatisticsViewModel
            {
                TotalAttempts = statistics.TotalAttempts,
                CompletedAttempts = statistics.CompletedAttempts,
                InProgressAttempts = statistics.InProgressAttempts,
                CancelledAttempts = statistics.CancelledAttempts,
                ExpiredAttempts = statistics.ExpiredAttempts,
                UniqueParticipants = statistics.UniqueParticipants,
                AverageScore = statistics.AverageScore,
                AverageCompletionMinutes = statistics.AverageCompletionMinutes,
                FirstAttemptAt = statistics.FirstAttemptAt,
                LastAttemptAt = statistics.LastAttemptAt
            }
        };

        ViewData["Title"] = "سوابق آزمون‌ها";
        ViewData["Subtitle"] = "لیست کامل تلاش کاربران در آزمون‌های سیستم";
        ViewData["ShowSearch"] = false;

        return View(viewModel);
    }

    [HttpGet]
    public async Task<IActionResult> AttemptDetails(Guid id)
    {
        var cancellationToken = HttpContext.RequestAborted;

        var result = await _mediator.Send(new GetUserTestAttemptQuery(id), cancellationToken);
        if (!result.IsSuccess)
        {
            TempData["Error"] = result.Error;
            return RedirectToAction(nameof(Attempts));
        }

        var attempt = result.Value;

        var viewModel = new TestAttemptDetailViewModel
        {
            Attempt = attempt,
            TestTypeTitle = GetTestTypeName(attempt.TestType),
            StatusTitle = GetAttemptStatusName(attempt.Status),
            UserFullName = attempt.UserFullName,
            UserEmail = attempt.UserEmail,
            UserPhoneNumber = attempt.UserPhoneNumber
        };

        ViewData["Title"] = "جزئیات تلاش آزمون";
        ViewData["Subtitle"] = attempt.TestTitle;
        ViewData["ShowSearch"] = false;

        return View("AttemptDetails", viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteAttempt(Guid id, [FromForm] TestAttemptsRequest? request)
    {
        var cancellationToken = HttpContext.RequestAborted;
        request ??= new TestAttemptsRequest();

        var result = await _mediator.Send(new DeleteTestAttemptCommand(id), cancellationToken);

        if (!result.IsSuccess)
        {
            TempData["Error"] = result.Error;
        }
        else
        {
            TempData["Success"] = "تلاش کاربر با موفقیت حذف شد.";
        }

        return RedirectToAction(nameof(Attempts), new
        {
            request.TestId,
            request.UserId,
            Status = request.Status,
            request.Search,
            StartedFrom = request.StartedFrom?.ToString("yyyy-MM-dd"),
            StartedTo = request.StartedTo?.ToString("yyyy-MM-dd"),
            Page = request.Page <= 0 ? 1 : request.Page,
            PageSize = request.PageSize <= 0 ? 20 : request.PageSize
        });
    }

    private static SelectList GetTestTypeSelectList()
    {
        return new SelectList(Enum.GetValues<TestType>()
            .Select(t => new { Value = (int)t, Text = GetTestTypeName(t) }), "Value", "Text");
    }

    private static SelectList GetTestStatusSelectList()
    {
        return new SelectList(Enum.GetValues<TestStatus>()
            .Select(s => new { Value = (int)s, Text = GetTestStatusName(s) }), "Value", "Text");
    }

    private static SelectList GetAttemptStatusSelectList(TestAttemptStatus? selected = null)
    {
        var items = Enum.GetValues<TestAttemptStatus>()
            .Select(s => new { Value = (int)s, Text = GetAttemptStatusName(s) });

        return new SelectList(items, "Value", "Text", selected.HasValue ? (int?)selected.Value : null);
    }

    private static SelectList GetQuestionTypeSelectList()
    {
        return new SelectList(Enum.GetValues<TestQuestionType>()
            .Select(q => new { Value = (int)q, Text = GetQuestionTypeName(q) }), "Value", "Text");
    }

    private static string GetTestTypeName(TestType type) => type switch
    {
        TestType.General => "عمومی",
        TestType.Disc => "DISC",
        TestType.Clifton => "کلیفتون",
        TestType.CliftonSchwartz => "کلیفتون + شوارتز",
        TestType.Raven => "ریون (هوش)",
        TestType.Personality => "شخصیت‌شناسی",
        _ => type.ToString()
    };

    private static string GetTestStatusName(TestStatus status) => status switch
    {
        TestStatus.Draft => "پیش‌نویس",
        TestStatus.Published => "منتشر شده",
        TestStatus.Archived => "آرشیو",
        _ => status.ToString()
    };

    private static string GetAttemptStatusName(TestAttemptStatus status) => status switch
    {
        TestAttemptStatus.InProgress => "در حال انجام",
        TestAttemptStatus.Completed => "تکمیل شده",
        TestAttemptStatus.Cancelled => "لغو شده",
        TestAttemptStatus.Expired => "منقضی شده",
        _ => status.ToString()
    };

    private static string GetQuestionTypeName(TestQuestionType type) => type switch
    {
        TestQuestionType.MultipleChoice => "چند گزینه‌ای",
        TestQuestionType.MultipleSelect => "چند انتخابی",
        TestQuestionType.TrueFalse => "درست/غلط",
        TestQuestionType.LikertScale => "مقیاس لیکرت",
        TestQuestionType.ShortText => "متن کوتاه",
        TestQuestionType.LongText => "متن بلند",
        _ => type.ToString()
    };

    private async Task PopulateEditViewModelAsync(EditTestViewModel model, CancellationToken cancellationToken)
    {
        var categories = await _categoryRepository.GetAllAsync(cancellationToken);
        model.Categories = new SelectList(categories, "Id", "Name", model.CategoryId);
        model.QuestionTypes = GetQuestionTypeSelectList();
        model.TestTypes = GetTestTypeSelectList();
        model.TestStatuses = GetTestStatusSelectList();
    }
}
