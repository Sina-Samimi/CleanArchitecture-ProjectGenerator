using System;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using LogsDtoCloneTest.Application.Commands.Tickets;
using LogsDtoCloneTest.Application.Interfaces;
using LogsDtoCloneTest.Application.Queries.Tickets;
using LogsDtoCloneTest.WebSite.Areas.User.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using LogsDtoCloneTest.Domain.Entities;

namespace LogsDtoCloneTest.WebSite.Areas.User.Controllers;

[Area("User")]
[Authorize]
public sealed class TicketsController : Controller
{
    private readonly IMediator _mediator;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IFormFileSettingServices _fileSettingServices;

    public TicketsController(
        IMediator mediator, 
        UserManager<ApplicationUser> userManager,
        IFormFileSettingServices fileSettingServices)
    {
        _mediator = mediator;
        _userManager = userManager;
        _fileSettingServices = fileSettingServices;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        [FromQuery] int page = 1,
        CancellationToken cancellationToken = default)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return RedirectToAction("Index", "Home");
        }

        ViewData["Title"] = "تیکت‌های من";
        ViewData["Subtitle"] = "مدیریت تیکت‌های پشتیبانی";

        var pageNumber = page < 1 ? 1 : page;
        var pageSize = 20;

        var query = new GetTicketsQuery(UserId: user.Id, PageNumber: pageNumber, PageSize: pageSize);
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
        
        // Get status counts
        var allTicketsQuery = new GetTicketsQuery(UserId: user.Id, PageNumber: 1, PageSize: int.MaxValue);
        var allTicketsResult = await _mediator.Send(allTicketsQuery, cancellationToken);
        var allTickets = allTicketsResult.IsSuccess && allTicketsResult.Value is not null 
            ? allTicketsResult.Value.Tickets 
            : Array.Empty<Application.DTOs.Tickets.TicketDto>();
        
        var statusCounts = new TicketStatusCounts
        {
            OpenCount = allTickets.Count(t => t.Status == Domain.Enums.TicketStatus.Pending),
            InProgressCount = allTickets.Count(t => t.Status == Domain.Enums.TicketStatus.InProgress),
            AnsweredCount = allTickets.Count(t => t.Status == Domain.Enums.TicketStatus.Answered),
            ClosedCount = allTickets.Count(t => t.Status == Domain.Enums.TicketStatus.Closed)
        };
        
        var viewModel = new TicketListViewModel
        {
            Tickets = data.Tickets.Select(t => new TicketViewModel
            {
                Id = t.Id,
                Subject = t.Subject,
                Message = t.Message,
                Department = t.Department,
                Status = t.Status,
                CreateDate = t.CreateDate,
                LastReplyDate = t.LastReplyDate,
                HasUnreadReplies = t.HasUnreadReplies,
                RepliesCount = t.RepliesCount,
                TicketNumber = $"#{t.Id.ToString("N").Substring(0, 8).ToUpper()}"
            }).ToArray(),
            TotalCount = data.TotalCount,
            PageNumber = data.PageNumber,
            PageSize = data.PageSize,
            TotalPages = data.TotalPages,
            StatusCounts = statusCounts
        };

        return View(viewModel);
    }

    [HttpGet]
    public async Task<IActionResult> Create([FromQuery] string? department)
    {
        ViewData["Title"] = "ثبت تیکت جدید";
        ViewData["Subtitle"] = "ارسال درخواست پشتیبانی";

        var viewModel = new TicketDetailViewModel
        {
            CreateForm = new CreateTicketViewModel
            {
                SelectedDepartment = department
            }
        };

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        string subject,
        string message,
        string? department,
        IFormFile? attachment,
        CancellationToken cancellationToken = default)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return RedirectToAction("Index", "Home");
        }

        if (string.IsNullOrWhiteSpace(subject))
        {
            TempData["Error"] = "موضوع تیکت الزامی است.";
            return View(new TicketDetailViewModel
            {
                CreateForm = new CreateTicketViewModel
                {
                    Subject = subject ?? string.Empty,
                    Message = message ?? string.Empty,
                    Department = department,
                    SelectedDepartment = department
                }
            });
        }

        if (string.IsNullOrWhiteSpace(message))
        {
            TempData["Error"] = "متن تیکت الزامی است.";
            return View(new TicketDetailViewModel
            {
                CreateForm = new CreateTicketViewModel
                {
                    Subject = subject,
                    Message = message ?? string.Empty,
                    Department = department,
                    SelectedDepartment = department
                }
            });
        }

        // Handle file upload
        string? attachmentPath = null;
        if (attachment != null && attachment.Length > 0)
        {
            if (!_fileSettingServices.IsFileSizeValid(attachment, 10240)) // 10MB max
            {
                TempData["Error"] = "حجم فایل باید کمتر از ۱۰ مگابایت باشد.";
                return View(new TicketDetailViewModel
                {
                    CreateForm = new CreateTicketViewModel
                    {
                        Subject = subject,
                        Message = message,
                        Department = department,
                        SelectedDepartment = department
                    }
                });
            }

            var fileResponse = _fileSettingServices.UploadFile("tickets/attachments", attachment, Guid.NewGuid().ToString("N"));
            if (fileResponse.Success && !string.IsNullOrWhiteSpace(fileResponse.Data))
            {
                attachmentPath = fileResponse.Data;
            }
            else
            {
                TempData["Error"] = fileResponse.Messages.FirstOrDefault()?.message ?? "خطا در آپلود فایل.";
                return View(new TicketDetailViewModel
                {
                    CreateForm = new CreateTicketViewModel
                    {
                        Subject = subject,
                        Message = message,
                        Department = department,
                        SelectedDepartment = department
                    }
                });
            }
        }

        var command = new CreateTicketCommand(user.Id, subject, message, department, attachmentPath);
        var result = await _mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
        {
            TempData["Error"] = result.Error ?? "ایجاد تیکت با خطا مواجه شد.";
            return View(new TicketDetailViewModel
            {
                CreateForm = new CreateTicketViewModel
                {
                    Subject = subject,
                    Message = message,
                    Department = department
                }
            });
        }

        TempData["Success"] = "تیکت با موفقیت ایجاد شد.";
        return RedirectToAction(nameof(Details), new { id = result.Value!.Id });
    }

    [HttpGet]
    public async Task<IActionResult> Details(Guid id, CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
        {
            TempData["Error"] = "شناسه تیکت معتبر نیست.";
            return RedirectToAction(nameof(Index));
        }

        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return RedirectToAction("Index", "Home");
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

        // Verify ticket belongs to user
        if (ticket.UserId != user.Id)
        {
            TempData["Error"] = "شما دسترسی به این تیکت ندارید.";
            return RedirectToAction(nameof(Index));
        }

        var viewModel = new TicketDetailViewModel
        {
            Id = ticket.Id,
            Subject = ticket.Subject,
            Message = ticket.Message,
            Department = ticket.Department,
            AttachmentPath = ticket.AttachmentPath,
            Status = ticket.Status,
            CreateDate = ticket.CreateDate,
            LastReplyDate = ticket.LastReplyDate,
            HasUnreadReplies = ticket.HasUnreadReplies,
            Replies = ticket.Replies.Select(r => new TicketReplyViewModel
            {
                Id = r.Id,
                TicketId = r.TicketId,
                Message = r.Message,
                IsFromAdmin = r.IsFromAdmin,
                RepliedByName = r.RepliedByName,
                CreateDate = r.CreateDate
            }).ToArray(),
            ReplyForm = new CreateTicketReplyViewModel
            {
                TicketId = ticket.Id
            },
            CurrentUserName = user.FullName
        };

        // Mark as read when user views it
        await _mediator.Send(new MarkTicketAsReadCommand(id), cancellationToken);

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reply(
        Guid ticketId,
        string message,
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

        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return RedirectToAction("Index", "Home");
        }

        // Verify ticket belongs to user
        var ticketQuery = new GetTicketByIdQuery(ticketId);
        var ticketResult = await _mediator.Send(ticketQuery, cancellationToken);
        if (!ticketResult.IsSuccess || ticketResult.Value is null || ticketResult.Value.UserId != user.Id)
        {
            TempData["Error"] = "شما دسترسی به این تیکت ندارید.";
            return RedirectToAction(nameof(Index));
        }

        var command = new CreateTicketReplyCommand(ticketId, message, IsFromAdmin: false, RepliedById: user.Id);
        var result = await _mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
        {
            TempData["Error"] = result.Error ?? "ارسال پاسخ با خطا مواجه شد.";
        }
        else
        {
            TempData["Success"] = "پاسخ با موفقیت ارسال شد.";
        }

        return RedirectToAction(nameof(Details), new { id = ticketId });
    }
}
