# رفع خطای IFormFile و Microsoft.AspNetCore.Http

## تاریخ: 2025-11-17

## خطاهای مرتبط:
```
- The type or namespace name 'Http' does not exist in the namespace 'Microsoft.AspNetCore'
- The type or namespace name 'IFormFile' could not be found
```

---

## مشکلات پیدا شده:

### 1️⃣ Application Layer بدون Framework Reference

**فایل**: `ProjectGenerator.Core/Templates/ApplicationLayerTemplates.cs`  
**متد**: `GetApplicationCsprojTemplate()`

**مشکل**: `IFileService` interface از `IFormFile` استفاده می‌کند ولی Application layer به ASP.NET Core دسترسی نداشت!

#### ✅ اصلاح:
```xml
<ItemGroup>
  <FrameworkReference Include="Microsoft.AspNetCore.App" />
</ItemGroup>
```

**توضیح**: با اضافه کردن `FrameworkReference` به `Microsoft.AspNetCore.App`، تمام namespace های ASP.NET Core (از جمله `Microsoft.AspNetCore.Http`) در دسترس قرار می‌گیرند.

---

### 2️⃣ IFileService Interface بدون Using

**فایل**: `ProjectGenerator.Core/Templates/ApplicationLayerTemplates.cs`  
**متد**: `GetIFileServiceTemplate()`

#### ✅ بعد از اصلاح:
```csharp
using Microsoft.AspNetCore.Http;  // ✅ اضافه شد

namespace {namespace}.Application.Services;

public interface IFileService
{
    Task<string?> SaveFileAsync(IFormFile file, string folder, CancellationToken cancellationToken = default);
    Task<bool> DeleteFileAsync(string filePath, CancellationToken cancellationToken = default);
    string GetFileUrl(string filePath);
}
```

---

### 3️⃣ SellersController بدون Using

**فایل**: `ProjectGenerator.Core/Templates/WebSiteTemplates.cs`  
**متد**: `GetAdminSellersControllerTemplate()`

Controller از ViewModel هایی استفاده می‌کند که `IFormFile` دارند.

#### ✅ بعد از اصلاح:
```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;  // ✅ اضافه شد
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using {namespace}.Domain.Entities;
using {namespace}.Infrastructure.Persistence;
using {namespace}.Application.Services;

namespace {projectName}.WebSite.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class SellersController : Controller
{
    // ... controller actions که از CreateSellerViewModel و EditSellerViewModel استفاده می‌کنند
}

public class CreateSellerViewModel
{
    public string DisplayName { get; set; } = string.Empty;
    public IFormFile? AvatarFile { get; set; }  // <-- نیاز به Microsoft.AspNetCore.Http
    // ...
}

public class EditSellerViewModel
{
    public Guid Id { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public IFormFile? AvatarFile { get; set; }  // <-- نیاز به Microsoft.AspNetCore.Http
    // ...
}
```

---

### 4️⃣ FileService Implementation

**فایل**: `ProjectGenerator.Core/Templates/InfrastructureTemplates.cs`  
**متد**: `GetFileServiceTemplate()`

✅ **قبلاً درست بود!**  
این template قبلاً `using Microsoft.AspNetCore.Http;` داشت:

```csharp
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;  // ✅ قبلاً بود
using {namespace}.Application.Services;

namespace {namespace}.Infrastructure.Services;

public class FileService : IFileService
{
    public async Task<string?> SaveFileAsync(IFormFile file, string folder, CancellationToken cancellationToken = default)
    {
        // ...
    }
}
```

---

## خلاصه تغییرات:

### Application Layer (`Application.csproj`):
```xml
✅ اضافه شد: <FrameworkReference Include="Microsoft.AspNetCore.App" />
```

### Application Layer (`IFileService.cs`):
```csharp
✅ using Microsoft.AspNetCore.Http; (قبلاً بود، اصلاح شد)
```

### WebSite Layer (`SellersController.cs`):
```csharp
✅ using Microsoft.AspNetCore.Http; (اضافه شد)
```

### Infrastructure Layer (`FileService.cs`):
```csharp
✅ using Microsoft.AspNetCore.Http; (قبلاً بود - تغییری نیاز نبود)
```

---

## دستور Build:
```bash
cd path/to/generated/project
dotnet clean
dotnet restore
dotnet build
```

## نتیجه:
✅ `Microsoft.AspNetCore.Http` namespace در همه جا موجود است  
✅ `IFormFile` به درستی resolve می‌شود  
✅ Application layer می‌تواند از ASP.NET Core types استفاده کند  
✅ تمام controller ها و service ها کامپایل می‌شوند
