using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TestAttarClone.WebSite.Models.Product;

namespace TestAttarClone.WebSite.Services.Products;

public sealed class InMemoryProductCatalogService : IProductCatalogService
{
    private readonly IReadOnlyList<Product> _products;

    public InMemoryProductCatalogService()
    {
        _products = CreateProducts();
    }

    public Task<IReadOnlyList<Product>> GetFeaturedProductsAsync(int count, CancellationToken cancellationToken = default)
    {
        var take = Math.Max(1, count);
        var featured = _products
            .Where(product => product.IsFeatured)
            .OrderByDescending(product => product.PublishedAt)
            .ThenByDescending(product => product.Rating)
            .Take(take)
            .ToList();

        if (featured.Count < take)
        {
            featured.AddRange(_products
                .Except(featured)
                .OrderByDescending(product => product.PublishedAt)
                .Take(take - featured.Count));
        }

        return Task.FromResult<IReadOnlyList<Product>>(featured);
    }

    public Task<ProductListResult> GetProductsAsync(ProductFilterOptions filterOptions, CancellationToken cancellationToken = default)
    {
        var query = _products.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(filterOptions.SearchTerm))
        {
            var search = filterOptions.SearchTerm.Trim();
            query = query.Where(product =>
                product.Name.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                product.ShortDescription.Contains(search, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(filterOptions.Category))
        {
            query = query.Where(product => string.Equals(product.Category, filterOptions.Category, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(filterOptions.DeliveryFormat))
        {
            query = query.Where(product => string.Equals(product.DeliveryFormat, filterOptions.DeliveryFormat, StringComparison.OrdinalIgnoreCase));
        }

        if (filterOptions.MinPrice.HasValue)
        {
            query = query.Where(product => product.Price >= filterOptions.MinPrice.Value);
        }

        if (filterOptions.MaxPrice.HasValue)
        {
            query = query.Where(product => product.Price <= filterOptions.MaxPrice.Value);
        }

        if (filterOptions.MinRating.HasValue)
        {
            query = query.Where(product => product.Rating >= filterOptions.MinRating.Value);
        }

        query = filterOptions.SortBy switch
        {
            "price-asc" => query.OrderBy(product => product.Price),
            "price-desc" => query.OrderByDescending(product => product.Price),
            "rating" => query.OrderByDescending(product => product.Rating).ThenByDescending(product => product.ReviewCount),
            _ => query.OrderByDescending(product => product.PublishedAt)
        };

        var filteredProducts = query.ToList();

        var categories = _products
            .Select(product => product.Category)
            .Where(category => !string.IsNullOrWhiteSpace(category))
            .Select(category => category!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(category => category, StringComparer.Create(CultureInfo.GetCultureInfo("fa-IR"), ignoreCase: true))
            .ToList();

        var deliveryFormats = _products
            .Select(product => product.DeliveryFormat)
            .Where(format => !string.IsNullOrWhiteSpace(format))
            .Select(format => format!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(format => format, StringComparer.Create(CultureInfo.GetCultureInfo("fa-IR"), ignoreCase: true))
            .ToList();

        var validPrices = _products.Where(p => p.Price.HasValue).Select(p => p.Price!.Value).ToList();
        var priceRangeMin = validPrices.Count > 0 ? validPrices.Min() : 0m;
        var priceRangeMax = validPrices.Count > 0 ? validPrices.Max() : 0m;

        var result = new ProductListResult(filteredProducts, filteredProducts.Count, categories, deliveryFormats, priceRangeMin, priceRangeMax);
        return Task.FromResult(result);
    }

    public Task<Product?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(slug))
        {
            return Task.FromResult<Product?>(null);
        }

        var normalized = slug.Trim();
        var product = _products.FirstOrDefault(item => string.Equals(item.Slug, normalized, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(product);
    }

    public Task<IReadOnlyList<Product>> GetRelatedProductsAsync(Guid productId, int count, CancellationToken cancellationToken = default)
    {
        var current = _products.FirstOrDefault(product => product.Id == productId);
        if (current is null)
        {
            return Task.FromResult<IReadOnlyList<Product>>(Array.Empty<Product>());
        }

        var take = Math.Max(1, count);
        var related = _products
            .Where(product => product.Id != productId && string.Equals(product.Category, current.Category, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(product => product.Rating)
            .ThenByDescending(product => product.PublishedAt)
            .Take(take)
            .ToList();

        if (related.Count < take)
        {
            related.AddRange(_products
                .Where(product => product.Id != productId && !related.Contains(product))
                .OrderByDescending(product => product.PublishedAt)
                .Take(take - related.Count));
        }

        return Task.FromResult<IReadOnlyList<Product>>(related);
    }

    public Task<bool> AddCommentAsync(
        Guid productId,
        string authorName,
        string content,
        double rating,
        Guid? parentId = null,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(false);
    }

    private static IReadOnlyList<Product> CreateProducts()
    {
        return new List<Product>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Slug = "talent-discovery-suite",
                Name = "پکیج جامع کشف استعداد",
                ShortDescription = "ارزیابی ۳۶۰ درجه برای شناسایی استعدادهای پنهان سازمانی.",
                Description = "این پکیج با استفاده از آزمون‌های روان‌سنجی عمیق و تحلیل داده‌های رفتاری، به شما کمک می‌کند تا استعدادهای کلیدی سازمان را شناسایی و مسیر توسعه آن‌ها را طراحی کنید.",
                HeroImageUrl = "https://images.unsplash.com/photo-1521737604893-d14cc237f11d?auto=format&fit=crop&w=1600&q=80",
                ThumbnailUrl = "https://images.unsplash.com/photo-1521737604893-d14cc237f11d?auto=format&fit=crop&w=800&q=80",
                Price = 1450000,
                OriginalPrice = 1890000,
                Rating = 4.9,
                ReviewCount = 124,
                DifficultyLevel = "سازمانی",
                Category = "تحلیل استعداد",
                DeliveryFormat = "آنلاین",
                Duration = "۶ هفته",
                PublishedAt = DateTimeOffset.UtcNow.AddDays(-40),
                Tags = new[] { "ارزیابی ۳۶۰ درجه", "نقشه راه رشد" },
                Highlights = new[]
                {
                    "شناسایی ۵ استعداد غالب هر عضو",
                    "طراحی نقشه رشد اختصاصی",
                    "گزارش مدیریتی قابل دانلود"
                },
                Modules = new List<ProductModule>
                {
                    new() { Title = "آزمون شخصیت و انگیزش", Description = "سنجش ۲۴ شاخص رفتاری با تحلیل تطبیقی.", Duration = "۹۰ دقیقه" },
                    new() { Title = "مصاحبه عمقی آنلاین", Description = "بررسی انگیزه‌های پنهان و مدل ذهنی.", Duration = "۴۵ دقیقه" },
                    new() { Title = "طراحی نقشه رشد", Description = "خروجی عملیاتی برای واحد منابع انسانی.", Duration = "هفته ۵" }
                },
                Statistics = new List<ProductStatistic>
                {
                    new() { Label = "میزان رضایت", Value = "۹۴٪", Tooltip = "براساس نظرسنجی از ۸۴ سازمان" },
                    new() { Label = "زمان استقرار", Value = "۱۴ روز" },
                    new() { Label = "تعداد گزارش", Value = "۳۲ گزارش" }
                },
                FaqItems = new List<ProductFaqItem>
                {
                    new() { Question = "آیا نیاز به حضور فیزیکی هست؟", Answer = "تمامی مراحل ارزیابی و تحویل گزارش به صورت آنلاین انجام می‌شود." },
                    new() { Question = "آیا امکان شخصی‌سازی وجود دارد؟", Answer = "بله، متناسب با مدل شایستگی سازمان شما سفارشی‌سازی می‌شود." }
                },
                Comments = new List<ProductComment>
                {
                    new()
                    {
                        Id = Guid.NewGuid(),
                        AuthorName = "رویا رستگار",
                        Content = "گزارش‌ها بسیار دقیق و قابل اقدام بودند و تیم ما توانست برنامه توسعه فردی را سریع‌تر طراحی کند.",
                        CreatedAt = DateTimeOffset.UtcNow.AddDays(-12),
                        Rating = 5
                    },
                    new()
                    {
                        Id = Guid.NewGuid(),
                        AuthorName = "سامان هاشمی",
                        Content = "فرآیند ارزیابی آنلاین و پشتیبانی تیم آرسیس عالی بود. پیشنهاد می‌کنم برای سازمان‌های در حال رشد.",
                        CreatedAt = DateTimeOffset.UtcNow.AddDays(-26),
                        Rating = 4.8
                    }
                },
                IsFeatured = true
            },
            new()
            {
                Id = Guid.NewGuid(),
                Slug = "leadership-lab",
                Name = "لابراتوار توسعه رهبران",
                ShortDescription = "برنامه فشرده برای توسعه مهارت‌های رهبری در مدیران میانی.",
                Description = "برنامه لابراتوار توسعه رهبران ترکیبی از کارگاه‌های تعاملی، کوچینگ فردی و تحلیل رفتاری است که مهارت‌های کلیدی رهبری را در مدیران تقویت می‌کند.",
                HeroImageUrl = "https://images.unsplash.com/photo-1552664730-d307ca884978?auto=format&fit=crop&w=1600&q=80",
                ThumbnailUrl = "https://images.unsplash.com/photo-1552664730-d307ca884978?auto=format&fit=crop&w=800&q=80",
                Price = 2350000,
                OriginalPrice = 2590000,
                Rating = 4.7,
                ReviewCount = 86,
                DifficultyLevel = "مدیران میانی",
                Category = "توسعه رهبری",
                DeliveryFormat = "ترکیبی",
                Duration = "۸ هفته",
                PublishedAt = DateTimeOffset.UtcNow.AddDays(-65),
                Tags = new[] { "کوچینگ", "کارگاه تعاملی" },
                Highlights = new[]
                {
                    "جلسات کوچینگ فردی با منتور اختصاصی",
                    "ارزیابی ۳۶۰ درجه رفتار رهبری",
                    "طراحی پروژه عملیاتی"
                },
                Modules = new List<ProductModule>
                {
                    new() { Title = "کارگاه حضوری رهبری", Description = "تمرین‌های تیمی و سناریوهای واقعی.", Duration = "۲ روز" },
                    new() { Title = "کوچینگ فردی", Description = "۴ جلسه آنلاین با منتور خبره.", Duration = "۴۵ دقیقه برای هر جلسه" },
                    new() { Title = "پروژه عملی", Description = "اجرای پروژه تحول در تیم سازمانی.", Duration = "۴ هفته" }
                },
                Statistics = new List<ProductStatistic>
                {
                    new() { Label = "نرخ تکمیل", Value = "۸۷٪" },
                    new() { Label = "رضایت شرکت‌کنندگان", Value = "۹۱٪" },
                    new() { Label = "تعداد منتورها", Value = "۱۴ منتور" }
                },
                FaqItems = new List<ProductFaqItem>
                {
                    new() { Question = "آیا جلسات حضوری الزام است؟", Answer = "بله، در ابتدای برنامه یک بوت‌کمپ حضوری دو روزه برگزار می‌شود." },
                    new() { Question = "حداکثر تعداد شرکت‌کننده؟", Answer = "هر گروه بین ۱۲ تا ۱۵ مدیر را شامل می‌شود." }
                },
                Comments = new List<ProductComment>
                {
                    new()
                    {
                        Id = Guid.NewGuid(),
                        AuthorName = "بهناز موسوی",
                        Content = "تجربه کوچینگ فردی فوق‌العاده بود و باعث شد نقاط کور خودم را بهتر ببینم.",
                        CreatedAt = DateTimeOffset.UtcNow.AddDays(-35),
                        Rating = 4.7
                    },
                    new()
                    {
                        Id = Guid.NewGuid(),
                        AuthorName = "ماهان زندی",
                        Content = "برنامه فشرده ولی بسیار کاربردی. پروژه عملی ما را مجبور کرد مهارت‌ها را به کار بگیریم.",
                        CreatedAt = DateTimeOffset.UtcNow.AddDays(-50),
                        Rating = 4.6
                    }
                },
                IsFeatured = true
            },
            new()
            {
                Id = Guid.NewGuid(),
                Slug = "agile-talent-sprint",
                Name = "اسپرینت چابک استعداد",
                ShortDescription = "طراحی ساختار تیم‌های چابک با تمرکز بر چیدمان استعداد.",
                Description = "اسپرینت چابک استعداد رویکردی سریع برای بازطراحی تیم‌ها بر اساس مدل استعداد و نقش‌های چابک ارائه می‌دهد و در سه هفته قابل اجرا است.",
                HeroImageUrl = "https://images.unsplash.com/photo-1521737604893-d14cc237f11d?auto=format&fit=crop&w=1600&q=80",
                ThumbnailUrl = "https://images.unsplash.com/photo-1498050108023-c5249f4df085?auto=format&fit=crop&w=800&q=80",
                Price = 980000,
                Rating = 4.5,
                ReviewCount = 64,
                DifficultyLevel = "تیم‌های محصول",
                Category = "چابک‌سازی",
                DeliveryFormat = "آنلاین",
                Duration = "۳ هفته",
                PublishedAt = DateTimeOffset.UtcNow.AddDays(-22),
                Tags = new[] { "چابکی سازمانی", "طراحی تیم" },
                Highlights = new[]
                {
                    "طراحی نقش‌های چابک مبتنی بر استعداد",
                    "نقشه استقرار تیمی",
                    "منتورینگ هفتگی"
                },
                Modules = new List<ProductModule>
                {
                    new() { Title = "تحلیل استعداد تیم", Description = "تحلیل ماتریس استعداد برای کل تیم.", Duration = "هفته ۱" },
                    new() { Title = "طراحی نقش‌های چابک", Description = "تعریف نقش‌ها و مسئولیت‌ها.", Duration = "هفته ۲" },
                    new() { Title = "منتورینگ اجرایی", Description = "جلسات هفتگی با اسکرام‌مستر ارشد.", Duration = "۳ جلسه" }
                },
                Statistics = new List<ProductStatistic>
                {
                    new() { Label = "میانگین زمان تحویل", Value = "۳ هفته" },
                    new() { Label = "تیم‌های بهره‌بردار", Value = "۲۸ تیم" }
                },
                FaqItems = new List<ProductFaqItem>
                {
                    new() { Question = "آیا برای تیم‌های توزیع‌شده مناسب است؟", Answer = "بله، تمامی جلسات به شکل آنلاین و منعطف برگزار می‌شود." },
                    new() { Question = "آیا نیاز به ابزار خاصی داریم؟", Answer = "دسترسی به ابزارهای همکاری آنلاین مانند Miro و Jira پیشنهاد می‌شود." }
                },
                Comments = new List<ProductComment>
                {
                    new()
                    {
                        Id = Guid.NewGuid(),
                        AuthorName = "هلیا برزگر",
                        Content = "در پایان سه هفته ساختار تیم ما به طور کامل بازطراحی شد و خروجی ملموسی گرفتیم.",
                        CreatedAt = DateTimeOffset.UtcNow.AddDays(-8),
                        Rating = 4.6
                    }
                },
                IsFeatured = true
            },
            new()
            {
                Id = Guid.NewGuid(),
                Slug = "talent-analytics-dashboard",
                Name = "داشبورد تحلیلی استعداد",
                ShortDescription = "داشبورد آنلاین برای پایش لحظه‌ای شاخص‌های استعداد سازمان.",
                Description = "داشبورد تحلیلی استعداد، داده‌های آزمون‌ها، ارزیابی‌ها و عملکرد را یکپارچه می‌کند تا مدیران بتوانند تصمیم‌های مبتنی بر داده بگیرند.",
                HeroImageUrl = "https://images.unsplash.com/photo-1460925895917-afdab827c52f?auto=format&fit=crop&w=1600&q=80",
                ThumbnailUrl = "https://images.unsplash.com/photo-1460925895917-afdab827c52f?auto=format&fit=crop&w=800&q=80",
                Price = 1820000,
                Rating = 4.4,
                ReviewCount = 41,
                DifficultyLevel = "سازمانی",
                Category = "تحلیل داده",
                DeliveryFormat = "آنلاین",
                Duration = "۴ هفته",
                PublishedAt = DateTimeOffset.UtcNow.AddDays(-15),
                Tags = new[] { "هوش تجاری", "BI" },
                Highlights = new[]
                {
                    "اتصال به منابع داده سازمانی",
                    "گزارش‌های سفارشی",
                    "هشدارهای هوشمند"
                },
                Modules = new List<ProductModule>
                {
                    new() { Title = "گردآوری داده", Description = "یکپارچه‌سازی منابع داده HR و آزمون.", Duration = "هفته ۱" },
                    new() { Title = "طراحی داشبورد", Description = "ساخت داشبوردهای مدیریتی و تیمی.", Duration = "هفته ۲" },
                    new() { Title = "آموزش و استقرار", Description = "آموزش کاربری و پایش نتایج.", Duration = "۲ جلسه" }
                },
                Statistics = new List<ProductStatistic>
                {
                    new() { Label = "زمان استقرار", Value = "۴ هفته" },
                    new() { Label = "گزارش آماده", Value = "۱۸ گزارش" },
                    new() { Label = "شاخص‌های کلیدی", Value = "۲۴ KPI" }
                },
                FaqItems = new List<ProductFaqItem>
                {
                    new() { Question = "آیا داده‌ها محرمانه می‌مانند؟", Answer = "بله، تمامی تبادلات با پروتکل‌های امن و رمزنگاری انجام می‌شود." },
                    new() { Question = "آیا با Power BI سازگار است؟", Answer = "بله، خروجی‌ها در قالب Power BI و Tableau قابل ارائه است." }
                },
                Comments = new List<ProductComment>
                {
                    new()
                    {
                        Id = Guid.NewGuid(),
                        AuthorName = "مهرداد صالحی",
                        Content = "ما توانستیم شاخص‌های استعداد را وارد گزارش‌های مدیریت ارشد کنیم و تصمیم‌های دقیق‌تری بگیریم.",
                        CreatedAt = DateTimeOffset.UtcNow.AddDays(-5),
                        Rating = 4.5
                    }
                },
                IsFeatured = false
            }
        };
    }
}
