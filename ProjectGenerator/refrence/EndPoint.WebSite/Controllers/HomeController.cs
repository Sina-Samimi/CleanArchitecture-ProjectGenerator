using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EndPoint.WebSite.Models;
using EndPoint.WebSite.Models.Blog;
using EndPoint.WebSite.Models.Home;
using EndPoint.WebSite.Models.Product;
using EndPoint.WebSite.Services.Blog;
using EndPoint.WebSite.Services.Products;
using Microsoft.AspNetCore.Mvc;

namespace EndPoint.WebSite.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IBlogService _blogService;
        private readonly IProductCatalogService _productCatalogService;

        public HomeController(ILogger<HomeController> logger, IBlogService blogService, IProductCatalogService productCatalogService)
        {
            _logger = logger;
            _blogService = blogService;
            _productCatalogService = productCatalogService;
        }

        public async Task<IActionResult> Index(CancellationToken cancellationToken)
        {
            var posts = await _blogService.GetLatestPostsAsync(4, cancellationToken);

            var latestPosts = posts
                .Select(post => new BlogPostSummaryViewModel
                {
                    Slug = post.Slug,
                    Title = post.Title,
                    Summary = post.Summary,
                    HeroImageUrl = post.HeroImageUrl,
                    PublishedAt = post.PublishedAt,
                    ReadingTimeMinutes = post.ReadingTimeMinutes,
                    AuthorName = post.AuthorName,
                    AuthorRole = post.AuthorRole,
                    Tags = post.Tags
                })
                .ToList();

            var featuredProducts = await _productCatalogService.GetFeaturedProductsAsync(6, cancellationToken);

            var productSummaries = featuredProducts
                .Select(MapToProductSummary)
                .ToList();

            var viewModel = new HomeViewModel
            {
                LatestPosts = latestPosts,
                FeaturedProducts = productSummaries
            };

            return View(viewModel);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        private static ProductSummaryViewModel MapToProductSummary(Models.Product.Product product)
        {
            return new ProductSummaryViewModel
            {
                Id = product.Id,
                Slug = product.Slug,
                Name = product.Name,
                ShortDescription = product.ShortDescription,
                Price = product.Price,
                OriginalPrice = product.OriginalPrice,
                ThumbnailUrl = product.ThumbnailUrl,
                Category = product.Category,
                DeliveryFormat = product.DeliveryFormat,
                Rating = product.Rating,
                ReviewCount = product.ReviewCount,
                Badge = product.Duration,
                Tags = product.Tags
            };
        }
    }
}
