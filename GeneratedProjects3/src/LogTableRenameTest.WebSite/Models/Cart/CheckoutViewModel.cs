using System;
using System.Collections.Generic;
using LogTableRenameTest.Application.DTOs.UserAddresses;

namespace LogTableRenameTest.WebSite.Models.Cart;

public sealed class CheckoutViewModel
{
    public required CartViewModel Cart { get; set; }
    public string SelectedPaymentMethod { get; set; } = "Gateway";
    public IReadOnlyCollection<UserAddressDto>? Addresses { get; set; }
    public Guid? SelectedAddressId { get; set; }
}
