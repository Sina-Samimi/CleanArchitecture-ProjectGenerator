using System;

namespace LogsDtoCloneTest.Application.DTOs.Billing;

public sealed record FrontTransactionInfoDto(
    decimal Amount,
    string Phonenumber,
    string UserId,
    Guid InvoiceId);
