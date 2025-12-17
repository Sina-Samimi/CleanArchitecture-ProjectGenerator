using Attar.Application.Commands.Admin.SmsSettings;
using Attar.Application.DTOs.Settings;
using System.ComponentModel.DataAnnotations;

namespace Attar.WebSite.Areas.Admin.Models;

public sealed class SmsSettingsViewModel
{
    [Required(ErrorMessage = "API Key الزامی است.")]
    [Display(Name = "API Key")]
    [StringLength(500, ErrorMessage = "API Key نمی‌تواند بیشتر از ۵۰۰ کاراکتر باشد.")]
    public string ApiKey { get; set; } = string.Empty;

    [Display(Name = "فعال")]
    public bool IsActive { get; set; }

    public static SmsSettingsViewModel FromDto(SmsSettingDto dto)
    {
        return new SmsSettingsViewModel
        {
            ApiKey = dto.ApiKey,
            IsActive = dto.IsActive
        };
    }

    public UpdateSmsSettingsCommand ToCommand()
    {
        return new UpdateSmsSettingsCommand(
            ApiKey,
            IsActive);
    }
}
