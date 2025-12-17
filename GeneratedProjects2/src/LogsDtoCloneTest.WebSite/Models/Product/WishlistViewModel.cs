using System.Collections.Generic;

namespace LogsDtoCloneTest.WebSite.Models.Product;

public class WishlistViewModel
{
    public IReadOnlyList<ProductSummaryViewModel> Products { get; init; } = System.Array.Empty<ProductSummaryViewModel>();
}

