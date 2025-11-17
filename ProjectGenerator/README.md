# 🚀 Clean Architecture Project Generator - نسخه پیشرفته

یک تولید کننده پروژه قدرتمند با معماری تمیز (Clean Architecture) که به صورت کامل شامل تمام امکانات یک سیستم فروشگاهی حرفه‌ای می‌باشد.

## ✨ امکانات

### امکانات اصلی پروژه تولید شده

- ✅ **مدیریت کاربران**: سیستم کامل احراز هویت و مجوزدهی با ASP.NET Core Identity
- ✅ **پنل فروشنده**: پنل اختصاصی برای فروشندگان جهت مدیریت محصولات و سفارشات
- ✅ **کاتالوگ محصولات**: مدیریت کامل محصولات، دسته‌بندی‌ها و تصاویر
- ✅ **سبد خرید**: سیستم سبد خرید پیشرفته
- ✅ **صدور فاکتور**: سیستم فاکتورنویسی و مدیریت پرداخت‌ها
- ✅ **سیستم بلاگ**: بلاگ کامل با نظرات و دسته‌بندی

### معماری پروژه

```
Solution/
├── src/
│   ├── Domain/              # لایه Domain - Entities, Enums, ValueObjects
│   ├── SharedKernel/        # لایه مشترک - Interfaces, Results, Guards
│   ├── Application/         # لایه Application - Services, DTOs, Interfaces
│   ├── Infrastructure/      # لایه Infrastructure - DbContext, Repositories
│   └── ProjectName.WebSite/ # لایه Presentation با تمام Areas
│       ├── Areas/
│       │   ├── Admin/      # پنل مدیریت
│       │   ├── Seller/     # پنل فروشنده
│       │   └── User/       # پنل کاربری
│       ├── Controllers/    # کنترلرهای اصلی
│       ├── Views/          # ویوهای Razor
│       └── wwwroot/        # فایل‌های استاتیک
└── tests/
    └── UnitTests/          # تست‌های واحد
```

## 🎯 راه‌های استفاده

### 1. استفاده از Windows Forms Application (پیشنهادی)

راحت‌ترین روش برای تولید پروژه:

```bash
cd ProjectGenerator.UI
dotnet run
```

در رابط گرافیکی:
1. نام پروژه را وارد کنید
2. مسیر خروجی را انتخاب کنید
3. امکانات مورد نظر را انتخاب کنید
4. نقش‌ها و کاربران اولیه را تنظیم کنید
5. دکمه "تولید پروژه" را بزنید

### 2. استفاده از Command Line

#### حالت تعاملی (Interactive)
```bash
cd ProjectGenerator
dotnet run
```

#### استفاده از پارامترها
```bash
dotnet run -- -n MyShop -o C:\Projects --seed-data
```

پارامترهای موجود:
- `-n, --name`: نام پروژه
- `-o, --output`: مسیر خروجی
- `--namespace`: فضای نام (پیش‌فرض: نام پروژه)
- `--no-web`: عدم تولید لایه WebSite
- `--no-tests`: عدم تولید پروژه Test
- `--seed-data`: تولید داده‌های اولیه

### 3. استفاده از فایل JSON

```bash
dotnet run -- --config my-project-config.json
```

نمونه فایل تنظیمات:

```json
{
  "ProjectName": "MyAwesomeShop",
  "OutputPath": "C:\\Projects\\MyAwesomeShop",
  "Namespace": "MyCompany.MyAwesomeShop",
  "Options": {
    "IncludeWebSite": true,
    "IncludeTests": true,
    "GenerateInitialSeedData": true,
    "Features": {
      "UserManagement": true,
      "SellerPanel": true,
      "ProductCatalog": true,
      "ShoppingCart": true,
      "Invoicing": true,
      "BlogSystem": true
    },
    "SeedRoles": [
      {
        "Name": "Admin",
        "Description": "مدیر سیستم",
        "Permissions": ["ManageUsers", "ManageProducts", "ManageOrders"]
      },
      {
        "Name": "Seller",
        "Description": "فروشنده",
        "Permissions": ["ManageOwnProducts", "ViewOrders"]
      },
      {
        "Name": "User",
        "Description": "کاربر عادی",
        "Permissions": ["ViewProducts", "PlaceOrders"]
      }
    ],
    "SeedUsers": [
      {
        "Username": "admin",
        "Email": "admin@example.com",
        "PhoneNumber": "09123456789",
        "Password": "Admin@123",
        "Roles": ["Admin"]
      }
    ]
  }
}
```

## 📦 نصب و راه‌اندازی

### پیش‌نیازها

- .NET 9.0 SDK یا بالاتر
- SQL Server (برای دیتابیس)
- Visual Studio 2022 یا VS Code (اختیاری)

### مراحل نصب

1. کلون کردن مخزن:
```bash
git clone <repository-url>
cd ProjectGenerator
```

2. Build کردن پروژه:
```bash
dotnet build
```

3. اجرای Windows Forms Application:
```bash
cd ProjectGenerator.UI
dotnet run
```

## 🏗️ ساختار پروژه تولید شده

### لایه‌های پروژه

#### 1. Domain Layer
- **Entities**: موجودیت‌های اصلی (Product, Order, Blog, Cart, etc.)
- **Enums**: شمارش‌ها (OrderStatus, InvoiceStatus, BlogStatus, etc.)
- **ValueObjects**: اشیاء ارزشی
- **Events**: رویدادهای Domain

#### 2. SharedKernel Layer
- **Interfaces**: رابط‌های مشترک (IRepository)
- **Results**: الگوی Result برای مدیریت خطاها
- **Guards**: محافظ‌ها برای اعتبارسنجی

#### 3. Application Layer
- **Interfaces**: رابط‌های سرویس‌ها
- **Services**: پیاده‌سازی منطق کسب‌وکار
- **DTOs**: اشیاء انتقال داده
- **Mapping**: نگاشت بین Entity و DTO

#### 4. Infrastructure Layer
- **Data**: DbContext و پیکربندی EF Core
- **Repositories**: پیاده‌سازی Repository ها
- **Services**: سرویس‌های زیرساختی
- **Identity**: پیکربندی ASP.NET Core Identity

#### 5. WebSite Layer (Presentation)

##### Areas

**Admin Area** (پنل مدیریت):
- مدیریت کاربران و نقش‌ها
- مدیریت محصولات و دسته‌بندی‌ها
- مدیریت سفارشات
- مدیریت بلاگ
- داشبورد و گزارش‌گیری

**Seller Area** (پنل فروشنده):
- مدیریت محصولات خود
- مشاهده و مدیریت سفارشات
- داشبورد فروشنده

**User Area** (پنل کاربری):
- مدیریت پروفایل
- مشاهده سفارشات
- پیگیری فاکتورها

##### Controllers اصلی
- **HomeController**: صفحه اصلی
- **AccountController**: ورود، خروج و ثبت‌نام
- **ProductController**: نمایش محصولات
- **CartController**: سبد خرید
- **CheckoutController**: تسویه حساب
- **BlogController**: نمایش بلاگ

## 🔐 احراز هویت و مجوزدهی

پروژه تولید شده شامل:

- ASP.NET Core Identity با پیکربندی کامل
- نقش‌های پیش‌فرض (Admin, Seller, User)
- کاربران اولیه قابل تنظیم
- Authorization Policies
- سیستم مجوزها (Permissions)

## 🗄️ دیتابیس

### ساختار دیتابیس

پروژه تولید شده شامل Entity های زیر است:

**کاربران و احراز هویت:**
- Users
- Roles
- UserRoles
- UserClaims

**محصولات:**
- Products
- Categories
- ProductImages

**سفارشات:**
- Orders
- OrderItems
- Invoices

**سبد خرید:**
- Carts
- CartItems

**بلاگ:**
- Blogs
- BlogComments
- BlogCategories

### Migration اولیه

پس از تولید پروژه:

```bash
cd src/YourProjectName.WebSite
dotnet ef migrations add InitialCreate --project ../Infrastructure
dotnet ef database update
```

## 🎨 رابط کاربری

- طراحی Responsive با Bootstrap 5
- پشتیبانی کامل از RTL برای فارسی
- Layout های جداگانه برای هر Area
- UI/UX مدرن و کاربرپسند

## 📋 مثال‌های استفاده

### ایجاد یک فروشگاه کامل

```bash
cd ProjectGenerator.UI
dotnet run
```

در فرم:
1. نام پروژه: `MyShop`
2. مسیر: `C:\Projects`
3. تمام امکانات را فعال کنید
4. دکمه "تنظیم نقش‌ها" → نقش‌های Admin, Seller, User را اضافه کنید
5. دکمه "تنظیم کاربران" → کاربر admin اضافه کنید
6. "تولید پروژه"

### ایجاد پروژه ساده‌تر (فقط بلاگ)

```json
{
  "ProjectName": "MyBlog",
  "OutputPath": "C:\\Projects\\MyBlog",
  "Options": {
    "Features": {
      "UserManagement": true,
      "BlogSystem": true,
      "SellerPanel": false,
      "ProductCatalog": false,
      "ShoppingCart": false,
      "Invoicing": false
    }
  }
}
```

```bash
dotnet run -- --config blog-config.json
```

## 🚀 پس از تولید پروژه

1. باز کردن Solution در Visual Studio
2. Restore کردن پکیج‌ها:
   ```bash
   dotnet restore
   ```

3. Build کردن پروژه:
   ```bash
   dotnet build
   ```

4. اجرای Migration:
   ```bash
   cd src/YourProject.WebSite
   dotnet ef migrations add InitialCreate --project ../Infrastructure
   dotnet ef database update
   ```

5. اجرای پروژه:
   ```bash
   dotnet run
   ```

6. مراجعه به آدرس: `https://localhost:5001`

## 🛠️ سفارشی‌سازی

### افزودن Entity جدید

1. Entity را در `Domain/Entities` اضافه کنید
2. DbSet را در `ApplicationDbContext` اضافه کنید
3. Configuration را در `OnModelCreating` اضافه کنید
4. Migration جدید ایجاد کنید

### افزودن Service جدید

1. Interface در `Application/Interfaces` تعریف کنید
2. Implementation در `Infrastructure/Services` پیاده‌سازی کنید
3. در `DependencyInjection` ثبت کنید

## 📝 نکات مهم

- پروژه تولید شده آماده استفاده در محیط Production است
- تمام Best Practice های Clean Architecture رعایت شده
- کد تولید شده قابل توسعه و نگهداری است
- از الگوهای طراحی استاندارد استفاده شده

## 🤝 مشارکت

برای مشارکت در توسعه این پروژه:
1. Fork کنید
2. Branch جدید ایجاد کنید
3. تغییرات را Commit کنید
4. Pull Request ارسال کنید

## 📄 لایسنس

این پروژه تحت لایسنس MIT منتشر شده است.

## 💬 پشتیبانی

برای سوالات و مشکلات:
- Issue در GitHub ایجاد کنید
- یا با ما تماس بگیرید

---

**ساخته شده با ❤️ برای توسعه‌دهندگان .NET**
