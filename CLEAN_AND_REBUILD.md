# راهنمای پاک کردن Cache و Build مجدد

## مشکل:
تغییرات در `.csproj` فایل‌ها اعمال نمیشن چون:
1. ✅ فایل‌های `obj` و `bin` cache شدن
2. ✅ IDE متوجه تغییرات نشده
3. ✅ NuGet packages restore نشده

---

## راه حل (گام به گام):

### روش 1️⃣: پاک کردن کامل (توصیه میشه!)

```bash
# 1. بستن Visual Studio یا Rider
# 2. رفتن به پوشه پروژه
cd D:\Projects\AjilMotaleby

# 3. پاک کردن همه obj و bin ها
for /d /r . %d in (bin,obj) do @if exist "%d" rd /s /q "%d"

# یا در PowerShell:
Get-ChildItem -Path . -Include bin,obj -Recurse -Directory | Remove-Item -Recurse -Force

# 4. پاک کردن NuGet cache
dotnet nuget locals all --clear

# 5. Restore packages
dotnet restore

# 6. Build کل solution
dotnet build

# 7. اگه باز خطا داد، یکی یکی:
dotnet build src/Domain/Domain.csproj
dotnet build src/Application/Application.csproj
dotnet build src/Infrastructure/Infrastructure.csproj
dotnet build src/AjilMotaleby.WebSite/AjilMotaleby.WebSite.csproj
```

---

### روش 2️⃣: از طریق Visual Studio

1. **بستن Visual Studio کامل**
2. پوشه پروژه رو باز کن: `D:\Projects\AjilMotaleby`
3. **دستی حذف کن**:
   - همه پوشه‌های `bin`
   - همه پوشه‌های `obj`
4. Visual Studio رو دوباره باز کن
5. Solution رو **Unload** و **Reload** کن:
   - راست کلیک روی Solution → `Unload Solution`
   - راست کلیک روی Solution → `Reload Solution`
6. **Rebuild Solution**: `Build` → `Rebuild Solution`

---

### روش 3️⃣: Clean Build (سریع)

```bash
cd D:\Projects\AjilMotaleby

# Clean
dotnet clean

# Restore
dotnet restore --force

# Rebuild
dotnet build --no-incremental
```

---

## اگه هنوز کار نکرد:

### بررسی دستی `.csproj` فایل‌ها:

#### ✅ `src/Application/Application.csproj`:
```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="MediatR" Version="12.2.0" />
    <PackageReference Include="FluentValidation" Version="11.9.0" />
    <PackageReference Include="FluentValidation.DependencyInjectionExtensions" Version="11.9.0" />
    <PackageReference Include="Microsoft.Extensions.DependencyInjection.Abstractions" Version="8.0.2" />
    <PackageReference Include="AutoMapper" Version="12.0.1" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\Domain\Domain.csproj" />
  </ItemGroup>

  <ItemGroup>
    <FrameworkReference Include="Microsoft.AspNetCore.App" />
  </ItemGroup>

</Project>
```

#### ✅ `src/Infrastructure/Infrastructure.csproj`:
```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
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

  <ItemGroup>
    <FrameworkReference Include="Microsoft.AspNetCore.App" />
  </ItemGroup>

</Project>
```

#### ✅ `src/AjilMotaleby.WebSite/AjilMotaleby.WebSite.csproj`:
```xml
<Project Sdk="Microsoft.NET.Sdk.Web">

  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\Application\Application.csproj" />
    <ProjectReference Include="..\Infrastructure\Infrastructure.csproj" />
    <ProjectReference Include="..\Domain\Domain.csproj" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.AspNetCore.Identity.EntityFrameworkCore" Version="8.0.10" />
    <PackageReference Include="Microsoft.EntityFrameworkCore" Version="8.0.10" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" Version="8.0.10" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="8.0.10">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
    </PackageReference>
  </ItemGroup>

</Project>
```

---

## آخرین راه (اگه هیچی کار نکرد):

```bash
# 1. حذف کامل پروژه
cd D:\Projects
rmdir /s /q AjilMotaleby

# 2. تولید مجدد از ProjectGenerator
cd D:\Projects\CleanArchitecture-ProjectGenerator
dotnet clean
dotnet build
dotnet run --project ProjectGenerator

# 3. Build پروژه جدید
cd D:\Projects\AjilMotaleby
dotnet restore
dotnet build
```

---

## نکات مهم:

1. ✅ **همیشه IDE رو ببند** قبل از پاک کردن obj/bin
2. ✅ **dotnet restore --force** رو حتماً بزن
3. ✅ **NuGet cache** رو پاک کن
4. ✅ اگه Visual Studio استفاده میکنی، Solution رو **Reload** کن
5. ✅ اگه Rider استفاده میکنی: `File` → `Invalidate Caches / Restart`

---

## چک کردن Build ها:

```bash
# بررسی Build شدن هر لایه
cd src/Domain
dotnet build
# باید: Build succeeded

cd ../Application
dotnet build
# باید: Build succeeded

cd ../Infrastructure
dotnet build
# باید: Build succeeded

cd ../AjilMotaleby.WebSite
dotnet build
# باید: Build succeeded
```

---

## اگه خطا داد:
خطای دقیق رو برام بفرست، مخصوصاً:
- کدوم لایه خطا میده؟
- متن کامل خطا چیه؟
- `dotnet build -v detailed` رو بزن و خروجی رو بفرست
