using System;
using System.Threading;
using System.Threading.Tasks;
using Attar.Application.Abstractions.Messaging;
using Attar.Application.Interfaces;
using Attar.SharedKernel.BaseTypes;

namespace Attar.Application.Commands.Tickets;

public sealed record MarkTicketAsReadCommand(Guid TicketId) : ICommand
{
    public sealed class Handler : ICommandHandler<MarkTicketAsReadCommand>
    {
        private readonly ITicketRepository _ticketRepository;

        public Handler(ITicketRepository ticketRepository)
        {
            _ticketRepository = ticketRepository;
        }

        public async Task<Result> Handle(MarkTicketAsReadCommand request, CancellationToken cancellationToken)
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

            ticket.MarkAsRead();
            await _ticketRepository.UpdateAsync(ticket, cancellationToken);

            return Result.Success();
        }
    }
}
