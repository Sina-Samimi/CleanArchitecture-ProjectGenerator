using System;
using System.Threading;
using System.Threading.Tasks;
using MobiRooz.Application.Abstractions.Messaging;
using MobiRooz.Application.Interfaces;
using MobiRooz.Domain.Enums;
using MobiRooz.SharedKernel.BaseTypes;

namespace MobiRooz.Application.Commands.Tickets;

public sealed record UpdateTicketStatusCommand(
    Guid TicketId,
    TicketStatus Status,
    string? AssignedToId = null) : ICommand
{
    public sealed class Handler : ICommandHandler<UpdateTicketStatusCommand>
    {
        private readonly ITicketRepository _ticketRepository;

        public Handler(ITicketRepository ticketRepository)
        {
            _ticketRepository = ticketRepository;
        }

        public async Task<Result> Handle(UpdateTicketStatusCommand request, CancellationToken cancellationToken)
        {
            if (request.TicketId == Guid.Empty)
            {
                return Result.Failure("شناسه تیکت الزامی است.");
            }

            var ticket = await _ticketRepository.GetByIdAsync(request.TicketId, cancellationToken);
            if (ticket is null)
            {
                return Result.Failure("تیکت یافت نشد.");
            }

            ticket.UpdateStatus(request.Status);

            if (!string.IsNullOrWhiteSpace(request.AssignedToId))
            {
                ticket.AssignTo(request.AssignedToId);
            }

            await _ticketRepository.UpdateAsync(ticket, cancellationToken);

            return Result.Success();
        }
    }
}
