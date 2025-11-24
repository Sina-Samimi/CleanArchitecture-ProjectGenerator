using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Arsis.Domain.Entities;
using OrganizationStatus = Arsis.Domain.Entities.OrganizationStatus;

namespace EndPoint.WebSite.Areas.Admin.Models.Organizations
{
    public class EditOrganizationViewModel : CreateOrganizationViewModel
    {
        public Guid Id { get; set; }

        [Display(Name = "شماره تلفن")]
        public string? PhoneNumber { get; set; }

        [Display(Name = "آدرس")]
        public string? Address { get; set; }

        [Display(Name = "حداکثر تعداد کاربر")]
        [Range(1, 10000, ErrorMessage = "حداکثر تعداد کاربر باید بین ۱ تا ۱۰۰۰۰ باشد")]
        public int? MaxUsers { get; set; }

        [Display(Name = "انقضای اشتراک")]
        public DateTime? SubscriptionExpiry { get; set; }
    }
}