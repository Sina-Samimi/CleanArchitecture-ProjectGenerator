using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using TestAttarClone.Application.Queries.Billing;
using TestAttarClone.Application.Queries.Catalog;
using TestAttarClone.Application.Queries.Notifications;
using TestAttarClone.Application.Queries.Tickets;
using TestAttarClone.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace TestAttarClone.WebSite.ViewComponents;

public sealed class AdminNotificationsDropdownViewComponent : ViewComponent
{
    private readonly IMediator _mediator;

    public AdminNotificationsDropdownViewComponent(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var cancellationToken = HttpContext.RequestAborted;
        var claimsPrincipal = User as ClaimsPrincipal;
        var userId = claimsPrincipal?.FindFirstValue(ClaimTypes.NameIdentifier);

        // Get unread system notifications count
        var unreadNotificationsCount = 0;
            if (!string.IsNullOrWhiteSpace(userId))
            {
                var notificationsCountResult = await _mediator.Send(
                    new GetUnreadNotificationsCountQuery(userId, "Admin"),
                    cancellationToken);

                if (notificationsCountResult.IsSuccess)
                {
                    unreadNotificationsCount = notificationsCountResult.Value;
                }
            }

        // Get pending product custom requests count
        var customRequestsResult = await _mediator.Send(
            new GetProductCustomRequestsQuery(1, 1, CustomRequestStatus.Pending, null),
            cancellationToken);

        var customRequestsCount = 0;
        Guid? firstCustomRequestId = null;

        if (customRequestsResult.IsSuccess && customRequestsResult.Value is not null)
        {
            customRequestsCount = customRequestsResult.Value.TotalCount;
            var firstRequest = customRequestsResult.Value.Requests.FirstOrDefault();
            if (firstRequest is not null)
            {
                firstCustomRequestId = firstRequest.Id;
            }
        }

        // Get pending withdrawal requests count
        var withdrawalRequestsResult = await _mediator.Send(
            new GetWithdrawalRequestsQuery(null, null, null, WithdrawalRequestStatus.Pending, 1, 1),
            cancellationToken);

        var withdrawalRequestsCount = 0;
        Guid? firstWithdrawalRequestId = null;

        if (withdrawalRequestsResult.IsSuccess && withdrawalRequestsResult.Value is not null)
        {
            withdrawalRequestsCount = withdrawalRequestsResult.Value.TotalCount;
            var firstRequest = withdrawalRequestsResult.Value.Items.FirstOrDefault();
            if (firstRequest is not null)
            {
                firstWithdrawalRequestId = firstRequest.Id;
            }
        }

        // Get new tickets count
        var newTicketsCount = 0;
        Guid? firstTicketId = null;
        try
        {
            var ticketsCountResult = await _mediator.Send(new GetNewTicketsCountQuery(), cancellationToken);
            if (ticketsCountResult.IsSuccess)
            {
                newTicketsCount = ticketsCountResult.Value;
            }

            // Get first new ticket ID
            if (newTicketsCount > 0)
            {
                try
                {
                    var ticketsQuery = new GetTicketsQuery(UserId: null, Status: TicketStatus.Pending, AssignedToId: null, PageNumber: 1, PageSize: 1);
                    var ticketsResult = await _mediator.Send(ticketsQuery, cancellationToken);
                    if (ticketsResult.IsSuccess && ticketsResult.Value is not null && ticketsResult.Value.Tickets.Any())
                    {
                        firstTicketId = ticketsResult.Value.Tickets.First().Id;
                    }
                }
                catch
                {
                    // Ignore errors
                }
            }
        }
        catch
        {
            // Ignore errors
        }

        // Total count includes unreadNotificationsCount (ticket notifications) + other requests
        var totalCount = unreadNotificationsCount + customRequestsCount + withdrawalRequestsCount;

        var model = new AdminNotificationsDropdownViewModel(
            totalCount,
            unreadNotificationsCount,
            customRequestsCount,
            firstCustomRequestId,
            withdrawalRequestsCount,
            firstWithdrawalRequestId,
            newTicketsCount,
            firstTicketId);

        return View(model);
    }
}

public record AdminNotificationsDropdownViewModel(
    int TotalCount,
    int UnreadNotificationsCount,
    int CustomRequestsCount,
    Guid? FirstCustomRequestId,
    int WithdrawalRequestsCount,
    Guid? FirstWithdrawalRequestId,
    int NewTicketsCount,
    Guid? FirstTicketId);

