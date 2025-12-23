using System;
using System.Threading;
using System.Threading.Tasks;
using MobiRooz.Application.Abstractions.Messaging;
using MobiRooz.Application.Interfaces;
using MobiRooz.SharedKernel.BaseTypes;

namespace MobiRooz.Application.Commands.Catalog;

public sealed record MarkProductViolationReportAsReviewedCommand(
    Guid ReportId,
    string ReviewedById) : ICommand<bool>
{
    public sealed class Handler : ICommandHandler<MarkProductViolationReportAsReviewedCommand, bool>
    {
        private readonly IProductViolationReportRepository _reportRepository;

        public Handler(IProductViolationReportRepository reportRepository)
        {
            _reportRepository = reportRepository;
        }

        public async Task<Result<bool>> Handle(MarkProductViolationReportAsReviewedCommand request, CancellationToken cancellationToken)
        {
            if (request.ReportId == Guid.Empty)
            {
                return Result<bool>.Failure("شناسه گزارش معتبر نیست.");
            }

            if (string.IsNullOrWhiteSpace(request.ReviewedById))
            {
                return Result<bool>.Failure("شناسه کاربری بررسی‌کننده معتبر نیست.");
            }

            var report = await _reportRepository.GetByIdAsync(request.ReportId, cancellationToken);
            if (report is null || report.IsDeleted)
            {
                return Result<bool>.Failure("گزارش مورد نظر یافت نشد.");
            }

            if (report.IsReviewed)
            {
                return Result<bool>.Failure("این گزارش قبلاً بررسی شده است.");
            }

            report.MarkAsReviewed(request.ReviewedById);
            await _reportRepository.UpdateAsync(report, cancellationToken);

            return Result<bool>.Success(true);
        }
    }
}

