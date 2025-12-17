using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LogsDtoCloneTest.Application.Abstractions.Messaging;
using LogsDtoCloneTest.Application.DTOs.Catalog;
using LogsDtoCloneTest.Application.Interfaces;
using LogsDtoCloneTest.SharedKernel.BaseTypes;

namespace LogsDtoCloneTest.Application.Queries.Catalog;

public sealed record GetProductViolationReportsQuery(
    int PageNumber = 1,
    int PageSize = 20,
    Guid? ProductId = null,
    string? SellerId = null,
    bool? IsReviewed = null,
    string? ReporterPhone = null,
    string? Subject = null,
    DateTimeOffset? DateFrom = null,
    DateTimeOffset? DateTo = null) : IQuery<ProductViolationReportListResultDto>;

public sealed record ProductViolationReportListResultDto(
    IReadOnlyCollection<ProductViolationReportDto> Reports,
    int TotalCount,
    int PageNumber,
    int PageSize);

public sealed record ProductViolationReportDto(
    Guid Id,
    Guid ProductId,
    string ProductName,
    string? ProductSellerId,
    Guid? ProductOfferId,
    string? SellerId,
    string? SellerName,
    string Subject,
    string Message,
    string ReporterId,
    string ReporterPhone,
    bool IsReviewed,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ReviewedAt,
    string? ReviewedById);

public sealed class GetProductViolationReportsQueryHandler : IQueryHandler<GetProductViolationReportsQuery, ProductViolationReportListResultDto>
{
    private readonly IProductViolationReportRepository _reportRepository;
    private readonly IProductOfferRepository _productOfferRepository;
    private readonly ISellerProfileRepository _sellerProfileRepository;

    public GetProductViolationReportsQueryHandler(
        IProductViolationReportRepository reportRepository,
        IProductOfferRepository productOfferRepository,
        ISellerProfileRepository sellerProfileRepository)
    {
        _reportRepository = reportRepository;
        _productOfferRepository = productOfferRepository;
        _sellerProfileRepository = sellerProfileRepository;
    }

    public async Task<Result<ProductViolationReportListResultDto>> Handle(
        GetProductViolationReportsQuery request,
        CancellationToken cancellationToken)
    {
        var pageNumber = request.PageNumber < 1 ? 1 : request.PageNumber;
        var pageSize = request.PageSize < 1 ? 20 : request.PageSize;

        IReadOnlyCollection<Domain.Entities.Catalog.ProductViolationReport> reports;

        if (request.ProductId.HasValue)
        {
            reports = await _reportRepository.GetByProductIdAsync(request.ProductId.Value, cancellationToken);
        }
        else if (!string.IsNullOrWhiteSpace(request.SellerId))
        {
            reports = await _reportRepository.GetBySellerIdAsync(request.SellerId, cancellationToken);
        }
        else
        {
            reports = await _reportRepository.GetAllAsync(cancellationToken, request.IsReviewed);
        }

        // Apply IsReviewed filter if needed
        if (request.IsReviewed.HasValue)
        {
            reports = reports.Where(r => r.IsReviewed == request.IsReviewed.Value).ToArray();
        }

        // Apply additional filters (reporter phone, subject, date range) in-memory
        if (!string.IsNullOrWhiteSpace(request.ReporterPhone))
        {
            var phone = request.ReporterPhone.Trim();
            reports = reports.Where(r => !string.IsNullOrWhiteSpace(r.ReporterPhone) && r.ReporterPhone.Contains(phone, StringComparison.OrdinalIgnoreCase)).ToArray();
        }

        if (!string.IsNullOrWhiteSpace(request.Subject))
        {
            var subj = request.Subject.Trim();
            reports = reports.Where(r => !string.IsNullOrWhiteSpace(r.Subject) && r.Subject.Contains(subj, StringComparison.OrdinalIgnoreCase)).ToArray();
        }

        if (request.DateFrom.HasValue)
        {
            reports = reports.Where(r => r.CreateDate >= request.DateFrom.Value).ToArray();
        }

        if (request.DateTo.HasValue)
        {
            reports = reports.Where(r => r.CreateDate <= request.DateTo.Value).ToArray();
        }

        var totalCount = reports.Count;

        // Apply pagination
        var paginatedReports = reports
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToArray();

        var dtos = new List<ProductViolationReportDto>();

        // Get seller profiles for all reports
        var sellerIds = paginatedReports
            .Where(r => !string.IsNullOrWhiteSpace(r.SellerId))
            .Select(r => r.SellerId!)
            .Distinct()
            .ToList();
        
        var sellerProfiles = await Task.WhenAll(
            sellerIds.Select(async id =>
            {
                var profile = await _sellerProfileRepository.GetByUserIdAsync(id, cancellationToken);
                return (id, profile);
            }));
        
        var sellerMap = sellerProfiles
            .Where(s => s.profile is not null)
            .ToDictionary(s => s.id, s => s.profile!);

        foreach (var report in paginatedReports)
        {
            string? sellerName = null;
            if (!string.IsNullOrWhiteSpace(report.SellerId) && sellerMap.TryGetValue(report.SellerId, out var sellerProfile))
            {
                sellerName = sellerProfile.DisplayName;
            }

            dtos.Add(new ProductViolationReportDto(
                report.Id,
                report.ProductId,
                report.Product?.Name ?? "نامشخص",
                report.Product?.SellerId,
                report.ProductOfferId,
                report.SellerId,
                sellerName,
                report.Subject,
                report.Message,
                report.ReporterId,
                report.ReporterPhone,
                report.IsReviewed,
                report.CreateDate,
                report.ReviewedAt,
                report.ReviewedById));
        }

        var result = new ProductViolationReportListResultDto(
            dtos,
            totalCount,
            pageNumber,
            pageSize);

        return Result<ProductViolationReportListResultDto>.Success(result);
    }
}

