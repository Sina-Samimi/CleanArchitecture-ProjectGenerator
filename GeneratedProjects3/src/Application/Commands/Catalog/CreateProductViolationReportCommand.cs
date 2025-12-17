using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LogTableRenameTest.Application.Abstractions.Messaging;
using LogTableRenameTest.Application.Commands.Notifications;
using LogTableRenameTest.Application.DTOs.Notifications;
using LogTableRenameTest.Application.Interfaces;
using LogTableRenameTest.Domain.Entities.Catalog;
using LogTableRenameTest.Domain.Enums;
using LogTableRenameTest.SharedKernel.BaseTypes;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace LogTableRenameTest.Application.Commands.Catalog;

public sealed record CreateProductViolationReportCommand(
    Guid ProductId,
    string Subject,
    string Message,
    string ReporterPhone,
    Guid? ProductOfferId = null,
    string? SellerId = null,
    string? ReporterId = null) : ICommand<Guid>
{
    public sealed class Handler : ICommandHandler<CreateProductViolationReportCommand, Guid>
    {
        private readonly IProductViolationReportRepository _reportRepository;
        private readonly IProductRepository _productRepository;
        private readonly IProductOfferRepository _productOfferRepository;
        private readonly IAuditContext _auditContext;
        private readonly IMediator _mediator;
        private readonly UserManager<Domain.Entities.ApplicationUser> _userManager;

        public Handler(
            IProductViolationReportRepository reportRepository,
            IProductRepository productRepository,
            IProductOfferRepository productOfferRepository,
            IAuditContext auditContext,
            IMediator mediator,
            UserManager<Domain.Entities.ApplicationUser> userManager)
        {
            _reportRepository = reportRepository;
            _productRepository = productRepository;
            _productOfferRepository = productOfferRepository;
            _auditContext = auditContext;
            _mediator = mediator;
            _userManager = userManager;
        }

        public async Task<Result<Guid>> Handle(CreateProductViolationReportCommand request, CancellationToken cancellationToken)
        {
            if (request.ProductId == Guid.Empty)
            {
                return Result<Guid>.Failure("شناسه محصول معتبر نیست.");
            }

            if (string.IsNullOrWhiteSpace(request.Subject))
            {
                return Result<Guid>.Failure("موضوع تخلف الزامی است.");
            }

            if (request.Subject.Length > 200)
            {
                return Result<Guid>.Failure("موضوع تخلف نمی‌تواند بیش از ۲۰۰ کاراکتر باشد.");
            }

            if (string.IsNullOrWhiteSpace(request.Message))
            {
                return Result<Guid>.Failure("پیام الزامی است.");
            }

            if (request.Message.Length > 2000)
            {
                return Result<Guid>.Failure("پیام نمی‌تواند بیش از ۲۰۰۰ کاراکتر باشد.");
            }

            if (string.IsNullOrWhiteSpace(request.ReporterPhone))
            {
                return Result<Guid>.Failure("شماره تماس گزارش دهنده الزامی است.");
            }

            if (request.ReporterPhone.Length > 20)
            {
                return Result<Guid>.Failure("شماره تماس نمی‌تواند بیش از ۲۰ کاراکتر باشد.");
            }

            // Verify product exists
            var product = await _productRepository.GetByIdAsync(request.ProductId, cancellationToken);
            if (product is null || product.IsDeleted)
            {
                return Result<Guid>.Failure("محصول مورد نظر یافت نشد.");
            }

            // If ProductOfferId is provided, verify it exists and belongs to the product
            if (request.ProductOfferId.HasValue && request.ProductOfferId.Value != Guid.Empty)
            {
                var productOffer = await _productOfferRepository.GetByIdAsync(request.ProductOfferId.Value, cancellationToken);
                if (productOffer is null || productOffer.IsDeleted)
                {
                    return Result<Guid>.Failure("پیشنهاد محصول یافت نشد.");
                }

                if (productOffer.ProductId != request.ProductId)
                {
                    return Result<Guid>.Failure("پیشنهاد محصول با محصول انتخاب شده مطابقت ندارد.");
                }
            }

            // Always use the seller ID from the product itself, not from offers
            var sellerId = product.SellerId;

            var audit = _auditContext.Capture();
            var reporterId = request.ReporterId ?? audit.UserId ?? "anonymous";

            var violationReport = new ProductViolationReport(
                request.ProductId,
                request.Subject,
                request.Message,
                reporterId,
                request.ReporterPhone,
                request.ProductOfferId,
                sellerId);

            violationReport.CreatorId = audit.UserId ?? "system";
            violationReport.Ip = audit.IpAddress;
            violationReport.CreateDate = audit.Timestamp;
            violationReport.UpdateDate = audit.Timestamp;
            violationReport.IsDeleted = false;

            await _reportRepository.AddAsync(violationReport, cancellationToken);

            // Create notifications for admin and seller
            await CreateNotificationsAsync(product, sellerId, violationReport, cancellationToken);

            return Result<Guid>.Success(violationReport.Id);
        }

        private async Task CreateNotificationsAsync(
            Product product,
            string? sellerId,
            ProductViolationReport report,
            CancellationToken cancellationToken)
        {
            var targetUserIds = new List<string>();

            // Add admin users only - EXCLUDE the reporter
            var adminUsers = await _userManager.GetUsersInRoleAsync("Admin");
            var adminUserIds = adminUsers
                .Where(u => u.Id != report.ReporterId) // Exclude the reporter even if they are an admin
                .Select(u => u.Id)
                .ToList();
            targetUserIds.AddRange(adminUserIds);

            // Add only the seller of the product (if sellerId is provided and is not the reporter)
            if (!string.IsNullOrWhiteSpace(sellerId) && sellerId != report.ReporterId)
            {
                targetUserIds.Add(sellerId);
            }

            if (targetUserIds.Count == 0)
            {
                return;
            }

            var notificationTitle = $"گزارش تخلف جدید برای محصول: {product.Name}";
            var notificationMessage = $"موضوع: {report.Subject}\n\n{report.Message}";

            var notificationDto = new CreateNotificationDto(
                notificationTitle,
                notificationMessage,
                NotificationType.Warning,
                NotificationPriority.High,
                null,
                new NotificationFilterDto(SelectedUserIds: targetUserIds.Distinct().ToArray()));

            var notificationCommand = new CreateNotificationCommand(notificationDto);
            await _mediator.Send(notificationCommand, cancellationToken);
        }
    }
}

