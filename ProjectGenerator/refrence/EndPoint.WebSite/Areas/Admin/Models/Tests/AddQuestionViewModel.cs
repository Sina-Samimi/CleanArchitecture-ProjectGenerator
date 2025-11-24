using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Arsis.Domain.Enums;

namespace EndPoint.WebSite.Areas.Admin.Models.Tests;

public sealed class AddQuestionViewModel
{
    [Required]
    public Guid TestId { get; set; }

    [Required(ErrorMessage = "متن سوال الزامی است.")]
    public string Text { get; set; } = null!;

    [Required(ErrorMessage = "نوع سوال الزامی است.")]
    public TestQuestionType QuestionType { get; set; }

    [Required(ErrorMessage = "ترتیب الزامی است.")]
    public int Order { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "امتیاز باید صفر یا بیشتر باشد.")]
    public int? Score { get; set; }

    public bool IsRequired { get; set; } = true;

    public string? ImageUrl { get; set; }

    public string? Explanation { get; set; }

    public List<QuestionOptionViewModel>? Options { get; set; }
}

public sealed class QuestionOptionViewModel
{
    public Guid? Id { get; set; }
    public string Text { get; set; } = null!;
    public bool IsCorrect { get; set; }
    public int? Score { get; set; }
    public string? ImageUrl { get; set; }
    public string? Explanation { get; set; }
}
