using System;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using LogTableRenameTest.Application.DTOs.Cart;
using LogTableRenameTest.Domain.Enums;

namespace LogTableRenameTest.WebSite.Models.Cart;

public sealed class CartViewModel
{
    public required Guid? AnonymousId { get; init; }

    public required string? UserId { get; init; }

    public required IReadOnlyList<CartItemViewModel> Items { get; init; }

    public required decimal Subtotal { get; init; }

    public required decimal DiscountTotal { get; init; }

    public required decimal GrandTotal { get; init; }

    public CartDiscountViewModel? Discount { get; init; }

    public bool IsEmpty => Items.Count == 0;
}

public sealed class CartItemViewModel
{
    public Guid? OfferId { get; init; }

    public required Guid ProductId { get; init; }

    public required string Name { get; init; }

    public required string Slug { get; init; }

    public string? ThumbnailUrl { get; init; }

    public required decimal UnitPrice { get; init; }

    public decimal? CompareAtPrice { get; init; }

    public required int Quantity { get; init; }

    public required decimal LineTotal { get; init; }

    public required ProductType ProductType { get; init; }

    public bool CanIncreaseQuantity => true;
}

public sealed class CartDiscountViewModel
{
    public required string Code { get; init; }

    public required DiscountType DiscountType { get; init; }

    public required decimal DiscountValue { get; init; }

    public required decimal DiscountAmount { get; init; }

    public required bool WasCapped { get; init; }

    public required DateTimeOffset AppliedAt { get; init; }
}

public sealed class ApplyDiscountInputModel
{
    [Required(ErrorMessage = "کد تخفیف را وارد کنید.")]
    [Display(Name = "کد تخفیف")]
    public string? Code { get; set; }
}

public sealed class CartPreviewItemViewModel
{
    public required Guid ProductId { get; init; }

    public required string Name { get; init; }

    public required string Slug { get; init; }

    public string? ThumbnailUrl { get; init; }

    public required int Quantity { get; init; }

    public required decimal LineTotal { get; init; }
}

public sealed class CartPreviewViewModel
{
    public const string NavbarPlacement = "navbar";
    public const string MenuPlacement = "menu";

    public required Guid? AnonymousId { get; init; }

    public required string? UserId { get; init; }

    public required string Placement { get; init; }

    public required int ItemCount { get; init; }

    public required decimal Subtotal { get; init; }

    public required decimal DiscountTotal { get; init; }

    public required decimal GrandTotal { get; init; }

    public required IReadOnlyList<CartPreviewItemViewModel> Items { get; init; }

    public bool HasItems => ItemCount > 0;

    public bool HasDiscount => DiscountTotal > 0;

    public int PreviewedItemCount => Items.Count;

    public int RemainingItemCount => Math.Max(0, ItemCount - PreviewedItemCount);

    public bool IsNavbar => string.Equals(Placement, NavbarPlacement, StringComparison.OrdinalIgnoreCase);

    public bool IsMenu => string.Equals(Placement, MenuPlacement, StringComparison.OrdinalIgnoreCase);
}

public static class CartViewModelFactory
{
    public static CartViewModel FromDto(CartDto dto)
    {
        var items = new List<CartItemViewModel>(dto.Items.Count);
        foreach (var item in dto.Items)
        {
            items.Add(new CartItemViewModel
            {
                OfferId = item.OfferId,
                ProductId = item.ProductId,
                Name = item.Name,
                Slug = item.Slug,
                ThumbnailUrl = item.ThumbnailUrl,
                UnitPrice = item.UnitPrice,
                CompareAtPrice = item.CompareAtPrice,
                Quantity = item.Quantity,
                LineTotal = item.LineTotal,
                ProductType = item.ProductType
            });
        }

        CartDiscountViewModel? discount = null;
        if (dto.Discount is not null)
        {
            discount = new CartDiscountViewModel
            {
                Code = dto.Discount.Code,
                DiscountType = dto.Discount.DiscountType,
                DiscountValue = dto.Discount.DiscountValue,
                DiscountAmount = dto.Discount.DiscountAmount,
                WasCapped = dto.Discount.WasCapped,
                AppliedAt = dto.Discount.AppliedAt
            };
        }

        return new CartViewModel
        {
            AnonymousId = dto.AnonymousId,
            UserId = dto.UserId,
            Items = items,
            Subtotal = dto.Subtotal,
            DiscountTotal = dto.DiscountTotal,
            GrandTotal = dto.GrandTotal,
            Discount = discount
        };
    }

    public static CartPreviewViewModel CreatePreview(CartDto dto, string placement = CartPreviewViewModel.NavbarPlacement, int previewLimit = 3)
    {
        var items = dto.Items
            .Take(Math.Max(1, previewLimit))
            .Select(item => new CartPreviewItemViewModel
            {
                ProductId = item.ProductId,
                Name = item.Name,
                Slug = item.Slug,
                ThumbnailUrl = item.ThumbnailUrl,
                Quantity = item.Quantity,
                LineTotal = item.LineTotal
            })
            .ToList();

        return new CartPreviewViewModel
        {
            AnonymousId = dto.AnonymousId,
            UserId = dto.UserId,
            Placement = placement,
            ItemCount = dto.Items.Count,
            Subtotal = dto.Subtotal,
            DiscountTotal = dto.DiscountTotal,
            GrandTotal = dto.GrandTotal,
            Items = items
        };
    }
}
