using System.Collections.Generic;

namespace LogTableRenameTest.WebSite.Models.Product;

public class WishlistViewModel
{
    public IReadOnlyList<ProductSummaryViewModel> Products { get; init; } = System.Array.Empty<ProductSummaryViewModel>();
}

