using System;
using System.Threading;
using System.Threading.Tasks;
using LogsDtoCloneTest.Application.Abstractions.Messaging;
using LogsDtoCloneTest.Application.Interfaces;
using LogsDtoCloneTest.SharedKernel.BaseTypes;

namespace LogsDtoCloneTest.Application.Commands.Catalog;

public sealed record UnmarkProductViolationReportAsReviewedCommand(
    Guid ReportId) : ICommand<bool>
{
    public sealed class Handler : ICommandHandler<UnmarkProductViolationReportAsReviewedCommand, bool>
    {
        private readonly IProductViolationReportRepository _reportRepository;

        public Handler(IProductViolationReportRepository reportRepository)
        {
            _reportRepository = reportRepository;
        }

        public async Task<Result<bool>> Handle(UnmarkProductViolationReportAsReviewedCommand request, CancellationToken cancellationToken)
        {
            if (request.ReportId == Guid.Empty)
            {
                return Result<bool>.Failure("شناسه گزارش معتبر نیست.");
            }

            var report = await _reportRepository.GetByIdAsync(request.ReportId, cancellationToken);
            if (report is null || report.IsDeleted)
            {
                return Result<bool>.Failure("گزارش مورد نظر یافت نشد.");
            }

            if (!report.IsReviewed)
            {
                return Result<bool>.Failure("این گزارش هنوز بررسی نشده است.");
            }

            report.UnmarkAsReviewed();
            await _reportRepository.UpdateAsync(report, cancellationToken);

            return Result<bool>.Success(true);
        }
    }
}

