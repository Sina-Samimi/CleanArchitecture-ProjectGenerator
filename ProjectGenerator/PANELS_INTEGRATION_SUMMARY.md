# Panel Integration Summary

## Overview
این پروژه generator حالا با پنل‌های کامل و زیبا از پروژه ArsisTest تجهیز شده است.

## تغییرات اعمال شده

### 1. فایل‌های CSS و استایل
- کپی کامل CSS از ArsisTest
- ✅ `admin.css` - استایل پنل مدیریت
- ✅ `seller.css` - استایل پنل فروشنده (تبدیل شده از teacher.css)
- ✅ `user.css` - استایل پنل کاربری
- ✅ فایل‌های CSS اضافی در پوشه‌های `admin/`, `seller/`, `user/`

### 2. فونت‌ها
- ✅ IranSans Font (تمام وزن‌ها)
- ✅ Bootstrap Icons
- فایل‌ها در `wwwroot/font/` ذخیره شده‌اند

### 3. فایل‌های JavaScript
- ✅ کپی تمام فایل‌های JS از ArsisTest
- ✅ فایل‌های مخصوص admin, seller, user
- ✅ Jalali Date Picker plugin

### 4. Layout Files
سه layout اصلی ایجاد شده:
- ✅ `_AdminLayout.cshtml` - برای پنل مدیریت
- ✅ `_SellerLayout.cshtml` - برای پنل فروشنده
- ✅ `_UserLayout.cshtml` - برای پنل کاربری

### 5. ViewComponents
ViewComponent‌های sidebar برای هر پنل:

#### AdminSidebarViewComponent
```csharp
public class AdminSidebarViewComponent : ViewComponent
{
    public IViewComponentResult Invoke(...)
    {
        // مدیریت sidebar مدیریت
    }
}
```

#### SellerSidebarViewComponent
```csharp
public class SellerSidebarViewComponent : ViewComponent
{
    public IViewComponentResult Invoke(...)
    {
        // مدیریت sidebar فروشنده
    }
}
```

#### UserSidebarViewComponent
```csharp
public class UserSidebarViewComponent : ViewComponent
{
    public IViewComponentResult Invoke(...)
    {
        // مدیریت sidebar کاربری
    }
}
```

### 6. تغییر نام Teacher به Seller
تمام موارد زیر تغییر یافته:
- ✅ CSS files: `teacher.css` → `seller.css`
- ✅ ViewComponents: `TeacherSidebar` → `SellerSidebar`
- ✅ Layout: `_TeacherLayout` → `_SellerLayout`
- ✅ Controllers: `TeacherController` → `SellerController`
- ✅ Areas: `Teacher` → `Seller`
- ✅ متن‌های فارسی: "مدرس" → "فروشنده"

## ساختار پروژه Generated

```
GeneratedProject/
├── WebSite/
│   ├── wwwroot/
│   │   ├── css/
│   │   │   ├── admin.css
│   │   │   ├── seller.css
│   │   │   ├── user.css
│   │   │   ├── admin/ (13 files)
│   │   │   ├── seller/ (3 files)
│   │   │   └── user/ (1 file)
│   │   ├── font/
│   │   │   ├── bootstrap/
│   │   │   └── iransans/ (25 files)
│   │   ├── js/
│   │   │   ├── admin/ (18 files)
│   │   │   ├── seller/
│   │   │   └── site.js
│   │   ├── icons/ (4 SVG files)
│   │   └── Plugins/
│   │       └── JalaliDatePicker/
│   ├── Areas/
│   │   ├── Admin/
│   │   │   ├── Controllers/
│   │   │   └── Views/
│   │   ├── Seller/ (اگر SellerPanel فعال باشد)
│   │   │   ├── Controllers/
│   │   │   └── Views/
│   │   └── User/
│   │       ├── Controllers/
│   │       └── Views/
│   ├── ViewComponents/
│   │   ├── AdminSidebarViewComponent.cs
│   │   ├── SellerSidebarViewComponent.cs
│   │   └── UserSidebarViewComponent.cs
│   └── Views/
│       └── Shared/
│           ├── _AdminLayout.cshtml
│           ├── _SellerLayout.cshtml
│           ├── _UserLayout.cshtml
│           └── Components/
│               ├── AdminSidebar/
│               │   └── Default.cshtml
│               ├── SellerSidebar/
│               │   └── Default.cshtml
│               └── UserSidebar/
│                   └── Default.cshtml
```

## ویژگی‌های پنل‌ها

### پنل مدیریت (Admin)
- رنگ اصلی: آبی (#6366f1)
- دسترسی کامل به تمام بخش‌ها
- مدیریت کاربران، نقش‌ها، دسترسی‌ها
- مدیریت محصولات، دسته‌بندی‌ها
- مدیریت سفارشات و فاکتورها
- مدیریت بلاگ
- تنظیمات سایت

### پنل فروشنده (Seller)
- رنگ اصلی: نارنجی (#f59e0b)
- مدیریت محصولات خود
- مشاهده آمار فروش
- مدیریت سفارشات
- ویرایش پروفایل فروشنده

### پنل کاربری (User)
- رنگ اصلی: سبز (#10b981)
- مشاهده و ویرایش پروفایل
- مدیریت کیف پول
- مشاهده محصولات خریداری شده
- مشاهده فاکتورها
- مشاهده تست‌های من

## نحوه استفاده

### 1. تولید پروژه با پنل فروشنده
```json
{
  "ProjectName": "MyShop",
  "Namespace": "MyShop",
  "Options": {
    "Features": {
      "SellerPanel": true,
      "ProductCatalog": true,
      "ShoppingCart": true
    }
  }
}
```

### 2. استفاده از Layouts در Views

#### Admin Area
```cshtml
@{
    Layout = "_AdminLayout";
    ViewData["Title"] = "عنوان صفحه";
}
```

#### Seller Area
```cshtml
@{
    Layout = "_SellerLayout";
    ViewData["Title"] = "عنوان صفحه";
}
```

#### User Area
```cshtml
@{
    Layout = "_UserLayout";
    ViewData["Title"] = "عنوان صفحه";
}
```

### 3. استفاده از ViewComponents

Layouts به صورت خودکار ViewComponent مربوطه را فراخوانی می‌کنند:

```cshtml
@await Component.InvokeAsync("AdminSidebar")
@await Component.InvokeAsync("SellerSidebar")
@await Component.InvokeAsync("UserSidebar")
```

## تنظیمات Sidebar

می‌توانید داده‌های sidebar را از Controller تنظیم کنید:

```csharp
public class MyController : Controller
{
    public IActionResult Index()
    {
        ViewData["AccountName"] = "نام کاربر";
        ViewData["AccountEmail"] = "email@example.com";
        ViewData["AccountPhone"] = "09123456789";
        ViewData["AccountAvatarUrl"] = "/images/avatar.jpg";
        ViewData["ProfileCompletionPercent"] = 75;
        ViewData["GreetingSubtitle"] = "خوش آمدید";
        
        return View();
    }
}
```

## Mobile Responsive

تمام پنل‌ها به صورت کامل responsive هستند:
- ✅ Sidebar قابل collapse در موبایل
- ✅ منوی hamburger در موبایل
- ✅ Grid های responsive
- ✅ فونت و spacing بهینه شده برای موبایل

## RTL Support

✅ تمام پنل‌ها از RTL پشتیبانی کامل می‌کنند
✅ فونت IranSans به صورت پیش‌فرض
✅ Bootstrap RTL classes

## Browser Compatibility

✅ Chrome, Firefox, Safari, Edge
✅ Mobile browsers (iOS Safari, Chrome Mobile)
✅ Fallback fonts برای مرورگرهای قدیمی

## نکات مهم

1. **فونت IranSans**: فایل‌های فونت در `wwwroot/font/iransans/` قرار دارند
2. **Bootstrap Icons**: برای نمایش آیکون‌ها ضروری است
3. **jQuery**: برای عملکرد sidebar و dropdown ها لازم است
4. **ViewData**: برای پاس دادن اطلاعات به sidebar استفاده می‌شود

## مشکلات احتمالی و راه‌حل

### Sidebar نمایش داده نمی‌شود
- مطمئن شوید ViewComponent در ViewComponents folder است
- مطمئن شوید View Component در Views/Shared/Components قرار دارد

### استایل‌ها اعمال نمی‌شوند
- مطمئن شوید فایل‌های CSS در wwwroot/css کپی شده‌اند
- Cache مرورگر را پاک کنید
- فایل _Layout را بررسی کنید که CSS ها را load می‌کند

### فونت‌ها نمایش داده نمی‌شوند
- مطمئن شوید فایل‌های woff/woff2 در wwwroot/font/iransans کپی شده‌اند
- فایل fontiran.css را بررسی کنید
- مسیر فونت‌ها را در CSS چک کنید

## تست شده در
- ✅ Windows 11
- ✅ .NET 9.0
- ✅ Bootstrap 5.3
- ✅ Modern Browsers

## نویسنده
این پنل‌ها از پروژه ArsisTest استخراج و برای استفاده عمومی adapt شده‌اند.

تاریخ: 2025-11-20

