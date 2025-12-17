using System;
using System.Threading;
using System.Threading.Tasks;
using LogsDtoCloneTest.Application.Abstractions.Messaging;
using LogsDtoCloneTest.Application.Interfaces;
using LogsDtoCloneTest.SharedKernel.BaseTypes;

namespace LogsDtoCloneTest.Application.Commands.Contacts;

public sealed record MarkContactMessageAsReadCommand(
    Guid MessageId,
    string ReadByUserId) : ICommand
{
    public sealed class Handler : ICommandHandler<MarkContactMessageAsReadCommand>
    {
        private readonly IContactMessageRepository _repository;

        public Handler(IContactMessageRepository repository)
        {
            _repository = repository;
        }

        public async Task<Result> Handle(MarkContactMessageAsReadCommand request, CancellationToken cancellationToken)
        {
            if (request.MessageId == Guid.Empty)
            {
                return Result.Failure("شناسه پیام معتبر نیست.");
            }

            if (string.IsNullOrWhiteSpace(request.ReadByUserId))
            {
                return Result.Failure("شناسه کاربر الزامی است.");
            }

            var message = await _repository.GetByIdAsync(request.MessageId, cancellationToken);
            if (message is null || message.IsDeleted)
            {
                return Result.Failure("پیام مورد نظر یافت نشد.");
            }

            message.MarkAsRead(request.ReadByUserId);
            await _repository.UpdateAsync(message, cancellationToken);

            return Result.Success();
        }
    }
}

