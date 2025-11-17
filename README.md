# 🎯 Clean Architecture Project Generator

<div align="center">

![.NET](https://img.shields.io/badge/.NET-9.0-purple?style=for-the-badge&logo=dotnet)
![License](https://img.shields.io/badge/License-MIT-green.svg?style=for-the-badge)
![Platform](https://img.shields.io/badge/Platform-Windows%20%7C%20Linux%20%7C%20macOS-lightgrey?style=for-the-badge)
![Language](https://img.shields.io/badge/Language-%D9%81%D8%A7%D8%B1%D8%B3%DB%8C-blue?style=for-the-badge)

**تولید پروژه‌های حرفه‌ای با Clean Architecture در چند دقیقه**

[English](README.EN.md) | [فارسی](README.md)

</div>

---

## 🌟 نمای کلی

یک **ابزار قدرتمند** برای تولید خودکار پروژه‌های کامل با **Clean Architecture**. 

بدون نیاز به کدنویسی دستی، یک پروژه فروشگاهی کامل با تمام امکانات در چند دقیقه تولید کنید!

### ✨ چرا این ابزار؟

- 🚀 **سریع** - تولید پروژه در کمتر از 1 دقیقه
- 🏗️ **استاندارد** - Clean Architecture با تمام Best Practices
- 🎨 **کامل** - شامل User Management, Shopping Cart, Blog و ...
- 📱 **چند پلتفرم** - Windows Forms UI + Console Application
- 🇮🇷 **فارسی** - پشتیبانی کامل از RTL و مستندات فارسی
- 🔧 **سفارشی** - انتخاب فیچرهای مورد نیاز
- 📦 **Production Ready** - آماده استفاده در پروژه واقعی

---

## 🎥 نمایش سریع

```bash
# نصب
git clone https://github.com/YOUR_USERNAME/CleanArchitecture-ProjectGenerator.git

# اجرا (Windows)
cd CleanArchitecture-ProjectGenerator
RUN_WINFORMS.bat

# اجرا (Linux/Mac)
cd CleanArchitecture-ProjectGenerator
./build-and-run.sh
```

**نتیجه:** یک پروژه کامل در `MyShop/` با:
- ✅ 15+ Entity
- ✅ 3 Area (Admin, Seller, User)
- ✅ Authentication & Authorization
- ✅ Shopping Cart, Blog, Invoicing
- ✅ Clean Architecture

---

## 📸 تصاویر

### Windows Forms Interface
```
[قرار دادن تصویر]
```
*رابط کاربری گرافیکی راحت با پشتیبانی کامل فارسی*

### Generated Project Structure
```
MyShop/
├── src/
│   ├── Domain/              # 15+ Entities
│   ├── Application/         # Services & DTOs
│   ├── Infrastructure/      # EF Core & Repositories
│   ├── SharedKernel/        # Common Interfaces
│   └── MyShop.WebSite/      # ASP.NET Core MVC
│       ├── Areas/
│       │   ├── Admin/      # Admin Panel
│       │   ├── Seller/     # Seller Panel
│       │   └── User/       # User Panel
│       ├── Controllers/
│       └── Views/
└── tests/
    └── UnitTests/
```

### Admin Panel
```
[قرار دادن تصویر]
```
*پنل مدیریت کامل با CRUD برای تمام Entities*

---

## ✨ امکانات

### 🖥️ دو نسخه اجرایی

#### Windows Forms UI
- رابط گرافیکی کاربرپسند
- تنظیمات بصری
- مدیریت نقش‌ها و کاربران
- ذخیره/بارگذاری Config
- پشتیبانی کامل RTL

#### Console Application
- برای تمام پلتفرم‌ها
- حالت تعاملی
- پارامترهای Command Line
- فایل JSON Config

### 🏗️ معماری تولید شده

**4 لایه Clean Architecture:**

1. **Domain Layer**
   - BaseEntity با Audit Fields
   - 15+ Entity (User, Product, Order, Blog, ...)
   - Value Objects & Enums
   - Domain Events (آماده)

2. **Application Layer**
   - 6+ Service Interfaces
   - DTOs با Validation
   - MediatR Ready (CQRS)
   - FluentValidation Ready

3. **Infrastructure Layer**
   - EF Core 9.0
   - Generic Repository Pattern
   - Identity با Custom User/Role
   - Seed Data Support

4. **WebSite Layer (Presentation)**
   - ASP.NET Core MVC
   - 3 Complete Areas
   - Bootstrap 5 RTL
   - Authentication & Authorization

### 🛍️ فیچرهای فروشگاهی

| فیچر | توضیح | وضعیت |
|------|-------|------|
| 👥 **User Management** | مدیریت کاربران و نقش‌ها | ✅ |
| 🏪 **Seller Panel** | پنل اختصاصی فروشندگان | ✅ |
| 📦 **Product Catalog** | محصولات + دسته‌بندی + تصاویر | ✅ |
| 🛒 **Shopping Cart** | سبد خرید با Session/Database | ✅ |
| 💰 **Invoicing** | صدور فاکتور + پیگیری پرداخت | ✅ |
| 📝 **Blog System** | بلاگ + نظرات + دسته‌بندی | ✅ |

### 📊 آمار پروژه تولید شده

```
📁 Domain Entities:         15+
🔧 Service Interfaces:      6
🎨 Controllers:             15+
📄 Views:                   10+
🏢 Areas:                   3 (Admin, Seller, User)
🔐 Authentication:          ASP.NET Core Identity
💾 Database:                EF Core 9.0
📝 Lines of Code:           5000+
```

---

## 🚀 شروع سریع

### پیش‌نیازها

- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- SQL Server (LocalDB کافی است)
- Windows OS (برای Windows Forms - اختیاری)

### نصب

```bash
git clone https://github.com/YOUR_USERNAME/CleanArchitecture-ProjectGenerator.git
cd CleanArchitecture-ProjectGenerator
```

### اجرا

#### 🪟 Windows (با رابط گرافیکی)
```cmd
RUN_WINFORMS.bat
```

#### 🐧 Linux / 🍎 Mac (Console)
```bash
chmod +x build-and-run.sh
./build-and-run.sh
```

#### ⚙️ از سورس
```bash
# Build
dotnet restore ProjectGenerator.sln
dotnet build ProjectGenerator.sln

# Run
cd ProjectGenerator.UI      # Windows Forms
# یا
cd ProjectGenerator         # Console
dotnet run
```

---

## 📖 استفاده

### گام 1: تنظیمات پروژه

```
نام پروژه:     MyAwesomeShop
مسیر خروجی:    C:\Projects
Namespace:     MyCompany.MyAwesomeShop
```

### گام 2: انتخاب فیچرها

- ✅ مدیریت کاربران
- ✅ پنل فروشنده
- ✅ کاتالوگ محصولات
- ✅ سبد خرید
- ✅ صدور فاکتور
- ✅ سیستم بلاگ

### گام 3: تنظیم نقش‌ها

```
Admin:  مدیر سیستم
Seller: فروشنده
User:   کاربر عادی
```

### گام 4: کاربران اولیه

```
Username: admin
Email:    admin@example.com  
Password: Admin@123
Role:     Admin
```

### گام 5: تولید

کلیک **"تولید پروژه"** و صبر کنید!

### گام 6: اجرای پروژه تولید شده

```bash
cd C:\Projects\MyAwesomeShop
dotnet restore

# ایجاد دیتابیس
cd src/MyAwesomeShop.WebSite
dotnet ef migrations add InitialCreate --project ../Infrastructure
dotnet ef database update

# اجرا
dotnet run

# مرور
# https://localhost:5001
```

### گام 7: ورود

```
نام کاربری: admin
رمز عبور:   Admin@123
```

---

## 📚 مستندات

| فایل | توضیح |
|------|-------|
| [📖 Quick Start](QUICK_START.md) | شروع در 5 دقیقه |
| [🔧 How to Run](HOW_TO_RUN.md) | راهنمای اجرا |
| [✨ Features](ProjectGenerator/FEATURES_SUMMARY.md) | لیست کامل امکانات |
| [🐛 Troubleshooting](WINFORMS_FIX.md) | رفع مشکلات |
| [📝 Changelog](CHANGELOG.md) | تاریخچه تغییرات |

---

## 🏗️ ساختار Repository

```
CleanArchitecture-ProjectGenerator/
├── ProjectGenerator/              # Console Application
│   ├── Models/                   # Config Models
│   ├── Generators/               # Code Generators
│   │   ├── SolutionGenerator.cs
│   │   ├── LayerGenerator.cs
│   │   └── WebSiteGenerator.cs
│   └── Templates/                # Code Templates
│       ├── DomainEntityTemplates.cs
│       ├── ApplicationLayerTemplates.cs
│       └── WebSiteTemplates.cs
│
├── ProjectGenerator.UI/           # Windows Forms UI
│   ├── MainForm.cs
│   ├── RolesConfigForm.cs
│   ├── RoleEditForm.cs
│   ├── UsersConfigForm.cs
│   └── UserEditForm.cs
│
├── ProjectGenerator.sln           # Solution File
├── RUN_WINFORMS.bat              # Quick Run (Windows)
├── build-and-run.sh              # Quick Run (Linux/Mac)
└── README.md                      # This file
```

---

## 🔧 توسعه

### Build از سورس

```bash
# Clone
git clone https://github.com/YOUR_USERNAME/CleanArchitecture-ProjectGenerator.git
cd CleanArchitecture-ProjectGenerator

# Restore & Build
dotnet restore
dotnet build

# Run
cd ProjectGenerator.UI
dotnet run
```

### افزودن Template جدید

1. فایل در `ProjectGenerator/Templates/` ایجاد کنید
2. کلاس را `partial class TemplateProvider` کنید
3. متد template را اضافه کنید
4. در Generator مربوطه استفاده کنید

مثال:
```csharp
// MyNewTemplate.cs
public partial class TemplateProvider
{
    public string GetMyNewTemplate()
    {
        return $@"
// Your template code here
";
    }
}
```

---

## 🤝 مشارکت

مشارکت‌ها خوشامد است! 

### چگونه مشارکت کنیم؟

1. Fork کنید
2. Branch جدید: `git checkout -b feature/AmazingFeature`
3. Commit: `git commit -m 'Add AmazingFeature'`
4. Push: `git push origin feature/AmazingFeature`
5. Pull Request باز کنید

### راهنمای Contribution

لطفاً [CONTRIBUTING.md](CONTRIBUTING.md) را بخوانید.

---

## 🐛 گزارش مشکل

مشکلی پیدا کردید? در [Issues](../../issues) گزارش دهید.

**قالب Issue:**
- توضیح مشکل
- مراحل بازتولید
- رفتار مورد انتظار
- Screenshots
- محیط (OS, .NET Version)

---

## 💡 سوالات متداول

### آیا می‌توانم فیچرها را انتخابی تولید کنم?
بله! در Windows Forms UI یا Console می‌توانید فیچرهای مورد نیاز را انتخاب کنید.

### آیا می‌توانم پایگاه داده غیر از SQL Server استفاده کنم?
در نسخه فعلی فقط SQL Server پشتیبانی می‌شود. PostgreSQL و MySQL در نسخه‌های آینده اضافه می‌شوند.

### آیا پروژه تولید شده قابل ویرایش است?
بله! کد تولید شده کاملاً قابل ویرایش و توسعه است.

### آیا می‌توانم برای پروژه‌های تجاری استفاده کنم?
بله! لایسنس MIT اجازه استفاده تجاری می‌دهد.

---

## 📝 لایسنس

این پروژه تحت [MIT License](LICENSE) منتشر شده است.

---

## 🙏 تشکر

این پروژه با استفاده از:
- [ASP.NET Core](https://github.com/dotnet/aspnetcore)
- [Entity Framework Core](https://github.com/dotnet/efcore)
- [Clean Architecture](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html)

---

## 📞 ارتباط

- 🐙 GitHub: [@YOUR_USERNAME](https://github.com/YOUR_USERNAME)
- 📧 Email: your.email@example.com
- 🌐 Website: https://yourwebsite.com
- 💼 LinkedIn: [Your Name](https://linkedin.com/in/yourprofile)

---

## 🌟 حمایت

اگر این پروژه برایتان مفید بود:

- ⭐ **Star** دهید
- 🍴 **Fork** کنید
- 🐛 **Issue** گزارش دهید
- 💡 **Feature** پیشنهاد دهید
- 📢 **Share** کنید

---

## 📈 Roadmap

### نسخه 1.1.0
- [ ] PostgreSQL Support
- [ ] MySQL Support
- [ ] Docker Compose
- [ ] Swagger/OpenAPI

### نسخه 1.2.0
- [ ] Blazor WebAssembly Option
- [ ] API-Only Mode
- [ ] GraphQL Support

### نسخه 2.0.0
- [ ] Microservices Template
- [ ] CQRS with MediatR
- [ ] Event Sourcing
- [ ] Redis Caching

---

<div align="center">

**ساخته شده با ❤️ برای جامعه توسعه‌دهندگان .NET**

[⬆ بازگشت به بالا](#-clean-architecture-project-generator)

---

[![GitHub stars](https://img.shields.io/github/stars/YOUR_USERNAME/CleanArchitecture-ProjectGenerator?style=social)](https://github.com/YOUR_USERNAME/CleanArchitecture-ProjectGenerator/stargazers)
[![GitHub forks](https://img.shields.io/github/forks/YOUR_USERNAME/CleanArchitecture-ProjectGenerator?style=social)](https://github.com/YOUR_USERNAME/CleanArchitecture-ProjectGenerator/network/members)
[![GitHub watchers](https://img.shields.io/github/watchers/YOUR_USERNAME/CleanArchitecture-ProjectGenerator?style=social)](https://github.com/YOUR_USERNAME/CleanArchitecture-ProjectGenerator/watchers)

</div>
