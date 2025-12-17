using System;

namespace TestAttarClone.Application.DTOs.Billing;

public sealed record FrontTransactionInfoDto(
    decimal Amount,
    string Phonenumber,
    string UserId,
    Guid InvoiceId);
