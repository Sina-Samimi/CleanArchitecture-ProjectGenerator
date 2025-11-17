# خلاصه اصلاحات دور دوم

## تاریخ: 2025-11-17

## خطاهای گزارش شده:
1. ✅ `Attribute 'SetsRequiredMembers' is not valid on this declaration type. It is only valid on 'constructor' declarations.`
2. ✅ `Package downgrade: Microsoft.Extensions.DependencyInjection.Abstractions from 8.0.2 to 8.0.0`
3. ✅ `Package Microsoft.AspNetCore.Identity.EntityFrameworkCore 9.0.0 is not compatible with net8.0`

---

## اصلاحات انجام شده:

### 1️⃣ حذف `[SetsRequiredMembers]` از Static Method
**فایل**: `ProjectGenerator.Core/Templates/DomainEntityTemplates.cs`  
**متد**: `GetUserSessionEntityTemplate()`

**قبل** ❌:
```csharp
[SetsRequiredMembers]
public static UserSession Start(...)
```

**بعد** ✅:
```csharp
public static UserSession Start(...)
```

**توضیح**: Attribute `[SetsRequiredMembers]` فقط روی **constructor** ها مجاز است، نه static method ها!

---

### 2️⃣ ارتقای Package Version
**فایل**: `ProjectGenerator.Core/Templates/ApplicationLayerTemplates.cs`  
**متد**: `GetApplicationCsprojTemplate()`

**تغییر**:
```xml
<!-- قبل -->
<PackageReference Include="Microsoft.Extensions.DependencyInjection.Abstractions" Version="8.0.0" />

<!-- بعد -->
<PackageReference Include="Microsoft.Extensions.DependencyInjection.Abstractions" Version="8.0.2" />
```

**دلیل**: Identity.Stores 8.0.10 نیازمند ورژن 8.0.2 است، نه 8.0.0!

---

### 3️⃣ اصلاح ورژن Identity برای WebSite
**فایل**: `ProjectGenerator.Core/Templates/TemplateProvider.cs`  
**متد**: `GetWebSiteCsprojTemplate()`

**قبل** ❌:
```xml
<PackageReference Include="Microsoft.AspNetCore.Identity.EntityFrameworkCore" Version="9.0.0" />
<PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="9.0.0">
```

**بعد** ✅:
```xml
<PackageReference Include="Microsoft.AspNetCore.Identity.EntityFrameworkCore" Version="8.0.10" />
<PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="8.0.10">
```

---

### 4️⃣ یکسان‌سازی ورژن Infrastructure (قدیمی)
**فایل**: `ProjectGenerator.Core/Templates/TemplateProvider.cs`  
**متد**: `GetInfrastructureCsprojTemplate()` (old template)

همه package های EF Core از `8.0.0` به `8.0.10` ارتقا یافتند:
- ✅ Microsoft.AspNetCore.Identity.EntityFrameworkCore
- ✅ Microsoft.EntityFrameworkCore
- ✅ Microsoft.EntityFrameworkCore.SqlServer
- ✅ Microsoft.EntityFrameworkCore.Tools

---

## بررسی نهایی

### Package Versions (همه 8.0.x):
| Package | Version |
|---------|---------|
| Microsoft.AspNetCore.Identity.EntityFrameworkCore | 8.0.10 |
| Microsoft.EntityFrameworkCore | 8.0.10 |
| Microsoft.EntityFrameworkCore.SqlServer | 8.0.10 |
| Microsoft.EntityFrameworkCore.Tools | 8.0.10 |
| Microsoft.EntityFrameworkCore.Design | 8.0.10 |
| Microsoft.Extensions.Identity.Stores | 8.0.10 |
| Microsoft.Extensions.DependencyInjection.Abstractions | 8.0.2 |

### Target Framework:
✅ همه پروژه‌ها: **net8.0**

---

## دستور Build:
```bash
# پاک کردن بیلدهای قبلی
dotnet clean

# بازسازی packages
dotnet restore

# بیلد کل solution
dotnet build
```

## نتیجه:
✅ همه خطاهای گزارش شده برطرف شدند  
✅ Package version ها یکپارچه هستند  
✅ Static method دیگر [SetsRequiredMembers] ندارد  
✅ همه با net8.0 سازگار هستند
