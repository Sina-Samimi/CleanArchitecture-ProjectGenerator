using System.Threading;
using System.Threading.Tasks;
using LogTableRenameTest.Application.Abstractions.Messaging;
using LogTableRenameTest.Application.Interfaces;
using LogTableRenameTest.SharedKernel.BaseTypes;

namespace LogTableRenameTest.Application.Queries.Tickets;

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
