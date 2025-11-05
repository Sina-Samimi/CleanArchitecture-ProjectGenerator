# شروع سریع - 5 دقیقه

## گام 1: نصب .NET SDK (اگر نصب نیست)

```bash
# Linux
bash ../scripts/install-dotnet9.sh

# Windows - از سایت رسمی دانلود کنید
# https://dotnet.microsoft.com/download/dotnet/9.0
```

## گام 2: اجرای ProjectGenerator

```bash
cd ProjectGenerator
dotnet run
```

## گام 3: پاسخ به سوالات

```
Enter project name: MyFirstProject
Enter output path: [Enter برای همین پوشه]
Enter namespace: [Enter برای پیش‌فرض]
Include WebSite project? (Y/n): Y
Include Tests project? (Y/n): Y
Generate seed data (users/roles)? (y/N): y
Add default roles (Admin, Teacher, User)? (Y/n): Y
Create default admin user? (Y/n): Y
Admin email: admin@myproject.com
Admin password: [Enter برای Admin@123]
```

## گام 4: بررسی نتیجه

```bash
cd MyFirstProject
ls -la
```

باید ساختار زیر را ببینید:
```
MyFirstProject/
├── MyFirstProject.sln
├── src/
│   ├── Domain/
│   ├── SharedKernel/
│   ├── Application/
│   └── Infrastructure/
├── MyFirstProject.WebSite/
└── tests/
    └── UnitTests/
```

## گام 5: Build و اجرا

```bash
# Build
dotnet build

# اجرا (برای WebSite)
cd MyFirstProject.WebSite
dotnet run
```

## 🎉 تمام شد!

حالا یک پروژه کامل Clean Architecture دارید که می‌توانید شروع به توسعه کنید.

---

## مثال‌های سریع دیگر

### پروژه بدون Web (فقط API یا Library)
```bash
dotnet run -- -n MyLibrary --no-web
```

### پروژه کامل با seed data
```bash
dotnet run -- -n MyELearning --seed-data
```

### استفاده از Config File
```bash
dotnet run -- --config example-config.json
```

---

## چند نکته سریع

✅ همه فایل‌ها با .NET 9 سازگار هستند
✅ Template ها آماده استفاده هستند
✅ فقط نیاز است Connection String را در `appsettings.json` تنظیم کنید
✅ برای Migration: `dotnet ef migrations add Initial`

**Happy Coding! 🚀**
