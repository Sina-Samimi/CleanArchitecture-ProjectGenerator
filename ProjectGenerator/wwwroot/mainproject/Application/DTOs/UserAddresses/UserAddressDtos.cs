using System;

namespace Attar.Application.DTOs.UserAddresses;

public sealed record UserAddressDto(
    Guid Id,
    string Title,
    string RecipientName,
    string RecipientPhone,
    string Province,
    string City,
    string PostalCode,
    string AddressLine,
    string? Plaque,
    string? Unit,
    bool IsDefault);
