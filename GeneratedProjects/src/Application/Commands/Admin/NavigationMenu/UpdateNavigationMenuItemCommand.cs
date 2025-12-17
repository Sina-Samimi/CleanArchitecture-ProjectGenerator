using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TestAttarClone.Application.Abstractions.Messaging;
using TestAttarClone.Application.Interfaces;
using TestAttarClone.Domain.Entities.Navigation;
using TestAttarClone.SharedKernel.BaseTypes;

namespace TestAttarClone.Application.Commands.Admin.NavigationMenu;

public sealed record UpdateNavigationMenuItemCommand(
    Guid Id,
    string Title,
    string Url,
    string Icon,
    bool IsVisible,
    bool OpenInNewTab,
    int DisplayOrder,
    Guid? ParentId,
    string? ImageUrl = null) : ICommand
{
    public sealed class Handler : ICommandHandler<UpdateNavigationMenuItemCommand>
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

        public async Task<Result> Handle(UpdateNavigationMenuItemCommand request, CancellationToken cancellationToken)
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

            var menuItem = await _repository.GetByIdAsync(request.Id, cancellationToken);

            if (menuItem is null)
            {
                return Result.Failure("آیتم منو یافت نشد.");
            }

            if (parentId.HasValue)
            {
                if (parentId.Value == request.Id)
                {
                    return Result.Failure("آیتم نمی‌تواند والد خودش باشد.");
                }

                var parent = await _repository.GetByIdAsync(parentId.Value, cancellationToken);

                if (parent is null)
                {
                    return Result.Failure("آیتم والد انتخاب‌شده معتبر نیست.");
                }

                var allItems = await _repository.GetAllAsync(cancellationToken);

                var descendants = CollectDescendants(request.Id, allItems);

                if (descendants.Contains(parentId.Value))
                {
                    return Result.Failure("نمی‌توان یک زیرمنو را به فرزند خودش منتقل کرد.");
                }
            }

            var audit = _auditContext.Capture();

            menuItem.UpdateDetails(title, url, icon, request.IsVisible, request.OpenInNewTab, displayOrder, request.ImageUrl);
            menuItem.SetParent(parentId);
            menuItem.UpdaterId = audit.UserId;
            menuItem.UpdateDate = audit.Timestamp;
            menuItem.Ip = audit.IpAddress;

            await _repository.UpdateAsync(menuItem, cancellationToken);

            return Result.Success();
        }

        private static HashSet<Guid> CollectDescendants(Guid rootId, IReadOnlyList<NavigationMenuItem> items)
        {
            var lookup = items
                .Where(item => item.ParentId.HasValue)
                .GroupBy(item => item.ParentId!.Value)
                .ToDictionary(group => group.Key, group => group.Select(child => child.Id).ToList());

            var visited = new HashSet<Guid>();
            var stack = new Stack<Guid>();
            stack.Push(rootId);

            while (stack.Count > 0)
            {
                var current = stack.Pop();

                if (lookup.TryGetValue(current, out var children))
                {
                    foreach (var child in children)
                    {
                        if (visited.Add(child))
                        {
                            stack.Push(child);
                        }
                    }
                }
            }

            return visited;
        }
    }
}
