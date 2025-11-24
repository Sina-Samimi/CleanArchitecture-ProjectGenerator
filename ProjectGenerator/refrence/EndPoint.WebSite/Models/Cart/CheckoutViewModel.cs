namespace EndPoint.WebSite.Models.Cart;

public sealed class CheckoutViewModel
{
    public required CartViewModel Cart { get; set; }
    public string SelectedPaymentMethod { get; set; } = "Gateway";
}
