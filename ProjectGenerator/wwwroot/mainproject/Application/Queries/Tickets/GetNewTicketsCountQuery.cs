using System.Threading;
using System.Threading.Tasks;
using MobiRooz.Application.Abstractions.Messaging;
using MobiRooz.Application.Interfaces;
using MobiRooz.SharedKernel.BaseTypes;

namespace MobiRooz.Application.Queries.Tickets;

public sealed record GetNewTicketsCountQuery() : IQuery<int>
{
    public sealed class Handler : IQueryHandler<GetNewTicketsCountQuery, int>
    {
        private readonly ITicketRepository _ticketRepository;

        public Handler(ITicketRepository ticketRepository)
        {
            _ticketRepository = ticketRepository;
        }

        public async Task<Result<int>> Handle(
            GetNewTicketsCountQuery request,
            CancellationToken cancellationToken)
        {
            var count = await _ticketRepository.GetNewTicketsCountAsync(cancellationToken);
            return Result<int>.Success(count);
        }
    }
}
