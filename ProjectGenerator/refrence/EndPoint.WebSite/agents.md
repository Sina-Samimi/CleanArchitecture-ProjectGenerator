# AGENT.md

## 🎯 هدف این فایل
این سند برای توسعه‌دهندگانی نوشته شده که به عنوان Agent روی این پروژه کار می‌کنند. هدف آن، تعریف دقیق نقش‌ها، مسئولیت‌ها، وابستگی‌ها، و نحوه تعامل با معماری پروژه است تا توسعه منسجم، قابل نگهداری، و قابل توسعه باقی بماند.

---

## 🧠 موضوع پروژه
پلتفرم تست روانشناسی چندکاربره با .NET 9، که بر اساس یک تست اختصاصی، ۲۴ استعداد انسانی را تحلیل و رتبه‌بندی می‌کند. کاربران تست را انجام می‌دهند و در پایان، گزارشی قابل دانلود شامل استعدادهای برترشان دریافت می‌کنند.

---

## 🏗️ معماری پروژه
پروژه با معماری Clean Architecture + DDD + Vertical Slice طراحی شده و آماده‌ی توسعه به Microservices در آینده است.

### لایه‌ها:
- `Presentation`: Razor Pages برای کاربران، MVC Area برای Admin
- `Application`: UseCaseها با MediatR، DTOها، و سرویس‌های واسط
- `Domain`: مدل دامنه غنی با Entity، ValueObject، DomainException
- `Infrastructure`: EF Core، سرویس‌های گزارش‌گیری، ذخیره‌سازی فایل
- `SharedKernel`: کلاس‌های پایه، Extensions، Constants
- `Tests`: تست‌های واحد، یکپارچه، و UI

---

## ⚠️ پیش‌نیاز حیاتی برای اجرای تست‌ها

برای اجرای تست‌ها، بیلد پروژه، و استفاده از ابزارهای CLI مثل `dotnet test`، **نصب SDK کامل .NET 9.0** روی سیستم توسعه‌دهنده الزامی است.

> در محیط‌هایی مثل Codex که دسترسی به `dotnet CLI` ندارند، اجرای تست‌ها ممکن نیست مگر اینکه روی سیستم خودتان SDK نصب باشد.

برای بررسی نسخه نصب‌شده، از دستور زیر استفاده کنید:

dotnet --version
همچنین لایه تست فعلا کد نویسی نشود

---

### ✅ عمومی و معماری
- `Microsoft.NETCore.App` (نسخه 9.0)
- `Microsoft.Extensions.DependencyInjection`
- `Microsoft.Extensions.Logging`
- `Microsoft.AspNetCore.Mvc.Razor.RuntimeCompilation`

### ✅ MediatR و CQRS
- `MediatR`
- `MediatR.Extensions.Microsoft.DependencyInjection`

### ✅ اعتبارسنجی
- `FluentValidation`
- `FluentValidation.DependencyInjectionExtensions`

### ✅ Entity Framework Core
- `Microsoft.EntityFrameworkCore`
- `Microsoft.EntityFrameworkCore.SqlServer`
- `Microsoft.EntityFrameworkCore.Tools`
- `Microsoft.EntityFrameworkCore.Design`

### ✅ تست و Mock
- `xUnit`
- `Moq`
- `FluentAssertions`

### ✅ تولید گزارش و فایل
- `QuestPDF` یا `PdfSharpCore` (برای خروجی PDF)
- `ClosedXML` یا `EPPlus` (در صورت نیاز به خروجی Excel)

### ✅ ابزارهای کمکی
- `AutoMapper`
- `Newtonsoft.Json` یا `System.Text.Json`
- `Serilog` (در صورت نیاز به لاگ حرفه‌ای)

---

## 🧩 وظایف Agent

### 1. توسعه لایه‌های غیر از Presentation
- تکمیل مدل‌های دامنه و منطق داخلی
- پیاده‌سازی UseCaseها در Application Layer
- ساخت سرویس‌های زیرساختی در Infrastructure
- نوشتن تست‌های واحد و یکپارچه

### 2. رعایت اصول معماری
- استفاده از MediatR برای decoupling
- رعایت DDD: منطق درون دامنه، Aggregate Root، ValueObject
- استفاده از FluentValidation برای ورودی‌ها
- استفاده از Dependency Injection برای همه‌ی سرویس‌ها

### 3. همکاری با مالک پروژه
- Pull Requestها فقط به برنچ `develop` ارسال شوند
- مرج به `main` فقط توسط مالک پروژه انجام می‌شود
- نام‌گذاری برنچ‌ها باید استاندارد باشد (`feature/`, `bugfix/`, `hotfix/`)
- مستندسازی هر فیچر یا تغییر در صورت نیاز

---

## 🔐 قوانین GitHub

- برنچ `main` محافظت‌شده است (Protected Branch)
- هیچ‌کس اجازه‌ی push یا merge مستقیم به `main` ندارد
- تمام تغییرات باید از طریق Pull Request به `develop` انجام شوند
- در صورت وجود CI، status checks باید پاس شوند

---

## 📦 منابع تکمیلی

- [README.md](./README.md): معرفی پروژه و نحوه اجرا
- [CONTRIBUTING.md](./CONTRIBUTING.md): قوانین همکاری و توسعه
- [branching-strategy.md](./branching-strategy.md): سیاست مدیریت برنچ‌ها

---

## 🤝 ارتباط و هماهنگی

در صورت نیاز به هماهنگی، سوال، یا پیشنهاد، لطفاً از طریق Issue یا پیام مستقیم با مالک پروژه (Sina) در ارتباط باشید.
