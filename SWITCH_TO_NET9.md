# تغییر به .NET 9

## تاریخ: 2025-11-17

---

## ✅ همه لایه‌ها به .NET 9 تغییر یافتند

### تغییرات انجام شده:

#### 1️⃣ Domain Layer:
```xml
<TargetFramework>net9.0</TargetFramework>
<PackageReference Include="Microsoft.Extensions.Identity.Stores" Version="9.0.0" />
```

#### 2️⃣ Application Layer:
```xml
<TargetFramework>net9.0</TargetFramework>
<PackageReference Include="MediatR" Version="12.2.0" />
<PackageReference Include="FluentValidation" Version="11.9.0" />
<PackageReference Include="FluentValidation.DependencyInjectionExtensions" Version="11.9.0" />
<PackageReference Include="Microsoft.Extensions.DependencyInjection.Abstractions" Version="9.0.0" />
<PackageReference Include="AutoMapper" Version="12.0.1" />
```

#### 3️⃣ Infrastructure Layer:
```xml
<TargetFramework>net9.0</TargetFramework>
<PackageReference Include="Microsoft.EntityFrameworkCore" Version="9.0.0" />
<PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" Version="9.0.0" />
<PackageReference Include="Microsoft.EntityFrameworkCore.Tools" Version="9.0.0" />
<PackageReference Include="Microsoft.AspNetCore.Identity.EntityFrameworkCore" Version="9.0.0" />
<PackageReference Include="Microsoft.Extensions.Configuration.Abstractions" Version="9.0.0" />
<PackageReference Include="Newtonsoft.Json" Version="13.0.3" />
```

#### 4️⃣ WebSite Layer:
```xml
<TargetFramework>net9.0</TargetFramework>
<PackageReference Include="Microsoft.AspNetCore.Identity.EntityFrameworkCore" Version="9.0.0" />
<PackageReference Include="Microsoft.EntityFrameworkCore" Version="9.0.0" />
<PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" Version="9.0.0" />
<PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="9.0.0" />
```

#### 5️⃣ Tests Layer:
```xml
<TargetFramework>net9.0</TargetFramework>
```

---

## 📦 Package Versions نهایی:

| Package | Version |
|---------|---------|
| **Target Framework** | **net9.0** |
| Microsoft.AspNetCore.Identity.EntityFrameworkCore | 9.0.0 |
| Microsoft.EntityFrameworkCore | 9.0.0 |
| Microsoft.EntityFrameworkCore.SqlServer | 9.0.0 |
| Microsoft.EntityFrameworkCore.Tools | 9.0.0 |
| Microsoft.EntityFrameworkCore.Design | 9.0.0 |
| Microsoft.Extensions.Identity.Stores | 9.0.0 |
| Microsoft.Extensions.DependencyInjection.Abstractions | 9.0.0 |
| Microsoft.Extensions.Configuration.Abstractions | 9.0.0 |
| MediatR | 12.2.0 |
| FluentValidation | 11.9.0 |
| AutoMapper | 12.0.1 |
| Newtonsoft.Json | 13.0.3 |

---

## 🚀 دستورات Build:

```bash
# 1. چک کردن .NET 9
dotnet --version
# باید 9.0.x باشه

# 2. حذف پروژه قدیمی
cd D:\Projects
rmdir /s /q AjilMotaleby

# 3. Build ProjectGenerator
cd CleanArchitecture-ProjectGenerator
dotnet clean
dotnet build

# 4. تولید پروژه جدید
dotnet run --project ProjectGenerator

# 5. Build پروژه تولید شده
cd ..\AjilMotaleby
dotnet nuget locals all --clear
dotnet restore --force
dotnet build
```

---

## ⚠️ پیش‌نیاز:

### .NET 9 SDK باید نصب باشه:

```bash
dotnet --version
```

اگه 9.0.x نیست، از اینجا دانلود کن:
**https://dotnet.microsoft.com/download/dotnet/9.0**

---

## ✅ مزایای .NET 9:

- ✅ جدیدترین features
- ✅ Performance بهتر
- ✅ Bug fixes بیشتر
- ✅ Long-term support (تا 2026)

---

## 🎯 نتیجه:

حالا همه پروژه‌های تولید شده روی **.NET 9** هستند! 🚀
