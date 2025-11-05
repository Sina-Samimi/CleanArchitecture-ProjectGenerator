# 🚀 راهنمای اجرا - Project Generator

## ⚡ اجرای سریع

### روش 1: استفاده از Windows Forms UI (پیشنهادی - فقط Windows)

```bash
cd ProjectGenerator.UI
dotnet run
```

یا اگر از Visual Studio استفاده می‌کنید:
1. فایل `ProjectGenerator.sln` را باز کنید
2. `ProjectGenerator.UI` را به عنوان Startup Project انتخاب کنید
3. F5 را بزنید

### روش 2: استفاده از Console Application

```bash
cd ProjectGenerator
dotnet run
```

یا با پارامترها:

```bash
cd ProjectGenerator
dotnet run -- -n MyShop -o C:\Projects --seed-data
```

### روش 3: استفاده از فایل Config

```bash
cd ProjectGenerator
dotnet run -- --config example-full-config.json
```

## 📋 پیش‌نیازها

- .NET 9.0 SDK
- Windows (برای Windows Forms UI)
- Visual Studio 2022 یا VS Code (اختیاری)

## 🔧 رفع خطاهای احتمالی

### خطای "dotnet command not found"
نصب .NET SDK از: https://dotnet.microsoft.com/download

### خطای "partial modifier"
✅ رفع شد! تمام فایل‌های TemplateProvider حالا partial هستند.

### خطای Build
```bash
# پاک کردن و Build مجدد
dotnet clean
dotnet restore
dotnet build
```

## 📁 ساختار Solution

```
/workspace/
├── ProjectGenerator.sln        ← Solution اصلی
├── ProjectGenerator/           ← Console Application
│   ├── Program.cs
│   ├── Models/
│   ├── Generators/
│   └── Templates/
└── ProjectGenerator.UI/        ← Windows Forms UI
    ├── Program.cs
    ├── MainForm.cs
    ├── RolesConfigForm.cs
    └── UsersConfigForm.cs
```

## 🎯 مثال استفاده

### با Windows Forms:
1. اجرای `ProjectGenerator.UI`
2. پر کردن فرم:
   - نام پروژه: `MyShop`
   - مسیر خروجی: `C:\Projects`
   - انتخاب تمام امکانات
3. تنظیم نقش‌ها و کاربران
4. کلیک "تولید پروژه"

### با Console:
```bash
cd ProjectGenerator
dotnet run -- -n MyAwesomeShop -o C:\Projects --seed-data
```

پاسخ به سوالات:
- شامل WebSite؟ Y
- شامل Tests؟ Y
- تولید seed data؟ Y
- نقش‌های پیش‌فرض؟ Y
- کاربر Admin؟ Y

## ✅ تست

پس از تولید پروژه:

```bash
cd C:\Projects\MyShop\MyShop.WebSite
dotnet restore
dotnet build
dotnet ef migrations add InitialCreate --project ../src/Infrastructure
dotnet ef database update
dotnet run
```

## 📚 مستندات بیشتر

- `ProjectGenerator/README.md` - راهنمای کامل
- `ProjectGenerator/QUICKSTART_FA.md` - راهنمای 5 دقیقه‌ای
- `ProjectGenerator/FEATURES_SUMMARY.md` - لیست امکانات
- `GENERATOR_COMPLETE_SUMMARY.md` - خلاصه کامل

## 🐛 مشکلات رایج

### پروژه build نمی‌شود
```bash
dotnet clean
dotnet restore
dotnet build
```

### خطای "The type or namespace name does not exist"
مطمئن شوید که تمام فایل‌ها ذخیره شده‌اند و:
```bash
dotnet restore
```

### Windows Forms اجرا نمی‌شود (Linux/Mac)
از روش Console استفاده کنید:
```bash
cd ProjectGenerator
dotnet run
```

## 💡 نکات

1. برای Windows حتما از **ProjectGenerator.UI** استفاده کنید (راحت‌تر است)
2. برای Linux/Mac از **ProjectGenerator** (Console) استفاده کنید
3. فایل `example-full-config.json` یک نمونه کامل تنظیمات است
4. می‌توانید تنظیمات خود را ذخیره و دوباره استفاده کنید

## 📞 کمک

اگر مشکلی داشتید:
1. مستندات را بخوانید
2. Issue در GitHub ایجاد کنید
3. خطا را کامل کپی کنید

---

**موفق باشید! 🚀**
