using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace LogsDtoCloneTest.WebSite.Areas.User.Models;

public sealed class UserAddressesViewModel
{
    public required IReadOnlyCollection<UserAddressViewModel> Addresses { get; init; }
}

public sealed class UserAddressViewModel
{
    public Guid Id { get; init; }

    public string Title { get; init; } = string.Empty;

    public string RecipientName { get; init; } = string.Empty;

    public string RecipientPhone { get; init; } = string.Empty;

    public string Province { get; init; } = string.Empty;

    public string City { get; init; } = string.Empty;

    public string PostalCode { get; init; } = string.Empty;

    public string AddressLine { get; init; } = string.Empty;

    public string? Plaque { get; init; }

    public string? Unit { get; init; }

    public bool IsDefault { get; init; }
}

public sealed class UserAddressFormViewModel
{
    public Guid? Id { get; set; }

    [Required(ErrorMessage = "عنوان آدرس الزامی است.")]
    [Display(Name = "عنوان آدرس")]
    [StringLength(100, ErrorMessage = "عنوان آدرس نمی‌تواند بیشتر از 100 کاراکتر باشد.")]
    public string? Title { get; set; }

    [Required(ErrorMessage = "نام گیرنده الزامی است.")]
    [Display(Name = "نام گیرنده")]
    [StringLength(200, ErrorMessage = "نام گیرنده نمی‌تواند بیشتر از 200 کاراکتر باشد.")]
    public string? RecipientName { get; set; }

    [Required(ErrorMessage = "شماره تماس گیرنده الزامی است.")]
    [Display(Name = "شماره تماس گیرنده")]
    [StringLength(20, ErrorMessage = "شماره تماس نمی‌تواند بیشتر از 20 کاراکتر باشد.")]
    public string? RecipientPhone { get; set; }

    [Required(ErrorMessage = "استان الزامی است.")]
    [Display(Name = "استان")]
    [StringLength(100, ErrorMessage = "استان نمی‌تواند بیشتر از 100 کاراکتر باشد.")]
    public string? Province { get; set; }

    [Required(ErrorMessage = "شهر الزامی است.")]
    [Display(Name = "شهر")]
    [StringLength(100, ErrorMessage = "شهر نمی‌تواند بیشتر از 100 کاراکتر باشد.")]
    public string? City { get; set; }

    [Required(ErrorMessage = "کد پستی الزامی است.")]
    [Display(Name = "کد پستی")]
    [StringLength(10, ErrorMessage = "کد پستی نمی‌تواند بیشتر از 10 کاراکتر باشد.")]
    public string? PostalCode { get; set; }

    [Required(ErrorMessage = "آدرس کامل الزامی است.")]
    [Display(Name = "آدرس کامل")]
    [StringLength(500, ErrorMessage = "آدرس کامل نمی‌تواند بیشتر از 500 کاراکتر باشد.")]
    public string? AddressLine { get; set; }

    [Display(Name = "پلاک")]
    [StringLength(20, ErrorMessage = "پلاک نمی‌تواند بیشتر از 20 کاراکتر باشد.")]
    public string? Plaque { get; set; }

    [Display(Name = "واحد")]
    [StringLength(20, ErrorMessage = "واحد نمی‌تواند بیشتر از 20 کاراکتر باشد.")]
    public string? Unit { get; set; }

    [Display(Name = "آدرس پیش‌فرض")]
    public bool IsDefault { get; set; }
}
