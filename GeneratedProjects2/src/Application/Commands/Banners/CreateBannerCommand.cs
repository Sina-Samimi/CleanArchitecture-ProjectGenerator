using System;
using System.Threading;
using System.Threading.Tasks;
using LogsDtoCloneTest.Application.Abstractions.Messaging;
using LogsDtoCloneTest.Application.Interfaces;
using LogsDtoCloneTest.Domain.Entities.Settings;
using LogsDtoCloneTest.SharedKernel.BaseTypes;

namespace LogsDtoCloneTest.Application.Commands.Banners;

public sealed record CreateBannerCommand(
    string Title,
    string ImagePath,
    string? LinkUrl,
    string? AltText,
    int DisplayOrder,
    bool IsActive,
    DateTimeOffset? StartDate,
    DateTimeOffset? EndDate,
    bool ShowOnHomePage) : ICommand<Guid>
{
    public sealed class Handler : ICommandHandler<CreateBannerCommand, Guid>
    {
        private readonly IBannerRepository _repository;
        private readonly IAuditContext _auditContext;

        public Handler(IBannerRepository repository, IAuditContext auditContext)
        {
            _repository = repository;
            _auditContext = auditContext;
        }

        public async Task<Result<Guid>> Handle(CreateBannerCommand request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.Title))
            {
                return Result<Guid>.Failure("عنوان بنر الزامی است.");
            }

            if (request.Title.Length > 200)
            {
                return Result<Guid>.Failure("عنوان بنر نمی‌تواند بیشتر از ۲۰۰ کاراکتر باشد.");
            }

            if (string.IsNullOrWhiteSpace(request.ImagePath))
            {
                return Result<Guid>.Failure("مسیر تصویر الزامی است.");
            }

            if (request.ImagePath.Length > 500)
            {
                return Result<Guid>.Failure("مسیر تصویر نمی‌تواند بیشتر از ۵۰۰ کاراکتر باشد.");
            }

            if (!string.IsNullOrWhiteSpace(request.LinkUrl) && request.LinkUrl.Length > 1000)
            {
                return Result<Guid>.Failure("لینک نمی‌تواند بیشتر از ۱۰۰۰ کاراکتر باشد.");
            }

            if (!string.IsNullOrWhiteSpace(request.AltText) && request.AltText.Length > 200)
            {
                return Result<Guid>.Failure("متن جایگزین نمی‌تواند بیشتر از ۲۰۰ کاراکتر باشد.");
            }

            if (request.StartDate.HasValue && request.EndDate.HasValue && request.StartDate.Value > request.EndDate.Value)
            {
                return Result<Guid>.Failure("تاریخ شروع نمی‌تواند بعد از تاریخ پایان باشد.");
            }

            var audit = _auditContext.Capture();
            var banner = new Banner(
                request.Title.Trim(),
                request.ImagePath.Trim(),
                request.LinkUrl,
                request.AltText,
                request.DisplayOrder,
                request.IsActive,
                request.StartDate,
                request.EndDate,
                request.ShowOnHomePage)
            {
                CreatorId = audit.UserId,
                CreateDate = audit.Timestamp,
                UpdateDate = audit.Timestamp,
                Ip = audit.IpAddress
            };

            await _repository.AddAsync(banner, cancellationToken);

            return Result<Guid>.Success(banner.Id);
        }
    }
}

