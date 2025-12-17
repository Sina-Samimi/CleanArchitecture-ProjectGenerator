using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using TestAttarClone.Application.Commands.Admin.DeploymentProfiles;
using TestAttarClone.Application.DTOs.Deployment;

namespace TestAttarClone.WebSite.Areas.Admin.Models;

public sealed class DeploymentProfileIndexViewModel
{
    public IReadOnlyCollection<DeploymentProfileListItemViewModel> Profiles { get; init; }
        = Array.Empty<DeploymentProfileListItemViewModel>();

    public int ActiveCount => Profiles.Count(profile => profile.IsActive);

    public int TotalCount => Profiles.Count;
}

public sealed class DeploymentProfileListItemViewModel
{
    public Guid Id { get; init; }

    public required string Name { get; init; }

    public required string Branch { get; init; }

    public required string ServerHost { get; init; }

    public int ServerPort { get; init; }

    public required string ServerUser { get; init; }

    public required string DestinationPath { get; init; }

    public required string ArtifactName { get; init; }

    public bool IsActive { get; init; }

    public DateTimeOffset UpdatedAt { get; init; }

    public static DeploymentProfileListItemViewModel FromDto(DeploymentProfileDto dto)
        => new()
        {
            Id = dto.Id,
            Name = dto.Name,
            Branch = dto.Branch,
            ServerHost = dto.ServerHost,
            ServerPort = dto.ServerPort,
            ServerUser = dto.ServerUser,
            DestinationPath = dto.DestinationPath,
            ArtifactName = dto.ArtifactName,
            IsActive = dto.IsActive,
            UpdatedAt = dto.UpdatedAt
        };
}

public sealed class DeploymentProfileFormViewModel
{
    public Guid? Id { get; init; }

    [Display(Name = "عنوان پروفایل")]
    [Required(ErrorMessage = "وارد کردن عنوان الزامی است.")]
    [StringLength(200, ErrorMessage = "عنوان نمی‌تواند بیشتر از ۲۰۰ کاراکتر باشد.")]
    public string Name { get; set; } = string.Empty;

    [Display(Name = "برنچ گیت")]
    [Required(ErrorMessage = "وارد کردن نام برنچ الزامی است.")]
    [StringLength(150, ErrorMessage = "نام برنچ نمی‌تواند بیشتر از ۱۵۰ کاراکتر باشد.")]
    public string Branch { get; set; } = string.Empty;

    [Display(Name = "آدرس سرور")]
    [Required(ErrorMessage = "آدرس سرور الزامی است.")]
    [StringLength(200, ErrorMessage = "آدرس سرور نمی‌تواند بیشتر از ۲۰۰ کاراکتر باشد.")]
    public string ServerHost { get; set; } = string.Empty;

    [Display(Name = "پورت SSH")]
    [Range(1, 65535, ErrorMessage = "پورت باید بین 1 تا 65535 باشد.")]
    public int ServerPort { get; set; } = 22;

    [Display(Name = "نام کاربری سرور")]
    [Required(ErrorMessage = "نام کاربری الزامی است.")]
    [StringLength(100, ErrorMessage = "نام کاربری نمی‌تواند بیشتر از ۱۰۰ کاراکتر باشد.")]
    public string ServerUser { get; set; } = string.Empty;

    [Display(Name = "مسیر استقرار روی سرور")]
    [Required(ErrorMessage = "مسیر استقرار الزامی است.")]
    [StringLength(400, ErrorMessage = "مسیر استقرار نمی‌تواند بیشتر از ۴۰۰ کاراکتر باشد.")]
    public string DestinationPath { get; set; } = string.Empty;

    [Display(Name = "نام آرشیو خروجی")]
    [Required(ErrorMessage = "نام فایل خروجی الزامی است.")]
    [StringLength(200, ErrorMessage = "نام آرشیو نمی‌تواند بیشتر از ۲۰۰ کاراکتر باشد.")]
    public string ArtifactName { get; set; } = "published-app";

    [Display(Name = "فعال")]
    public bool IsActive { get; set; } = true;

    [Display(Name = "دستور قبل از استقرار")]
    [StringLength(1000, ErrorMessage = "حداکثر طول مجاز ۱۰۰۰ کاراکتر است.")]
    public string? PreDeployCommand { get; set; }
        = "mkdir -p ~/deployments";

    [Display(Name = "دستور بعد از استقرار")]
    [StringLength(1000, ErrorMessage = "حداکثر طول مجاز ۱۰۰۰ کاراکتر است.")]
    public string? PostDeployCommand { get; set; }
        = "chmod +x deploy.sh && ./deploy.sh";

    [Display(Name = "دستور ری‌استارت سرویس")]
    [StringLength(400, ErrorMessage = "حداکثر طول مجاز ۴۰۰ کاراکتر است.")]
    public string? ServiceReloadCommand { get; set; }
        = "sudo systemctl restart kestrel-arsis";

    [Display(Name = "نام سکرت کلید خصوصی در گیت‌هاب")]
    [StringLength(200, ErrorMessage = "حداکثر طول مجاز ۲۰۰ کاراکتر است.")]
    public string? SecretKeyName { get; set; }
        = "DEPLOY_SSH_KEY";

    [Display(Name = "توضیحات")]
    [StringLength(1000, ErrorMessage = "حداکثر طول مجاز ۱۰۰۰ کاراکتر است.")]
    public string? Notes { get; set; }
        = "اطلاعات بیشتر درباره نحوه استقرار";

    public static DeploymentProfileFormViewModel FromDto(DeploymentProfileDto dto)
        => new()
        {
            Id = dto.Id,
            Name = dto.Name,
            Branch = dto.Branch,
            ServerHost = dto.ServerHost,
            ServerPort = dto.ServerPort,
            ServerUser = dto.ServerUser,
            DestinationPath = dto.DestinationPath,
            ArtifactName = dto.ArtifactName,
            IsActive = dto.IsActive,
            PreDeployCommand = dto.PreDeployCommand,
            PostDeployCommand = dto.PostDeployCommand,
            ServiceReloadCommand = dto.ServiceReloadCommand,
            SecretKeyName = dto.SecretKeyName,
            Notes = dto.Notes
        };

    public SaveDeploymentProfileCommand ToCommand()
        => new(
            Id,
            Name,
            Branch,
            ServerHost,
            ServerPort,
            ServerUser,
            DestinationPath,
            ArtifactName,
            IsActive,
            PreDeployCommand,
            PostDeployCommand,
            ServiceReloadCommand,
            SecretKeyName,
            Notes);
}
