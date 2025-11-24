using System;
using Arsis.Domain.Enums;

namespace Arsis.Application.DTOs.Blogs;

public sealed record BlogListFilterDto(
    string? Search,
    Guid? CategoryId,
    Guid? AuthorId,
    BlogStatus? Status,
    DateOnly? FromDate,
    DateOnly? ToDate,
    int Page,
    int PageSize);
