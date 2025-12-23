namespace MobiRooz.Application.DTOs.Sms;

public sealed record VerifyPhonenumberSmsDto(
    string PhoneNumber,
    string Code);

public sealed record AnswerLinkAlertSmsDto(
    string PhoneNumber,
    string FirstName,
    string LastName,
    string Link);

public sealed record TicketReplySmsDto(
    string PhoneNumber,
    string FirstName,
    string LastName,
    string TicketTitle);

public sealed record WithdrawalResponseSmsDto(
    string PhoneNumber,
    string FirstName,
    string LastName,
    string StatusText);

public sealed record ProductFollowUpStatusSmsDto(
    string PhoneNumber,
    string FirstName,
    string LastName,
    string ProductName,
    string StatusText);

public sealed record SellerProductRequestReplySmsDto(
    string PhoneNumber,
    string FirstName,
    string LastName,
    string ProductName,
    string StatusText);

public sealed record BackInStockSmsDto(
    string PhoneNumber,
    string ProductName,
    string? SellerName);