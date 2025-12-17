using System.Collections.Generic;

namespace TestAttarClone.WebSite.Models.Product;

public class WishlistViewModel
{
    public IReadOnlyList<ProductSummaryViewModel> Products { get; init; } = System.Array.Empty<ProductSummaryViewModel>();
}

