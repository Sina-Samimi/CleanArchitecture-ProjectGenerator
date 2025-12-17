using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TestAttarClone.Application.Abstractions.Messaging;
using TestAttarClone.Application.DTOs.Tickets;
using TestAttarClone.Application.Interfaces;
using TestAttarClone.SharedKernel.BaseTypes;

namespace TestAttarClone.Application.Queries.Tickets;

public sealed record GetTicketByIdQuery(Guid TicketId) : IQuery<TicketDetailDto>
{
    public sealed class Handler : IQueryHandler<GetTicketByIdQuery, TicketDetailDto>
    {
        private readonly ITicketRepository _ticketRepository;

        public Handler(ITicketRepository ticketRepository)
        {
            _ticketRepository = ticketRepository;
        }

        public async Task<Result<TicketDetailDto>> Handle(
            GetTicketByIdQuery request,
            CancellationToken cancellationToken)
        {
            if (request.TicketId == Guid.Empty)
            {
                return Result<TicketDetailDto>.Failure("شناسه تیکت الزامی است.");
            }

            var ticket = await _ticketRepository.GetByIdWithRepliesAsync(request.TicketId, cancellationToken);
            if (ticket is null)
            {
                return Result<TicketDetailDto>.Failure("تیکت یافت نشد.");
            }

            var replies = ticket.Replies
                .OrderBy(r => r.CreateDate)
                .Select(r => new TicketReplyDto(
                    r.Id,
                    r.TicketId,
                    r.Message,
                    r.IsFromAdmin,
                    r.RepliedById,
                    r.RepliedBy?.FullName,
                    r.CreateDate))
                .ToArray();

            var dto = new TicketDetailDto(
                ticket.Id,
                ticket.UserId,
                ticket.User?.UserName ?? "نامشخص",
                ticket.User?.FullName ?? "نامشخص",
                ticket.User?.PhoneNumber,
                ticket.Subject,
                ticket.Message,
                ticket.Department,
                ticket.AttachmentPath,
                ticket.Status,
                ticket.AssignedToId,
                ticket.AssignedTo?.FullName,
                ticket.CreateDate,
                ticket.LastReplyDate,
                ticket.HasUnreadReplies,
                replies);

            return Result<TicketDetailDto>.Success(dto);
        }
    }
}
