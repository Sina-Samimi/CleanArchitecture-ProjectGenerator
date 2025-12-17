using System;
using System.Threading;
using System.Threading.Tasks;
using LogTableRenameTest.Application.Abstractions.Messaging;
using LogTableRenameTest.Application.Interfaces;
using LogTableRenameTest.Domain.Entities.Navigation;
using LogTableRenameTest.SharedKernel.BaseTypes;

namespace LogTableRenameTest.Application.Commands.Admin.NavigationMenu;

public sealed record CreateNavigationMenuItemCommand(
    string Title,
    string Url,
    string Icon,
    bool IsVisible,
    bool OpenInNewTab,
    int DisplayOrder,
    Guid? ParentId,
    string? ImageUrl = null) : ICommand
{
    public sealed class Handler : ICommandHandler<CreateNavigationMenuItemCommand>
    {
        private const int MaxTitleLength = 200;
        private const int MaxUrlLength = 500;
        private const int MaxIconLength = 100;

        private readonly INavigationMenuRepository _repository;
        private readonly IAuditContext _auditContext;

        public Handler(INavigationMenuRepository repository, IAuditContext auditContext)
        {
            _repository = repository;
            _auditContext = auditContext;
        }

        public async Task<Result> Handle(CreateNavigationMenuItemCommand request, CancellationToken cancellationToken)
        {
            var title = request.Title?.Trim() ?? string.Empty;
            var url = request.Url?.Trim() ?? string.Empty;
            var icon = request.Icon?.Trim() ?? string.Empty;
            var displayOrder = request.DisplayOrder;
            var parentId = request.ParentId;

            if (string.IsNullOrWhiteSpace(title))
            {
                return Result.Failure("عنوان منو الزامی است.");
            }

            if (title.Length > MaxTitleLength)
            {
                return Result.Failure($"عنوان منو نمی‌تواند بیشتر از {MaxTitleLength} کاراکتر باشد.");
            }

            if (url.Length > MaxUrlLength)
            {
                return Result.Failure($"آدرس لینک نمی‌تواند بیشتر از {MaxUrlLength} کاراکتر باشد.");
            }

            if (icon.Length > MaxIconLength)
            {
                return Result.Failure($"آیکون نمی‌تواند بیشتر از {MaxIconLength} کاراکتر باشد.");
            }

            if (displayOrder < 0)
            {
                return Result.Failure("ترتیب نمایش نمی‌تواند منفی باشد.");
            }

            if (parentId.HasValue)
            {
                var parent = await _repository.GetByIdAsync(parentId.Value, cancellationToken);

                if (parent is null)
                {
                    return Result.Failure("آیتم والد انتخاب‌شده معتبر نیست.");
                }
            }

            var audit = _auditContext.Capture();

            var menuItem = new NavigationMenuItem(
                title,
                url,
                icon,
                request.IsVisible,
                request.OpenInNewTab,
                displayOrder,
                parentId,
                request.ImageUrl)
            {
                CreatorId = audit.UserId,
                CreateDate = audit.Timestamp,
                UpdateDate = audit.Timestamp,
                Ip = audit.IpAddress
            };

            await _repository.AddAsync(menuItem, cancellationToken);

            return Result.Success();
        }
    }
}
