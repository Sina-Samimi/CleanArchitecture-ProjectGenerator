using System;
using System.Threading;
using System.Threading.Tasks;
using LogsDtoCloneTest.Application.Abstractions.Messaging;
using LogsDtoCloneTest.Application.Interfaces;
using LogsDtoCloneTest.Domain.Entities.Contacts;
using LogsDtoCloneTest.SharedKernel.BaseTypes;

namespace LogsDtoCloneTest.Application.Commands.Contacts;

public sealed record CreateContactMessageCommand(
    string FullName,
    string Email,
    string Phone,
    string Subject,
    string Message,
    string? UserId = null) : ICommand<Guid>
{
    public sealed class Handler : ICommandHandler<CreateContactMessageCommand, Guid>
    {
        private readonly IContactMessageRepository _repository;
        private readonly IAuditContext _auditContext;

        public Handler(
            IContactMessageRepository repository,
            IAuditContext auditContext)
        {
            _repository = repository;
            _auditContext = auditContext;
        }

        public async Task<Result<Guid>> Handle(CreateContactMessageCommand request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.FullName))
            {
                return Result<Guid>.Failure("نام و نام خانوادگی الزامی است.");
            }

            if (request.FullName.Length > 200)
            {
                return Result<Guid>.Failure("نام و نام خانوادگی نمی‌تواند بیشتر از ۲۰۰ کاراکتر باشد.");
            }

            if (string.IsNullOrWhiteSpace(request.Email))
            {
                return Result<Guid>.Failure("ایمیل الزامی است.");
            }

            if (request.Email.Length > 256)
            {
                return Result<Guid>.Failure("ایمیل نمی‌تواند بیشتر از ۲۵۶ کاراکتر باشد.");
            }

            if (string.IsNullOrWhiteSpace(request.Phone))
            {
                return Result<Guid>.Failure("شماره تماس الزامی است.");
            }

            if (request.Phone.Length > 50)
            {
                return Result<Guid>.Failure("شماره تماس نمی‌تواند بیشتر از ۵۰ کاراکتر باشد.");
            }

            if (string.IsNullOrWhiteSpace(request.Subject))
            {
                return Result<Guid>.Failure("موضوع الزامی است.");
            }

            if (request.Subject.Length > 500)
            {
                return Result<Guid>.Failure("موضوع نمی‌تواند بیشتر از ۵۰۰ کاراکتر باشد.");
            }

            if (string.IsNullOrWhiteSpace(request.Message))
            {
                return Result<Guid>.Failure("پیام الزامی است.");
            }

            var contactMessage = new ContactMessage(
                request.FullName,
                request.Email,
                request.Phone,
                request.Subject,
                request.Message,
                request.UserId);

            var audit = _auditContext.Capture();
            contactMessage.CreatorId = audit.UserId;
            contactMessage.Ip = audit.IpAddress;
            contactMessage.CreateDate = audit.Timestamp;
            contactMessage.UpdateDate = audit.Timestamp;
            contactMessage.IsDeleted = false;

            await _repository.AddAsync(contactMessage, cancellationToken);

            return Result<Guid>.Success(contactMessage.Id);
        }
    }
}

