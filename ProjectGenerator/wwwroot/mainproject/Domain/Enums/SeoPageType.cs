namespace MobiRooz.Domain.Enums;

public enum SeoPageType
{
    Home = 1,                    // صفحه اصلی
    ProductList = 2,             // لیست محصولات (/product)
    ProductDetails = 3,           // جزییات محصول (/product/{slug})
    BlogList = 4,                // لیست وبلاگ (/blog)
    BlogPost = 5,                // پست وبلاگ (/blog/{slug})
    Page = 6,                    // صفحه داینامیک (/page/{slug})
    Contact = 7,                 // تماس با ما
    About = 8,                   // درباره ما
    Category = 9,                // دسته‌بندی محصولات (/category/{slug})
    BlogCategory = 10,           // دسته‌بندی وبلاگ
    Search = 11,                 // صفحه جستجو
    Cart = 12,                   // سبد خرید
    Checkout = 13                // تسویه حساب
}

