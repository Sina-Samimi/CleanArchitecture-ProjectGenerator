using System;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using TestAttarClone.Application.Commands.Tickets;
using TestAttarClone.Application.Queries.Identity.GetUserLookups;
using TestAttarClone.Application.Queries.Tickets;
using TestAttarClone.Domain.Enums;
using TestAttarClone.WebSite.Areas.Admin.Models;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace TestAttarClone.WebSite.Areas.Admin.Controllers;

[Area("Admin")]
public sealed class TicketsController : Controller
{
    private readonly IMediator _mediator;

    public TicketsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        [FromQuery] string? userId,
        [FromQuery] TicketStatus? status,
        [FromQuery] string? assignedToId,
        [FromQuery] int page = 1,
        CancellationToken cancellationToken = default)
    {
        ViewData["Title"] = "مدیریت تیکت‌ها";
        ViewData["Subtitle"] = "رسیدگی به تیکت‌های کاربران";

        var pageNumber = page < 1 ? 1 : page;
        var pageSize = 20;

        var query = new GetTicketsQuery(userId, status, assignedToId, pageNumber, pageSize);
        var result = await _mediator.Send(query, cancellationToken);

        if (!result.IsSuccess || result.Value is null)
        {
            TempData["Error"] = result.Error ?? "دریافت لیست تیکت‌ها با خطا مواجه شد.";
            return View(new TicketListViewModel
            {
                Tickets = Array.Empty<TicketViewModel>(),
                TotalCount = 0,
                PageNumber = 1,
                PageSize = pageSize,
                TotalPages = 0
            });
        }

        var data = result.Value;
        var viewModel = new TicketListViewModel
        {
            Tickets = data.Tickets.Select(t => new TicketViewModel
            {
                Id = t.Id,
                UserId = t.UserId,
                UserName = t.UserName,
                UserFullName = t.UserFullName,
                UserPhoneNumber = t.UserPhoneNumber,
                Subject = t.Subject,
                Message = t.Message,
                Department = t.Department,
                AttachmentPath = t.AttachmentPath,
                Status = t.Status,
                AssignedToId = t.AssignedToId,
                AssignedToName = t.AssignedToName,
                CreateDate = t.CreateDate,
                LastReplyDate = t.LastReplyDate,
                HasUnreadReplies = t.HasUnreadReplies,
                RepliesCount = t.RepliesCount
            }).ToArray(),
            TotalCount = data.TotalCount,
            PageNumber = data.PageNumber,
            PageSize = data.PageSize,
            TotalPages = data.TotalPages,
            Filter = new TicketFilterViewModel
            {
                UserId = userId,
                Status = status,
                AssignedToId = assignedToId
            }
        };

        return View(viewModel);
    }

    [HttpGet]
    public async Task<IActionResult> Details(Guid id, CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
        {
            TempData["Error"] = "شناسه تیکت معتبر نیست.";
            return RedirectToAction(nameof(Index));
        }

        ViewData["Title"] = "جزئیات تیکت";
        ViewData["Subtitle"] = "مشاهده و پاسخ به تیکت";

        var query = new GetTicketByIdQuery(id);
        var result = await _mediator.Send(query, cancellationToken);

        if (!result.IsSuccess || result.Value is null)
        {
            TempData["Error"] = result.Error ?? "تیکت یافت نشد.";
            return RedirectToAction(nameof(Index));
        }

        var ticket = result.Value;
        
        // Get admin users for dropdown
        var adminUsersResult = await _mediator.Send(new GetAdminUserLookupsQuery(500), cancellationToken);
        var adminUserOptions = new List<SelectListItem>();
        
        if (adminUsersResult.IsSuccess && adminUsersResult.Value is not null)
        {
            adminUserOptions = adminUsersResult.Value
                .Select(admin => new SelectListItem
                {
                    Value = admin.Id,
                    Text = string.IsNullOrWhiteSpace(admin.Email) || admin.Email == admin.DisplayName
                        ? admin.DisplayName + (admin.IsActive ? "" : " (غیرفعال)")
                        : $"{admin.DisplayName} ({admin.Email})" + (admin.IsActive ? "" : " (غیرفعال)"),
                    Selected = !string.IsNullOrWhiteSpace(ticket.AssignedToId) && 
                               string.Equals(admin.Id, ticket.AssignedToId, StringComparison.OrdinalIgnoreCase)
                })
                .OrderBy(item => item.Text)
                .ToList();
        }
        
        // Add empty option
        adminUserOptions.Insert(0, new SelectListItem("-- انتخاب کارشناس --", "", string.IsNullOrWhiteSpace(ticket.AssignedToId)));

        var viewModel = new TicketDetailViewModel
        {
            Id = ticket.Id,
            UserId = ticket.UserId,
            UserName = ticket.UserName,
            UserFullName = ticket.UserFullName,
            UserPhoneNumber = ticket.UserPhoneNumber,
            Subject = ticket.Subject,
            Message = ticket.Message,
            Department = ticket.Department,
            AttachmentPath = ticket.AttachmentPath,
            Status = ticket.Status,
            AssignedToId = ticket.AssignedToId,
            AssignedToName = ticket.AssignedToName,
            CreateDate = ticket.CreateDate,
            LastReplyDate = ticket.LastReplyDate,
            HasUnreadReplies = ticket.HasUnreadReplies,
            Replies = ticket.Replies.Select(r => new TicketReplyViewModel
            {
                Id = r.Id,
                TicketId = r.TicketId,
                Message = r.Message,
                IsFromAdmin = r.IsFromAdmin,
                RepliedById = r.RepliedById,
                RepliedByName = r.RepliedByName,
                CreateDate = r.CreateDate
            }).ToArray(),
            ReplyForm = new CreateTicketReplyViewModel
            {
                TicketId = ticket.Id
            },
            AdminUserOptions = adminUserOptions
        };

        // Mark as read when admin views it
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var currentUserName = User.FindFirstValue(ClaimTypes.Name) ?? User.FindFirstValue("fullname") ?? User.FindFirstValue("name");
        
        viewModel.CurrentUserName = currentUserName;

        if (!string.IsNullOrWhiteSpace(currentUserId) && ticket.AssignedToId == currentUserId)
        {
            await _mediator.Send(new MarkTicketAsReadCommand(id), cancellationToken);
        }

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reply(
        Guid ticketId,
        string message,
        bool? closeTicket = null,
        CancellationToken cancellationToken = default)
    {
        if (ticketId == Guid.Empty)
        {
            TempData["Error"] = "شناسه تیکت معتبر نیست.";
            return RedirectToAction(nameof(Index));
        }

        if (string.IsNullOrWhiteSpace(message))
        {
            TempData["Error"] = "متن پاسخ الزامی است.";
            return RedirectToAction(nameof(Details), new { id = ticketId });
        }

        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var command = new CreateTicketReplyCommand(ticketId, message, IsFromAdmin: true, RepliedById: currentUserId);
        var result = await _mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
        {
            TempData["Error"] = result.Error ?? "ارسال پاسخ با خطا مواجه شد.";
        }
        else
        {
            // If closeTicket is true, also close the ticket
            if (closeTicket == true)
            {
                var statusCommand = new UpdateTicketStatusCommand(ticketId, TicketStatus.Closed, null);
                var statusResult = await _mediator.Send(statusCommand, cancellationToken);
                if (statusResult.IsSuccess)
                {
                    TempData["Success"] = "پاسخ ارسال شد و تیکت بسته شد.";
                }
                else
                {
                    TempData["Success"] = "پاسخ با موفقیت ارسال شد.";
                    TempData["Warning"] = "تیکت بسته نشد: " + (statusResult.Error ?? "خطای نامشخص");
                }
        }
        else
        {
            TempData["Success"] = "پاسخ با موفقیت ارسال شد.";
            }
        }

        return RedirectToAction(nameof(Details), new { id = ticketId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(
        Guid id,
        TicketStatus status,
        string? assignedToId,
        CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
        {
            TempData["Error"] = "شناسه تیکت معتبر نیست.";
            return RedirectToAction(nameof(Index));
        }

        // اگر وضعیت "پاسخ داده شده" انتخاب شده باشد، شناسه کارشناس فعلی خودکار تنظیم شود
        if (status == TicketStatus.Answered && string.IsNullOrWhiteSpace(assignedToId))
        {
            assignedToId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        }

        var command = new UpdateTicketStatusCommand(id, status, assignedToId);
        var result = await _mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
        {
            TempData["Error"] = result.Error ?? "بروزرسانی وضعیت تیکت با خطا مواجه شد.";
        }
        else
        {
            TempData["Success"] = "وضعیت تیکت با موفقیت بروزرسانی شد.";
        }

        return RedirectToAction(nameof(Details), new { id });
    }
}
