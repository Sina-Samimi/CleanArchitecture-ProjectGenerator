using MobiRooz.Domain.Enums;

namespace MobiRooz.Application.DTOs.Catalog;

public sealed record ProductListFilterDto(
    int Page,
    int PageSize,
    string? SearchTerm,
    ProductType? Type,
    bool? IsPublished,
    decimal? MinPrice,
    decimal? MaxPrice,
    string? SellerId);
