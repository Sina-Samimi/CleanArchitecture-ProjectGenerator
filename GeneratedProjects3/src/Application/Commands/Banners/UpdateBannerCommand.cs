using System;
using System.Threading;
using System.Threading.Tasks;
using LogTableRenameTest.Application.Abstractions.Messaging;
using LogTableRenameTest.Application.Interfaces;
using LogTableRenameTest.SharedKernel.BaseTypes;

namespace LogTableRenameTest.Application.Commands.Banners;

public sealed record UpdateBannerCommand(
    Guid Id,
    string Title,
    string ImagePath,
    string? LinkUrl,
    string? AltText,
    int DisplayOrder,
    bool IsActive,
    DateTimeOffset? StartDate,
    DateTimeOffset? EndDate,
    bool ShowOnHomePage) : ICommand
{
    public sealed class Handler : ICommandHandler<UpdateBannerCommand>
    {
        private readonly IBannerRepository _repository;
        private readonly IAuditContext _auditContext;

        public Handler(IBannerRepository repository, IAuditContext auditContext)
        {
            _repository = repository;
            _auditContext = auditContext;
        }

        public async Task<Result> Handle(UpdateBannerCommand request, CancellationToken cancellationToken)
        {
            if (request.Id == Guid.Empty)
            {
                return Result.Failure("شناسه بنر معتبر نیست.");
            }

            if (string.IsNullOrWhiteSpace(request.Title))
            {
                return Result.Failure("عنوان بنر الزامی است.");
            }

            if (request.Title.Length > 200)
            {
                return Result.Failure("عنوان بنر نمی‌تواند بیشتر از ۲۰۰ کاراکتر باشد.");
            }

            if (string.IsNullOrWhiteSpace(request.ImagePath))
            {
                return Result.Failure("مسیر تصویر الزامی است.");
            }

            if (request.ImagePath.Length > 500)
            {
                return Result.Failure("مسیر تصویر نمی‌تواند بیشتر از ۵۰۰ کاراکتر باشد.");
            }

            if (!string.IsNullOrWhiteSpace(request.LinkUrl) && request.LinkUrl.Length > 1000)
            {
                return Result.Failure("لینک نمی‌تواند بیشتر از ۱۰۰۰ کاراکتر باشد.");
            }

            if (!string.IsNullOrWhiteSpace(request.AltText) && request.AltText.Length > 200)
            {
                return Result.Failure("متن جایگزین نمی‌تواند بیشتر از ۲۰۰ کاراکتر باشد.");
            }

            if (request.StartDate.HasValue && request.EndDate.HasValue && request.StartDate.Value > request.EndDate.Value)
            {
                return Result.Failure("تاریخ شروع نمی‌تواند بعد از تاریخ پایان باشد.");
            }

            var banner = await _repository.GetByIdForUpdateAsync(request.Id, cancellationToken);
            if (banner is null)
            {
                return Result.Failure("بنر یافت نشد.");
            }

            var audit = _auditContext.Capture();
            banner.Update(
                request.Title.Trim(),
                request.ImagePath.Trim(),
                request.LinkUrl,
                request.AltText,
                request.DisplayOrder,
                request.IsActive,
                request.StartDate,
                request.EndDate,
                request.ShowOnHomePage);

            banner.UpdaterId = audit.UserId;
            banner.UpdateDate = audit.Timestamp;
            banner.Ip = audit.IpAddress;

            await _repository.UpdateAsync(banner, cancellationToken);

            return Result.Success();
        }
    }
}

