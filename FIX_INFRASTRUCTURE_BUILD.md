# رفع مشکل Build شدن Infrastructure Layer

## تاریخ: 2025-11-17

## مشکل گزارش شده:
```
- The type or namespace name 'Infrastructure' does not exist in the namespace
- The type or namespace name 'ApplicationDbContext' could not be found
- The type or namespace name 'EntityFrameworkCore' does not exist in the namespace 'Microsoft'
```

**علت**: WebSite نمی‌تونه Infrastructure رو ببینه چون **خود Infrastructure build نمیشه!**

---

## علت اصلی:

### Infrastructure Layer نمی‌تونست build بشه! ❌

**فایل**: `ProjectGenerator.Core/Templates/InfrastructureTemplates.cs`  
**متد**: `GetInfrastructureCsprojTemplate()`

**مشکل**: Infrastructure از `FileService` استفاده می‌کنه که به `IWebHostEnvironment` و `IFormFile` نیاز داره، ولی `FrameworkReference` نداشت!

#### ❌ قبل از اصلاح:
```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <RootNamespace>{namespace}.Infrastructure</RootNamespace>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.EntityFrameworkCore" Version="8.0.10" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" Version="8.0.10" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.Tools" Version="8.0.10">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
    </PackageReference>
    <PackageReference Include="Microsoft.AspNetCore.Identity.EntityFrameworkCore" Version="8.0.10" />
    <PackageReference Include="Microsoft.Extensions.Configuration.Abstractions" Version="8.0.0" />
    <PackageReference Include="Newtonsoft.Json" Version="13.0.3" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\Domain\Domain.csproj" />
    <ProjectReference Include="..\Application\Application.csproj" />
  </ItemGroup>

  <!-- ❌ FrameworkReference نبود! -->

</Project>
```

**نتیجه**: `FileService` خطا می‌داد:
```csharp
using Microsoft.AspNetCore.Hosting;  // ❌ پیدا نمیشه!
using Microsoft.AspNetCore.Http;     // ❌ پیدا نمیشه!

public class FileService : IFileService
{
    private readonly IWebHostEnvironment _environment;  // ❌ پیدا نمیشه!
    
    public async Task<string?> SaveFileAsync(IFormFile file, ...)  // ❌ پیدا نمیشه!
    {
        // ...
    }
}
```

---

## راه حل:

### ✅ اضافه کردن FrameworkReference به Infrastructure:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <RootNamespace>{namespace}.Infrastructure</RootNamespace>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.EntityFrameworkCore" Version="8.0.10" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" Version="8.0.10" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.Tools" Version="8.0.10">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
    </PackageReference>
    <PackageReference Include="Microsoft.AspNetCore.Identity.EntityFrameworkCore" Version="8.0.10" />
    <PackageReference Include="Microsoft.Extensions.Configuration.Abstractions" Version="8.0.0" />
    <PackageReference Include="Newtonsoft.Json" Version="13.0.3" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\Domain\Domain.csproj" />
    <ProjectReference Include="..\Application\Application.csproj" />
  </ItemGroup>

  <!-- ✅ اضافه شد! -->
  <ItemGroup>
    <FrameworkReference Include="Microsoft.AspNetCore.App" />
  </ItemGroup>

</Project>
```

---

## چرا این مشکل رخ داد؟

Infrastructure Layer شامل `FileService` است که:
- از `IWebHostEnvironment` استفاده می‌کند (برای دسترسی به `wwwroot`)
- از `IFormFile` استفاده می‌کند (برای upload فایل)

این types بخشی از **ASP.NET Core Framework** هستند، نه NuGet packages معمولی!

بدون `<FrameworkReference Include="Microsoft.AspNetCore.App" />`:
- ❌ Infrastructure build نمیشه
- ❌ WebSite نمی‌تونه به Infrastructure دسترسی داشته باشه
- ❌ IDE نمی‌تونه namespace Infrastructure رو ببینه و اندپوینت بده

---

## خلاصه تغییرات در همه لایه‌ها:

### Domain Layer:
```xml
<ItemGroup>
  <PackageReference Include="Microsoft.Extensions.Identity.Stores" Version="8.0.10" />
</ItemGroup>
```

### Application Layer:
```xml
<ItemGroup>
  <FrameworkReference Include="Microsoft.AspNetCore.App" />  <!-- ✅ برای IFileService -->
</ItemGroup>
```

### Infrastructure Layer:
```xml
<ItemGroup>
  <FrameworkReference Include="Microsoft.AspNetCore.App" />  <!-- ✅ برای FileService -->
</ItemGroup>
```

### WebSite Layer:
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

## دستور Build:
```bash
# پاک کردن output قبلی
dotnet clean

# Restore packages
dotnet restore

# Build کل solution
dotnet build

# اگه باز خطا داد، یکی یکی build کن:
dotnet build src/Domain/Domain.csproj
dotnet build src/Application/Application.csproj
dotnet build src/Infrastructure/Infrastructure.csproj
dotnet build src/ProjectName.WebSite/ProjectName.WebSite.csproj
```

---

## نتیجه:
✅ Infrastructure حالا می‌تونه build بشه  
✅ WebSite می‌تونه Infrastructure رو ببینه  
✅ IDE اندپوینت درست نشون میده  
✅ همه namespace ها و types در دسترس هستند  
✅ FileService بدون خطا کامپایل میشه
