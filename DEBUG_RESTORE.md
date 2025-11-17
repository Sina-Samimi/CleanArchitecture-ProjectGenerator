# راهنمای Debug کردن مشکل Restore

## مشکل:
پکیج‌ها تو `.csproj` هستن ولی در NuGet نصب نشدن!

---

## ✅ راه حل (گام به گام):

### 1️⃣ پاک کردن کامل Cache:

```bash
# باز کردن CMD به عنوان Administrator
cd D:\Projects\AjilMotaleby

# پاک کردن bin و obj
for /d /r . %d in (bin,obj) do @if exist "%d" rd /s /q "%d"

# پاک کردن NuGet cache
dotnet nuget locals all --clear
```

---

### 2️⃣ Restore با جزئیات کامل:

```bash
# Restore با log کامل
dotnet restore -v detailed > restore-log.txt 2>&1
```

**بعد فایل `restore-log.txt` رو باز کن و ببین چه خطایی میده!**

معمولاً خطاها مثل این هستن:
- ❌ Package version conflict
- ❌ Network error
- ❌ NuGet source غیرفعال
- ❌ Circular dependency

---

### 3️⃣ Restore هر لایه جداگانه:

```bash
cd src

# Domain
cd Domain
dotnet restore -v detailed
# اگه خطا داد، متن کامل خطا رو بگیر
cd ..

# Application  
cd Application
dotnet restore -v detailed
# اگه خطا داد، متن کامل خطا رو بگیر
cd ..

# Infrastructure
cd Infrastructure
dotnet restore -v detailed
# اگه خطا داد، متن کامل خطا رو بگیر
cd ..

# WebSite
cd AjilMotaleby.WebSite
dotnet restore -v detailed
# اگه خطا داد، متن کامل خطا رو بگیر
cd ..
```

---

### 4️⃣ بررسی NuGet Config:

```bash
# چک کردن source های NuGet
dotnet nuget list source
```

**خروجی باید اینطوری باشه:**
```
Registered Sources:
  1.  nuget.org [Enabled]
      https://api.nuget.org/v3/index.json
```

اگه غیرفعال بود:
```bash
dotnet nuget enable source nuget.org
```

---

### 5️⃣ بررسی Internet و Proxy:

```bash
# تست دانلود یه package
dotnet add package Newtonsoft.Json --version 13.0.3
```

اگه این کار کرد، یعنی اینترنت OK هست.

---

### 6️⃣ بررسی دستی `.csproj` ها:

#### 📄 `src/Domain/Domain.csproj`:
```bash
type src\Domain\Domain.csproj
```

باید دقیقاً اینطوری باشه:
```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.Extensions.Identity.Stores" Version="8.0.10" />
  </ItemGroup>

</Project>
```

#### 📄 `src/Application/Application.csproj`:
```bash
type src\Application\Application.csproj
```

باید اینطوری باشه:
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

#### 📄 `src/Infrastructure/Infrastructure.csproj`:
```bash
type src\Infrastructure\Infrastructure.csproj
```

باید اینطوری باشه:
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

---

### 7️⃣ Force Restore با Package Source مستقیم:

```bash
dotnet restore --force --source https://api.nuget.org/v3/index.json
```

---

### 8️⃣ بررسی .NET SDK:

```bash
# چک کردن ورژن .NET
dotnet --version
```

باید **8.0.x** باشه. اگه نیست:
```bash
# لیست SDK های نصب شده
dotnet --list-sdks
```

اگه .NET 8 نصب نیست، باید از [اینجا](https://dotnet.microsoft.com/download/dotnet/8.0) دانلود کنی.

---

### 9️⃣ استفاده از Visual Studio (اگه داری):

1. پروژه رو تو Visual Studio باز کن
2. `Tools` → `Options` → `NuGet Package Manager` → `Package Sources`
3. مطمئن شو `nuget.org` فعال هست
4. `Tools` → `NuGet Package Manager` → `Package Manager Console`
5. اجرا کن:
```powershell
Update-Package -reinstall
```

---

### 🔟 آخرین راه (Nuclear Option):

```bash
# 1. حذف کامل global packages
rmdir /s /q %userprofile%\.nuget\packages

# 2. حذف پروژه
cd D:\Projects
rmdir /s /q AjilMotaleby

# 3. تولید مجدد
cd CleanArchitecture-ProjectGenerator
dotnet clean
dotnet restore
dotnet build
dotnet run --project ProjectGenerator

# 4. رفتن به پروژه جدید
cd ..\AjilMotaleby

# 5. Restore با force
dotnet restore --force --no-cache

# 6. Build
dotnet build
```

---

## چک کردن Package ها بعد از Restore:

```bash
# بررسی packages نصب شده
dir %userprofile%\.nuget\packages\microsoft.entityframeworkcore\8.0.10
dir %userprofile%\.nuget\packages\microsoft.aspnetcore.identity.entityframeworkcore\8.0.10
```

اگه این پوشه‌ها خالی باشن، یعنی restore موفق نبوده!

---

## ⚠️ خطاهای معمول:

### خطا 1: NU1101 - Package not found
```
error NU1101: Unable to find package Microsoft.EntityFrameworkCore. No packages exist with this id in source(s): nuget.org
```

**راه حل**: Check internet connection & NuGet source

### خطا 2: NU1202 - Package is not compatible
```
error NU1202: Package Microsoft.EntityFrameworkCore 8.0.10 is not compatible with net8.0
```

**راه حل**: Update .NET SDK to 8.0.x

### خطا 3: NU1605 - Detected package downgrade
```
warning NU1605: Detected package downgrade
```

**راه حل**: یکسان کردن ورژن همه package های مرتبط

---

## 📤 اگه هنوز کار نکرد:

خروجی این دستورات رو برام بفرست:

```bash
# 1. ورژن .NET
dotnet --version

# 2. SDK های نصب شده
dotnet --list-sdks

# 3. NuGet sources
dotnet nuget list source

# 4. Restore با log کامل
cd D:\Projects\AjilMotaleby
dotnet restore -v detailed > D:\restore-detailed.txt 2>&1
```

بعد فایل `D:\restore-detailed.txt` رو برام بفرست.

---

## 🎯 چک لیست:

- [ ] `dotnet --version` = 8.0.x
- [ ] `dotnet nuget list source` = nuget.org فعال
- [ ] Internet connection OK
- [ ] `dotnet restore` بدون خطا
- [ ] پوشه `%userprofile%\.nuget\packages\` پر از package ها
- [ ] Visual Studio/Rider بسته است
- [ ] همه `bin` و `obj` پاک شده
