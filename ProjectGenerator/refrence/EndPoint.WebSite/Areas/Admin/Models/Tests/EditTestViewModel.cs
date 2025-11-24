using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Arsis.Application.DTOs.Tests;
using Arsis.Domain.Enums;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace EndPoint.WebSite.Areas.Admin.Models.Tests;

public sealed class EditTestViewModel
{
    public Guid Id { get; set; }

    [Required(ErrorMessage = "عنوان تست الزامی است.")]
    [StringLength(300, ErrorMessage = "عنوان نمی‌تواند بیشتر از 300 کاراکتر باشد.")]
    public string Title { get; set; } = null!;

    [DataType(DataType.MultilineText)]
    public string? Description { get; set; }

    public TestType Type { get; set; }

    public TestStatus Status { get; set; }

    public Guid? CategoryId { get; set; }

    [Required(ErrorMessage = "قیمت الزامی است.")]
    [Range(0, double.MaxValue, ErrorMessage = "قیمت باید صفر یا بیشتر باشد.")]
    public decimal Price { get; set; }

    [StringLength(10, ErrorMessage = "واحد پول نمی‌تواند بیشتر از 10 کاراکتر باشد.")]
    [Display(Name = "واحد پول")]
    public string Currency { get; set; } = "IRT";

    [Range(1, int.MaxValue, ErrorMessage = "مدت زمان باید بزرگتر از صفر باشد.")]
    public int? DurationMinutes { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "تعداد دفعات باید بزرگتر از صفر باشد.")]
    public int? MaxAttempts { get; set; }

    public bool ShowResultsImmediately { get; set; }

    public bool ShowCorrectAnswers { get; set; }

    public bool RandomizeQuestions { get; set; }

    public bool RandomizeOptions { get; set; }

    public DateTimeOffset? AvailableFrom { get; set; }

    public DateTimeOffset? AvailableUntil { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "تعداد سوالات باید بزرگتر از صفر باشد.")]
    public int? NumberOfQuestionsToShow { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "نمره قبولی باید صفر یا بیشتر باشد.")]
    public decimal? PassingScore { get; set; }

    public List<TestQuestionDto> Questions { get; set; } = new();

    public SelectList? QuestionTypes { get; set; }

    public SelectList? Categories { get; set; }

    public SelectList? TestTypes { get; set; }

    public SelectList? TestStatuses { get; set; }
}
