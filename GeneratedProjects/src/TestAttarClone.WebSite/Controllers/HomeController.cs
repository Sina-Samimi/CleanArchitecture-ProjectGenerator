using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TestAttarClone.Application.Queries.Admin.AboutSettings;
using TestAttarClone.Application.Queries.Admin.SiteSettings;
using TestAttarClone.WebSite.Models;
using TestAttarClone.WebSite.Models.Blog;
using TestAttarClone.WebSite.Models.Home;
using TestAttarClone.WebSite.Models.Product;
using TestAttarClone.WebSite.Services.Blog;
using TestAttarClone.WebSite.Services.Products;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace TestAttarClone.WebSite.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IBlogService _blogService;
        private readonly IProductCatalogService _productCatalogService;
        private readonly IMediator _mediator;

        public HomeController(ILogger<HomeController> logger, IBlogService blogService, IProductCatalogService productCatalogService, IMediator mediator)
        {
            _logger = logger;
            _blogService = blogService;
            _productCatalogService = productCatalogService;
            _mediator = mediator;
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

        public async Task<IActionResult> About(CancellationToken cancellationToken)
        {
            var aboutResult = await _mediator.Send(new GetAboutSettingsQuery(), cancellationToken);
            
            if (aboutResult.IsSuccess && aboutResult.Value is not null)
            {
                var about = aboutResult.Value;
                ViewData["Title"] = string.IsNullOrWhiteSpace(about.MetaTitle) 
                    ? "درباره ما - عطاری آنلاین" 
                    : about.MetaTitle;
                ViewData["MetaDescription"] = about.MetaDescription ?? "درباره عطاری آنلاین - بیش از ۲۰ سال تجربه در زمینه فروش گیاهان دارویی";
                ViewData["AboutTitle"] = about.Title;
                ViewData["AboutDescription"] = about.Description;
                ViewData["AboutVision"] = about.Vision;
                ViewData["AboutMission"] = about.Mission;
                ViewData["AboutImagePath"] = about.ImagePath;
            }
            else
            {
                ViewData["Title"] = "درباره ما - عطاری آنلاین";
                ViewData["MetaDescription"] = "درباره عطاری آنلاین - بیش از ۲۰ سال تجربه در زمینه فروش گیاهان دارویی";
            }
            
            return View();
        }

        public async Task<IActionResult> Contact(CancellationToken cancellationToken)
        {
            var siteSettingsResult = await _mediator.Send(new GetSiteSettingsQuery(), cancellationToken);
            
            if (siteSettingsResult.IsSuccess && siteSettingsResult.Value is not null)
            {
                var settings = siteSettingsResult.Value;
                ViewData["Title"] = "تماس با ما - عطاری آنلاین";
                ViewData["MetaDescription"] = settings.ContactDescription ?? "تماس با عطاری آنلاین - اطلاعات تماس و فرم ارتباط با ما";
                ViewData["ContactAddress"] = settings.Address;
                ViewData["ContactPhone"] = settings.ContactPhone;
                ViewData["SupportPhone"] = settings.SupportPhone;
                ViewData["SiteEmail"] = settings.SiteEmail;
                ViewData["SupportEmail"] = settings.SupportEmail;
                ViewData["ContactDescription"] = settings.ContactDescription;
            }
            else
            {
                ViewData["Title"] = "تماس با ما - عطاری آنلاین";
                ViewData["MetaDescription"] = "تماس با عطاری آنلاین - اطلاعات تماس و فرم ارتباط با ما";
            }
            
            return View();
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
