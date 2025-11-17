# ساختار پروژه (Project Structure)

## معماری سه‌لایه (Three-Layer Architecture)

این پروژه به سه لایه مجزا تقسیم شده است:

### 1. ProjectGenerator.Core (کتابخانه مشترک)
**هدف**: شامل تمام منطق اصلی، Templates، Models و Generators

**محتویات**:
- `Models/` - کلاس‌های مدل و تنظیمات (ProjectConfig)
- `Templates/` - تمام Template های تولید کد
  - `TemplateProvider.cs` - Template های اصلی
  - `DomainEntityTemplates.cs` - Entity های Domain
  - `ApplicationLayerTemplates.cs` - Commands/Queries/DTOs
  - `InfrastructureTemplates.cs` - DbContext و Configurations
  - `WebSiteTemplates.cs` - Controllers و Views
- `Generators/` - کلاس‌های تولیدکننده لایه‌ها
  - `SolutionGenerator.cs`
  - `LayerGenerator.cs`
  - `WebSiteGenerator.cs`
  - `SeedDataGenerator.cs`

**وابستگی‌ها**:
- Newtonsoft.Json

---

### 2. ProjectGenerator (برنامه Console)
**هدف**: رابط خط فرمان برای تولید پروژه‌ها

**محتویات**:
- `Program.cs` - نقطه ورود برنامه کنسولی

**وابستگی‌ها**:
- ProjectGenerator.Core (Project Reference)

**نحوه اجرا**:
```bash
dotnet run --project ProjectGenerator
```

---

### 3. ProjectGenerator.UI (برنامه WinForms)
**هدف**: رابط گرافیکی برای مدیریت و تولید پروژه‌ها

**محتویات**:
- `MainForm.cs` - فرم اصلی
- `RolesConfigForm.cs` - مدیریت نقش‌ها
- `UsersConfigForm.cs` - مدیریت کاربران
- `RoleEditForm.cs`
- `UserEditForm.cs`

**وابستگی‌ها**:
- ProjectGenerator.Core (Project Reference)

**نحوه اجرا**:
```bash
dotnet run --project ProjectGenerator.UI
```
یا
```bash
./RUN_WINFORMS.bat    # Windows
./RUN_WINFORMS.sh     # Linux/Mac
```

---

## مزایای این ساختار

### ✅ جداسازی کامل (Separation of Concerns)
- منطق اصلی در Core
- UI و Console مستقل از هم
- هیچ وابستگی متقابلی بین UI و Console

### ✅ قابلیت توسعه (Extensibility)
- می‌توان UI های جدید اضافه کرد (مثلاً Blazor، Web API)
- تغییرات Core روی همه UI ها اعمال می‌شود

### ✅ تست‌پذیری (Testability)
- Core به راحتی قابل تست است
- می‌توان Unit Test نوشت

### ✅ قابلیت استفاده مجدد (Reusability)
- Core می‌تواند به صورت NuGet Package منتشر شود
- سایر پروژه‌ها می‌توانند از Core استفاده کنند

---

## Build و Run

### Build کل Solution:
```bash
dotnet restore
dotnet build
```

### پاک کردن و Build مجدد:
```bash
# Windows (PowerShell)
Remove-Item -Recurse -Force ProjectGenerator\obj,ProjectGenerator\bin -ErrorAction SilentlyContinue
Remove-Item -Recurse -Force ProjectGenerator.UI\obj,ProjectGenerator.UI\bin -ErrorAction SilentlyContinue
Remove-Item -Recurse -Force ProjectGenerator.Core\obj,ProjectGenerator.Core\bin -ErrorAction SilentlyContinue
dotnet restore
dotnet build

# Linux/Mac (bash)
rm -rf ProjectGenerator/obj ProjectGenerator/bin
rm -rf ProjectGenerator.UI/obj ProjectGenerator.UI/bin
rm -rf ProjectGenerator.Core/obj ProjectGenerator.Core/bin
dotnet restore
dotnet build
```

---

## نمودار وابستگی‌ها

```
┌─────────────────────┐
│ ProjectGenerator.UI │
│   (WinForms App)    │
└──────────┬──────────┘
           │
           │ references
           ▼
┌──────────────────────┐
│ ProjectGenerator.Core│◄─────────┐
│  (Class Library)     │          │
└──────────────────────┘          │
                                  │ references
                                  │
                    ┌─────────────┴─────────┐
                    │   ProjectGenerator    │
                    │   (Console App)       │
                    └───────────────────────┘
```

---

## توجه مهم برای توسعه‌دهندگان

⚠️ **هرگز** `ProjectGenerator` یا `ProjectGenerator.UI` را به یکدیگر رفرنس ندهید!

✅ هر دو فقط باید به `ProjectGenerator.Core` رفرنس داشته باشند.

✅ تمام تغییرات منطق اصلی باید در `ProjectGenerator.Core` انجام شود.

✅ UI ها فقط برای نمایش و تعامل با کاربر هستند.
