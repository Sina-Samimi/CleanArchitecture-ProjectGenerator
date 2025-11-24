using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Arsis.Domain.Entities;
using OrganizationStatus = Arsis.Domain.Entities.OrganizationStatus;

namespace EndPoint.WebSite.Areas.Admin.Models.Organizations
{
    public class CreateOrganizationViewModel
    {
        [Required(ErrorMessage = "نام سازمان الزامی است")]
        [Display(Name = "نام سازمان")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "کد سازمان الزامی است")]
        [Display(Name = "کد سازمان")]
        [StringLength(10, ErrorMessage = "کد سازمان حداکثر ۱۰ کاراکتر می‌تواند باشد")]
        public string Code { get; set; } = string.Empty;

        [Display(Name = "توضیحات")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "نام مدیر الزامی است")]
        [Display(Name = "نام مدیر")]
        public string AdminName { get; set; } = string.Empty;

        [Required(ErrorMessage = "ایمیل مدیر الزامی است")]
        [Display(Name = "ایمیل مدیر")]
        [EmailAddress(ErrorMessage = "ایمیل معتبر وارد کنید")]
        public string AdminEmail { get; set; } = string.Empty;

        [Display(Name = "شماره تلفن")]
        public string? PhoneNumber { get; set; }

        [Display(Name = "آدرس")]
        public string? Address { get; set; }

        [Required(ErrorMessage = "وضعیت الزامی است")]
        [Display(Name = "وضعیت")]
        public OrganizationStatus Status { get; set; } = OrganizationStatus.Active;

        [Display(Name = "حداکثر تعداد کاربر")]
        [Range(1, 10000, ErrorMessage = "حداکثر تعداد کاربر باید بین ۱ تا ۱۰۰۰۰ باشد")]
        public int? MaxUsers { get; set; }

        [Display(Name = "انقضای اشتراک")]
        public DateTime? SubscriptionExpiry { get; set; }

        public IEnumerable<SelectListItem> StatusOptions { get; set; } = new List<SelectListItem>();
    }
}
