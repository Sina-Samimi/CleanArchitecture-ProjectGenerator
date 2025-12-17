using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TestAttarClone.Application.Abstractions.Messaging;
using TestAttarClone.Application.DTOs.Contacts;
using TestAttarClone.Application.Interfaces;
using TestAttarClone.SharedKernel.BaseTypes;

namespace TestAttarClone.Application.Queries.Contacts;

public sealed record GetContactMessagesQuery(
    int PageNumber = 1,
    int PageSize = 20,
    bool UnreadOnly = false) : IQuery<ContactMessagesListDto>
{
    public sealed class Handler : IQueryHandler<GetContactMessagesQuery, ContactMessagesListDto>
    {
        private readonly IContactMessageRepository _repository;

        public Handler(IContactMessageRepository repository)
        {
            _repository = repository;
        }

        public async Task<Result<ContactMessagesListDto>> Handle(GetContactMessagesQuery request, CancellationToken cancellationToken)
        {
            var pageNumber = request.PageNumber < 1 ? 1 : request.PageNumber;
            var pageSize = request.PageSize < 1 ? 20 : request.PageSize;

            var messages = request.UnreadOnly
                ? await _repository.GetUnreadAsync(pageNumber, pageSize, cancellationToken)
                : await _repository.GetAllAsync(pageNumber, pageSize, cancellationToken);

            var totalCount = request.UnreadOnly
                ? await _repository.GetUnreadCountAsync(cancellationToken)
                : await _repository.GetCountAsync(cancellationToken);

            var messageDtos = messages.Select(m => new ContactMessageDto(
                m.Id,
                m.UserId,
                m.FullName,
                m.Email,
                m.Phone,
                m.Subject,
                m.Message,
                m.IsRead,
                m.ReadAt,
                m.ReadByUserId,
                m.AdminReply,
                m.RepliedAt,
                m.RepliedByUserId,
                m.CreateDate)).ToList();

            var dto = new ContactMessagesListDto(
                messageDtos,
                totalCount,
                pageNumber,
                pageSize);

            return Result<ContactMessagesListDto>.Success(dto);
        }
    }
}

