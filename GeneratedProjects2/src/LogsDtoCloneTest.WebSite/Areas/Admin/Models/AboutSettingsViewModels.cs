using System.ComponentModel.DataAnnotations;
using LogsDtoCloneTest.Application.Commands.Admin.AboutSettings;
using LogsDtoCloneTest.Application.DTOs.Settings;

namespace LogsDtoCloneTest.WebSite.Areas.Admin.Models;

public sealed class AboutSettingsViewModel
{
    [Display(Name = "عنوان")]
    [Required(ErrorMessage = "عنوان الزامی است.")]
    [StringLength(300, ErrorMessage = "عنوان نمی‌تواند بیشتر از ۳۰۰ کاراکتر باشد.")]
    public string Title { get; set; } = string.Empty;

    [Display(Name = "توضیحات")]
    public string Description { get; set; } = string.Empty;

    [Display(Name = "چشم‌انداز")]
    public string? Vision { get; set; }

    [Display(Name = "ماموریت")]
    public string? Mission { get; set; }

    [Display(Name = "تصویر")]
    public IFormFile? Image { get; set; }

    public string? ImagePath { get; set; }

    public bool RemoveImage { get; set; }

    [Display(Name = "عنوان SEO")]
    [StringLength(200, ErrorMessage = "عنوان SEO نمی‌تواند بیشتر از ۲۰۰ کاراکتر باشد.")]
    public string? MetaTitle { get; set; }

    [Display(Name = "توضیحات SEO")]
    [StringLength(500, ErrorMessage = "توضیحات SEO نمی‌تواند بیشتر از ۵۰۰ کاراکتر باشد.")]
    public string? MetaDescription { get; set; }

    public static AboutSettingsViewModel FromDto(AboutSettingDto dto)
        => new()
        {
            Title = dto.Title,
            Description = dto.Description,
            Vision = dto.Vision,
            Mission = dto.Mission,
            ImagePath = dto.ImagePath,
            MetaTitle = dto.MetaTitle,
            MetaDescription = dto.MetaDescription
        };

    public UpdateAboutSettingsCommand ToCommand()
        => new(
            Title,
            Description,
            Vision,
            Mission,
            ImagePath,
            MetaTitle,
            MetaDescription);
}

