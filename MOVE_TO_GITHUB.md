# 📦 انتقال Project Generator به Repository جدید

## مراحل انتقال به GitHub

### گام 1: ایجاد Repository جدید در GitHub

1. برو به: https://github.com/new
2. نام Repository: `CleanArchitecture-ProjectGenerator`
3. توضیحات: `A powerful project generator for Clean Architecture with all e-commerce features`
4. Public یا Private را انتخاب کن
5. **بدون** README, .gitignore, License ایجاد کن (چون خودمان داریم)
6. کلیک "Create repository"

---

### گام 2: آماده‌سازی فایل‌ها

فایل‌های زیر باید منتقل شوند:

```
ProjectGenerator/               ← پوشه اصلی
├── .gitignore                 ✅ آماده شد
├── LICENSE                    ✅ آماده شد
├── README.md                  ✅ آماده شد
├── QUICKSTART_FA.md          ✅ موجود
├── FEATURES_SUMMARY.md       ✅ موجود
├── example-config.json       ✅ موجود
├── example-full-config.json  ✅ موجود
├── ProjectGenerator.csproj   ✅ موجود
├── Program.cs                ✅ موجود
├── Models/                   ✅ موجود
├── Generators/               ✅ موجود
└── Templates/                ✅ موجود

ProjectGenerator.UI/            ← پوشه Windows Forms
├── (تمام فایل‌ها)            ✅ موجود

ProjectGenerator.sln            ✅ موجود
BUILD_AND_RUN.bat              ✅ موجود
build-and-run.sh               ✅ موجود
RUN_WINFORMS.bat               ✅ موجود
RUN_WINFORMS.sh                ✅ موجود
```

---

### گام 3: دستورات Git

#### الف) ایجاد Repository محلی

```bash
# رفتن به پوشه workspace
cd /workspace

# ایجاد پوشه جدید برای repository
mkdir CleanArchitecture-ProjectGenerator
cd CleanArchitecture-ProjectGenerator

# مقداردهی اولیه git
git init
```

#### ب) کپی فایل‌ها

```bash
# کپی فایل‌های اصلی
cp -r ../ProjectGenerator .
cp -r ../ProjectGenerator.UI .
cp ../ProjectGenerator.sln .
cp ../BUILD_AND_RUN.bat .
cp ../build-and-run.sh .
cp ../RUN_WINFORMS.bat .
cp ../RUN_WINFORMS.sh .

# کپی مستندات
cp ../README_GENERATOR.md ./README.md
cp ../QUICK_START.md .
cp ../HOW_TO_RUN.md .
cp ../WINFORMS_FIX.md .
cp ../CHANGELOG.md .
cp ../FIXED_ISSUES.md .

# یا استفاده از این دستور ساده:
```

**یا روش ساده‌تر:**

```bash
# رفتن به پوشه workspace
cd /workspace

# ایجاد یک copy از فایل‌های مورد نیاز
cp -r ProjectGenerator CleanArchitecture-ProjectGenerator-Files/
cp -r ProjectGenerator.UI CleanArchitecture-ProjectGenerator-Files/
cp ProjectGenerator.sln CleanArchitecture-ProjectGenerator-Files/
cp *.bat CleanArchitecture-ProjectGenerator-Files/
cp *.sh CleanArchitecture-ProjectGenerator-Files/
cp *.md CleanArchitecture-ProjectGenerator-Files/

cd CleanArchitecture-ProjectGenerator-Files
git init
```

#### ج) Commit اولیه

```bash
# اضافه کردن همه فایل‌ها
git add .

# Commit اولیه
git commit -m "Initial commit: Clean Architecture Project Generator v1.0.2

Features:
- Windows Forms UI with full Persian support
- Console Application for all platforms
- Complete Clean Architecture implementation
- User Management & Role-Based Authorization
- Seller Panel
- Product Catalog with Categories
- Shopping Cart
- Invoicing System
- Blog System with Comments
- Generate complete projects with 3 Areas (Admin, Seller, User)
- 15+ Domain Entities
- 6 Service Interfaces
- EF Core 9.0 with Identity
- Full documentation in Persian"
```

#### د) اتصال به GitHub

```bash
# جایگزینی با URL repository خودتان
git remote add origin https://github.com/YOUR_USERNAME/CleanArchitecture-ProjectGenerator.git

# تنظیم نام branch اصلی
git branch -M main

# Push به GitHub
git push -u origin main
```

---

### گام 4: دستورات کامل (Copy & Paste)

برای راحتی، این دستورات را **یک‌جا** اجرا کنید:

```bash
# رفتن به workspace
cd /workspace

# ایجاد پوشه جدید
mkdir -p CleanArchitecture-ProjectGenerator
cd CleanArchitecture-ProjectGenerator

# کپی فایل‌ها
cp -r ../ProjectGenerator ./ProjectGenerator
cp -r ../ProjectGenerator.UI ./ProjectGenerator.UI
cp ../ProjectGenerator.sln ./
cp ../*.bat ./
cp ../*.sh ./

# کپی مستندات اصلی
cp ../README_GENERATOR.md ./README.md
cp ../QUICK_START.md ./
cp ../HOW_TO_RUN.md ./
cp ../WINFORMS_FIX.md ./
cp ../CHANGELOG.md ./
cp ../FIXED_ISSUES.md ./

# مقداردهی Git
git init
git add .
git commit -m "Initial commit: Clean Architecture Project Generator v1.0.2"

# جایگزین کنید با URL repository خودتان
# git remote add origin https://github.com/YOUR_USERNAME/CleanArchitecture-ProjectGenerator.git
# git branch -M main
# git push -u origin main
```

---

### گام 5: تنظیمات Repository در GitHub

بعد از push، در صفحه GitHub:

#### 1. About Section
```
Description: 
یک تولید کننده پروژه قدرتمند برای Clean Architecture با تمام امکانات فروشگاهی

Website: 
(اگر داری)

Topics:
clean-architecture
dotnet
csharp
project-generator
aspnet-core
entity-framework-core
code-generator
persian
farsi
ecommerce
```

#### 2. ایجاد Release

1. برو به "Releases" → "Create a new release"
2. Tag: `v1.0.2`
3. Title: `Clean Architecture Project Generator v1.0.2`
4. Description:
```markdown
## ✨ امکانات

- Windows Forms UI با پشتیبانی کامل فارسی
- Console Application برای تمام پلتفرم‌ها
- تولید پروژه کامل با Clean Architecture
- مدیریت کاربران و نقش‌ها
- پنل فروشنده
- کاتالوگ محصولات
- سبد خرید
- سیستم فاکتور
- سیستم بلاگ

## 🚀 نصب و اجرا

### Windows
دانلود و اجرا:
```cmd
RUN_WINFORMS.bat
```

### Linux/Mac
```bash
chmod +x build-and-run.sh
./build-and-run.sh
```

## 📚 مستندات

- [راهنمای سریع](QUICK_START.md)
- [نحوه اجرا](HOW_TO_RUN.md)
- [مستندات کامل](README.md)

## 🐛 رفع مشکلات

- [رفع خطاهای Windows Forms](WINFORMS_FIX.md)
- [لیست تغییرات](CHANGELOG.md)
```

5. Upload Assets (اختیاری): فایل ZIP از پروژه

---

### گام 6: README برای Repository

یک README.md جامع در ریشه repository:

```markdown
# 🎯 Clean Architecture Project Generator

یک تولید کننده پروژه کامل و حرفه‌ای برای .NET

[تصویر لوگو یا اسکرین‌شات]

## ✨ امکانات

- Windows Forms UI
- Console Application  
- Clean Architecture
- و...

[ادامه README.md موجود]
```

---

### گام 7: تنظیمات اضافی

#### GitHub Actions (اختیاری)

ایجاد `.github/workflows/build.yml`:

```yaml
name: Build

on:
  push:
    branches: [ main ]
  pull_request:
    branches: [ main ]

jobs:
  build:
    runs-on: windows-latest
    
    steps:
    - uses: actions/checkout@v3
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: 9.0.x
    
    - name: Restore dependencies
      run: dotnet restore ProjectGenerator.sln
    
    - name: Build
      run: dotnet build ProjectGenerator.sln --configuration Release --no-restore
    
    - name: Test
      run: dotnet test ProjectGenerator.sln --no-build --verbosity normal
```

---

## 🎉 انتقال کامل شد!

Repository شما در:
```
https://github.com/YOUR_USERNAME/CleanArchitecture-ProjectGenerator
```

### مراحل بعدی:

1. ✅ Star به repository خودت بده 😊
2. ✅ README.md را کامل کن
3. ✅ Topics مناسب اضافه کن
4. ✅ License را بررسی کن
5. ✅ اولین Release را منتشر کن

---

## 📢 اشتراک‌گذاری

می‌توانید repository را به اشتراک بگذارید:

- LinkedIn
- Twitter
- Reddit (r/dotnet, r/csharp)
- Dev.to
- Medium

---

**موفق باشید! 🚀**
