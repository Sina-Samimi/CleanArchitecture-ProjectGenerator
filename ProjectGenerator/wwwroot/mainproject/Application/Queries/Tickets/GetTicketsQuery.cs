using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Attar.Application.Abstractions.Messaging;
using Attar.Application.DTOs.Tickets;
using Attar.Application.Interfaces;
using Attar.Domain.Enums;
using Attar.SharedKernel.BaseTypes;

namespace Attar.Application.Queries.Tickets;

public sealed record GetTicketsQuery(
    string? UserId = null,
    TicketStatus? Status = null,
    string? AssignedToId = null,
    int PageNumber = 1,
    int PageSize = 20) : IQuery<TicketListResultDto>
{
    public sealed class Handler : IQueryHandler<GetTicketsQuery, TicketListResultDto>
    {
        private readonly ITicketRepository _ticketRepository;

        public Handler(ITicketRepository ticketRepository)
        {
            _ticketRepository = ticketRepository;
        }

        public async Task<Result<TicketListResultDto>> Handle(
            GetTicketsQuery request,
            CancellationToken cancellationToken)
        {
            var pageNumber = request.PageNumber < 1 ? 1 : request.PageNumber;
            var pageSize = request.PageSize < 1 ? 20 : request.PageSize;

            var tickets = await _ticketRepository.GetAllAsync(
                request.UserId,
                request.Status,
                request.AssignedToId,
                pageNumber,
                pageSize,
                cancellationToken);

            var totalCount = await _ticketRepository.GetCountAsync(
                request.UserId,
                request.Status,
                request.AssignedToId,
                cancellationToken);

            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            var dtos = tickets.Select(t => new TicketDto(
                t.Id,
                t.UserId,
                t.User?.UserName ?? "نامشخص",
                t.User?.FullName ?? "نامشخص",
                t.User?.PhoneNumber,
                t.Subject,
                t.Message,
                t.Department,
                t.AttachmentPath,
                t.Status,
                t.AssignedToId,
                t.AssignedTo?.FullName,
                t.CreateDate,
                t.LastReplyDate,
                t.HasUnreadReplies,
                t.Replies.Count)).ToArray();

            var result = new TicketListResultDto(
                dtos,
                totalCount,
                pageNumber,
                pageSize,
                totalPages);

            return Result<TicketListResultDto>.Success(result);
        }
    }
}
