using System.Threading;
using System.Threading.Tasks;
using LogTableRenameTest.Application.Abstractions.Messaging;
using LogTableRenameTest.Application.Interfaces;
using LogTableRenameTest.SharedKernel.BaseTypes;

namespace LogTableRenameTest.Application.Queries.Tickets;

public sealed record GetUnreadTicketsCountForUserQuery(string UserId) : IQuery<int>
{
    public sealed class Handler : IQueryHandler<GetUnreadTicketsCountForUserQuery, int>
    {
        private readonly ITicketRepository _ticketRepository;

        public Handler(ITicketRepository ticketRepository)
        {
            _ticketRepository = ticketRepository;
        }

        public async Task<Result<int>> Handle(
            GetUnreadTicketsCountForUserQuery request,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.UserId))
            {
                return Result<int>.Failure("شناسه کاربر الزامی است.");
            }

            var count = await _ticketRepository.GetUnreadRepliesCountForUserAsync(request.UserId, cancellationToken);
            return Result<int>.Success(count);
        }
    }
}
