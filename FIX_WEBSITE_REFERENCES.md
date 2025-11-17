# رفع خطاهای WebSite Layer

## تاریخ: 2025-11-17

## خطاهای گزارش شده:
```
1. The type or namespace name 'Infrastructure' does not exist in the namespace
2. The type or namespace name 'ApplicationDbContext' could not be found
3. The type or namespace name 'EntityFrameworkCore' does not exist in the namespace 'Microsoft'
4. The type or namespace name 'Http' does not exist in the namespace 'Microsoft.AspNetCore'
5. The type or namespace name 'IFormFile' could not be found
```

---

## مشکلات پیدا شده:

### 1️⃣ **Program.cs با namespace های اشتباه**
**فایل**: `ProjectGenerator.Core/Templates/TemplateProvider.cs`  
**متد**: `GetEnhancedProgramTemplate()`

#### ❌ قبل از اصلاح:
```csharp
using {namespace}.Application;
using {namespace}.Infrastructure;
using {namespace}.Infrastructure.Data;  // ❌ اشتباه!
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

// ...

builder.Services.AddApplication();  // ❌ متد وجود نداره!
builder.Services.AddInfrastructure(builder.Configuration);  // ❌ متد وجود نداره!

builder.Services.AddIdentity<ApplicationUser, ApplicationRole>  // ❌ ApplicationRole نداریم!

// ...

// Identity classes for Program.cs
public class ApplicationUser : Microsoft.AspNetCore.Identity.IdentityUser<int>  // ❌ inline تعریف شده!
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    // ...
}

public class ApplicationRole : Microsoft.AspNetCore.Identity.IdentityRole<int>  // ❌ اصلاً لازم نیست!
{
    public string? Description { get; set; }
}
```

#### ✅ بعد از اصلاح:
```csharp
using {namespace}.Domain.Entities;  // ✅ ApplicationUser از Domain می‌آد
using {namespace}.Infrastructure.Persistence;  // ✅ namespace صحیح
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add DbContext
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>  // ✅ IdentityRole استاندارد
{
    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 6;
    options.User.RequireUniqueEmail = false;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// ... ادامه کد ...

app.Run();
// ✅ دیگه هیچ کلاس inline ای نداریم!
```

---

### 2️⃣ **WebSite.csproj package های کامل نداشت**
**فایل**: `ProjectGenerator.Core/Templates/TemplateProvider.cs`  
**متد**: `GetWebSiteCsprojTemplate()`

#### ❌ قبل:
```xml
<ItemGroup>
  <ProjectReference Include="..\Application\Application.csproj" />
  <ProjectReference Include="..\Infrastructure\Infrastructure.csproj" />
  <!-- ❌ Domain رو نداشت! -->
</ItemGroup>

<ItemGroup>
  <PackageReference Include="Microsoft.AspNetCore.Identity.EntityFrameworkCore" Version="8.0.10" />
  <!-- ❌ EF Core و SqlServer رو نداشت! -->
  <PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="8.0.10">
    <PrivateAssets>all</PrivateAssets>
    <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
  </PackageReference>
</ItemGroup>
```

#### ✅ بعد:
```xml
<ItemGroup>
  <ProjectReference Include="..\Application\Application.csproj" />
  <ProjectReference Include="..\Infrastructure\Infrastructure.csproj" />
  <ProjectReference Include="..\Domain\Domain.csproj" />  <!-- ✅ اضافه شد -->
</ItemGroup>

<ItemGroup>
  <PackageReference Include="Microsoft.AspNetCore.Identity.EntityFrameworkCore" Version="8.0.10" />
  <PackageReference Include="Microsoft.EntityFrameworkCore" Version="8.0.10" />  <!-- ✅ اضافه شد -->
  <PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" Version="8.0.10" />  <!-- ✅ اضافه شد -->
  <PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="8.0.10">
    <PrivateAssets>all</PrivateAssets>
    <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
  </PackageReference>
</ItemGroup>
```

---

## خلاصه تغییرات:

### Program.cs:
✅ حذف inline class های ApplicationUser و ApplicationRole  
✅ استفاده از `{namespace}.Domain.Entities.ApplicationUser`  
✅ تغییر namespace از `Infrastructure.Data` به `Infrastructure.Persistence`  
✅ حذف `AddApplication()` و `AddInfrastructure()` که وجود نداشتند  
✅ اضافه کردن مستقیم `AddDbContext`  
✅ استفاده از `IdentityRole` استاندارد به جای `ApplicationRole`

### WebSite.csproj:
✅ اضافه شدن `ProjectReference` به Domain  
✅ اضافه شدن `Microsoft.EntityFrameworkCore`  
✅ اضافه شدن `Microsoft.EntityFrameworkCore.SqlServer`

---

## دستور Build:
```bash
cd path/to/generated/project
dotnet clean
dotnet restore
dotnet build
```

## نتیجه:
✅ تمام namespace ها صحیح هستند  
✅ ApplicationDbContext پیدا می‌شود  
✅ Microsoft.EntityFrameworkCore در دسترس است  
✅ تمام package های لازم نصب هستند  
✅ ApplicationUser از Domain استفاده می‌شود، نه inline definition
