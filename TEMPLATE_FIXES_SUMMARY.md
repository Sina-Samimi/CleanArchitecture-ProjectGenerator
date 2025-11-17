# خلاصه اصلاحات Template ها برای رفع خطاهای پروژه تولید شده

## تاریخ: 2025-11-17

## خطاهای گزارش شده توسط کاربر:
1. `The type or namespace name 'Entity' could not be found`
2. `The name 'UpdateDate' does not exist in the current context`
3. `The type or namespace name 'AspNetCore' does not exist in the namespace 'Microsoft'`
4. `The type or namespace name 'IdentityUser' could not be found`
5. `Attribute 'SetsRequiredMembers' is not valid on this declaration type`
6. `Project Domain is not compatible with net8.0 (.NETCoreApp,Version=v8.0). Project Domain supports: net9.0`
7. تناقض ورژن بین لایه‌های مختلف

## اصلاحات انجام شده:

### 1. یکسان‌سازی ورژن .NET (net8.0)
- **فایل**: `ProjectGenerator.Core/Templates/TemplateProvider.cs`
- **تغییر**: همه `TargetFramework` ها از `net9.0` به `net8.0` تغییر یافتند
  - ✅ `GetBasicCsprojTemplate()` (برای Domain و SharedKernel)
  - ✅ `GetApplicationCsprojTemplate()` 
  - ✅ `GetInfrastructureCsprojTemplate()`
  - ✅ `GetTestsCsprojTemplate()`
  - ✅ `GetWebSiteCsprojTemplate()`

### 2. اضافه کردن کلاس پایه Entity
- **فایل**: `ProjectGenerator.Core/Templates/TemplateProvider.cs`
- **متد جدید**: `GetEntityBaseClassTemplate()`
- **محتوا**:
```csharp
public abstract class Entity
{
    public Guid Id { get; protected set; } = Guid.NewGuid();
    public DateTimeOffset CreateDate { get; protected set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdateDate { get; protected set; } = DateTimeOffset.UtcNow;
    public IPAddress Ip { get; protected set; } = IPAddress.None;
}
```

### 3. اضافه کردن Identity Package به Domain
- **فایل**: `ProjectGenerator.Core/Templates/TemplateProvider.cs`
- **متد**: `GetBasicCsprojTemplate()`
- **Package اضافه شده**:
```xml
<PackageReference Include="Microsoft.Extensions.Identity.Stores" Version="8.0.10" />
```

### 4. به‌روزرسانی Infrastructure Packages
- **فایل**: `ProjectGenerator.Core/Templates/InfrastructureTemplates.cs`
- **متد**: `GetInfrastructureCsprojTemplate()`
- **Packages به‌روز شده**:
  - `Microsoft.EntityFrameworkCore`: 8.0.0 → 8.0.10
  - `Microsoft.EntityFrameworkCore.SqlServer`: 8.0.0 → 8.0.10
  - `Microsoft.EntityFrameworkCore.Tools`: 8.0.0 → 8.0.10
  - `Microsoft.AspNetCore.Identity.EntityFrameworkCore`: 8.0.0 → 8.0.10
  - ✅ اضافه شد: `Newtonsoft.Json` Version 13.0.3

### 5. اضافه کردن AutoMapper به Application
- **فایل**: `ProjectGenerator.Core/Templates/ApplicationLayerTemplates.cs`
- **متد**: `GetApplicationCsprojTemplate()`
- **Package اضافه شده**:
```xml
<PackageReference Include="AutoMapper" Version="12.0.1" />
```

### 6. اصلاح Using در Product Entity
- **فایل**: `ProjectGenerator.Core/Templates/DomainEntityTemplates.cs`
- **متد**: `GetProductEntityTemplate()`
- **تغییر**:
  - ❌ حذف: `using {namespace}.Domain.Entities;` (چرخه ای!)
  - ✅ اضافه: `using {namespace}.Domain.Base;`

### 7. اصلاح فراخوانی در LayerGenerator
- **فایل**: `ProjectGenerator.Core/Generators/LayerGenerator.cs`
- **متد**: `GenerateDomainLayer()`
- **تغییر**:
  - ❌ قبلی: `_templateProvider.GetBaseEntityTemplate()`
  - ✅ جدید: `_templateProvider.GetEntityBaseClassTemplate()`

## بررسی تمام Entity Templates:
✅ همه 23 entity template بررسی شدند
✅ همگی `using {namespace}.Domain.Base;` را دارند
✅ همگی از `[SetsRequiredMembers]` فقط روی constructorها استفاده می‌کنند

## وضعیت نهایی:
- ✅ Domain Layer: net8.0 + Identity package
- ✅ Application Layer: net8.0 + AutoMapper
- ✅ Infrastructure Layer: net8.0 + EF Core 8.0.10
- ✅ WebSite Layer: net8.0
- ✅ Test Layer: net8.0
- ✅ کلاس Entity با UpdateDate property
- ✅ کلاس SeoEntity که از Entity ارث‌بری می‌کند
- ✅ ApplicationUser با using Microsoft.AspNetCore.Identity
- ✅ همه Entity ها Base namespace را import می‌کنند

## دستور Build:
```bash
cd /path/to/generated/project
dotnet clean
dotnet restore
dotnet build
```

## نکات مهم:
1. همه پروژه‌های تولید شده از این به بعد روی **net8.0** خواهند بود
2. Domain layer دیگر مستقل نیست و به `Microsoft.Extensions.Identity.Stores` وابسته است
3. همه package های EF Core به ورژن **8.0.10** به‌روز شده‌اند
4. SeoEntity property به نام `UpdateDate` دارد که از کلاس پایه `Entity` می‌آید
