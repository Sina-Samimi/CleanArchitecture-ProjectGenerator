using System;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using LogsDtoCloneTest.Application.Commands.Contacts;
using LogsDtoCloneTest.Application.Queries.Contacts;
using LogsDtoCloneTest.WebSite.Areas.Admin.Models;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace LogsDtoCloneTest.WebSite.Areas.Admin.Controllers;

[Area("Admin")]
public sealed class ContactMessagesController : Controller
{
    private readonly IMediator _mediator;

    public ContactMessagesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        int page = 1,
        bool unreadOnly = false,
        CancellationToken cancellationToken = default)
    {
        ViewData["Title"] = "پیام‌های تماس";
        ViewData["Subtitle"] = "مدیریت و پاسخگویی به پیام‌های تماس با ما";

        var pageNumber = page < 1 ? 1 : page;
        var pageSize = 20;

        var query = new GetContactMessagesQuery(pageNumber, pageSize, unreadOnly);
        var result = await _mediator.Send(query, cancellationToken);

        if (!result.IsSuccess || result.Value is null)
        {
            TempData["Error"] = result.Error ?? "دریافت لیست پیام‌ها با خطا مواجه شد.";
            return View(new ContactMessagesListViewModel
            {
                Messages = Array.Empty<ContactMessageViewModel>(),
                TotalCount = 0,
                PageNumber = 1,
                PageSize = pageSize,
                UnreadOnly = false
            });
        }

        var data = result.Value;
        var viewModel = new ContactMessagesListViewModel
        {
            Messages = data.Messages.Select(m => new ContactMessageViewModel
            {
                Id = m.Id,
                UserId = m.UserId,
                FullName = m.FullName,
                Email = m.Email,
                Phone = m.Phone,
                Subject = m.Subject,
                Message = m.Message,
                IsRead = m.IsRead,
                ReadAt = m.ReadAt,
                ReadByUserId = m.ReadByUserId,
                AdminReply = m.AdminReply,
                RepliedAt = m.RepliedAt,
                RepliedByUserId = m.RepliedByUserId,
                CreateDate = m.CreateDate
            }).ToArray(),
            TotalCount = data.TotalCount,
            PageNumber = data.PageNumber,
            PageSize = data.PageSize,
            UnreadOnly = unreadOnly
        };

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAsRead(
        Guid id,
        CancellationToken cancellationToken)
    {
        if (id == Guid.Empty)
        {
            TempData["Error"] = "شناسه پیام معتبر نیست.";
            return RedirectToAction(nameof(Index));
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            TempData["Error"] = "شناسه کاربر یافت نشد.";
            return RedirectToAction(nameof(Index));
        }

        var command = new MarkContactMessageAsReadCommand(id, userId);
        var result = await _mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
        {
            TempData["Error"] = result.Error ?? "در به‌روزرسانی وضعیت پیام خطایی رخ داد.";
        }
        else
        {
            TempData["Success"] = "پیام به عنوان خوانده شده علامت‌گذاری شد.";
        }

        return RedirectToAction(nameof(Index));
    }
}

