# ویژگی‌ها و قابلیت‌های ProjectGenerator

## 🎯 ویژگی‌های اصلی

### 1. ساختار Clean Architecture
- **Domain Layer**: موجودیت‌ها، ارزش‌ها، رویدادها
- **SharedKernel**: اینترفیس‌ها، نتایج، گاردها
- **Application Layer**: سرویس‌ها، DTO ها، mapping
- **Infrastructure Layer**: Repository ها، DbContext، Identity
- **Presentation Layer**: وب سایت ASP.NET Core MVC
- **Tests Layer**: تست‌های واحد با xUnit

### 2. Template های آماده

#### BaseEntity
```csharp
public abstract class BaseEntity
{
    public int Id { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime? ModifiedDate { get; set; }
    public bool IsDeleted { get; set; }
}
```

#### IRepository Interface
```csharp
public interface IRepository<T> where T : class
{
    Task<T?> GetByIdAsync(int id);
    Task<IEnumerable<T>> GetAllAsync();
    Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate);
    Task<T> AddAsync(T entity);
    Task UpdateAsync(T entity);
    Task DeleteAsync(T entity);
    Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate);
}
```

#### Result Pattern
```csharp
public class Result
{
    public bool IsSuccess { get; }
    public string Message { get; }
    public List<string> Errors { get; }
}

public class Result<T> : Result
{
    public T? Data { get; }
}
```

#### Generic Repository
```csharp
public class GenericRepository<T> : IRepository<T> where T : class
{
    // پیاده‌سازی کامل CRUD
}
```

### 3. Seed Data Management

قابلیت ایجاد خودکار:
- **Roles**: Admin, Teacher, Student, User
- **Users**: کاربران اولیه با نقش‌های مشخص
- **Permissions**: دسترسی‌های مبتنی بر نقش

فایل‌های JSON قابل ویرایش:
- `roles.json`
- `users.json`

کلاس `DatabaseSeeder` برای اجرای seed:
```csharp
public class DatabaseSeeder
{
    public async Task SeedAsync()
    {
        await SeedRolesAsync();
        await SeedUsersAsync();
    }
}
```

### 4. سه حالت اجرا

#### Interactive Mode
- تعاملی و کاربرپسند
- راهنمایی گام‌به‌گام
- مناسب برای کاربران مبتدی

#### Command-line Mode
```bash
dotnet run -- -n MyProject -o /path/to/output --seed-data
```
- سریع و خودکار
- مناسب برای اسکریپت‌نویسی
- قابل استفاده در CI/CD

#### Config File Mode
```bash
dotnet run -- --config project-config.json
```
- تکرارپذیر
- قابل مدیریت با Git
- مناسب برای تیم‌ها

### 5. پروژه‌های قابل سفارشی‌سازی

#### فلگ‌های موجود:
- `--no-web`: بدون پروژه WebSite
- `--no-tests`: بدون پروژه Tests
- `--seed-data`: با seed data
- `--namespace`: تعیین namespace سفارشی

#### مثال‌ها:
```bash
# فقط Core Layers
dotnet run -- -n MyLibrary --no-web --no-tests

# فقط با Web
dotnet run -- -n MyWebApp --no-tests

# کامل با seed data
dotnet run -- -n MyFullApp --seed-data
```

## 🚀 مزایای استفاده

### 1. صرفه‌جویی در زمان
- ایجاد پروژه در کمتر از 1 دقیقه
- بدون نیاز به کپی‌پیست
- ساختار استاندارد

### 2. کیفیت کد
- معماری Clean Architecture
- Best Practices
- SOLID Principles

### 3. مستقل و Portable
- بدون وابستگی به پروژه دیگر
- قابل استفاده در هر پروژه‌ای
- منبع باز و قابل توسعه

### 4. قابلیت توسعه
- Template های قابل سفارشی‌سازی
- امکان افزودن Generator جدید
- کد تمیز و خوانا

## 📦 Package های پیش‌فرض

### Domain & SharedKernel
- هیچ وابستگی خارجی

### Application
- FluentValidation
- MediatR

### Infrastructure
- Entity Framework Core 9.0
- Identity Framework
- SQL Server Provider
- Newtonsoft.Json

### Tests
- xUnit
- Moq
- Microsoft.NET.Test.Sdk

### WebSite
- ASP.NET Core MVC 9.0
- Identity UI
- Entity Framework Design

## 🔧 سفارشی‌سازی

### افزودن Template جدید

1. به `Templates/TemplateProvider.cs` بروید
2. متد جدید اضافه کنید:
```csharp
public string GetMyCustomTemplate()
{
    return $@"namespace {_namespace}.MyNamespace;
    
public class MyClass
{{
    // Your code here
}}";
}
```

3. در Generator مربوطه استفاده کنید

### افزودن Layer جدید

1. `LayerType` enum را تکمیل کنید
2. متد جدید در `LayerGenerator` اضافه کنید
3. Template های مورد نیاز را بسازید

### تغییر ساختار پوشه‌ها

در `LayerGenerator.cs` ساختار دلخواه را تعریف کنید.

## 🎨 استفاده‌های پیشرفته

### 1. ایجاد پروژه با Multiple Database
کافیست در config فایل، connection string های مختلف تعریف کنید.

### 2. ایجاد Microservices
برای هر microservice یک پروژه جداگانه با `--no-web` ایجاد کنید.

### 3. CI/CD Integration
```yaml
# GitHub Actions
- name: Generate Project
  run: |
    dotnet run --project ProjectGenerator -- --config project-config.json
```

### 4. Custom Build Scripts
```bash
#!/bin/bash
for project in ProjectA ProjectB ProjectC; do
    dotnet run -- -n $project -o ./output/$project
done
```

## 📊 آمار

- زمان ایجاد: < 1 دقیقه
- تعداد فایل‌های ایجادی: 20-30+
- تعداد پوشه‌ها: 15-20+
- خطوط کد template: 1000+

## 🔮 قابلیت‌های آینده (Roadmap)

- [ ] پشتیبانی از PostgreSQL
- [ ] Template های API-only (بدون MVC)
- [ ] Blazor WebAssembly support
- [ ] Docker configuration
- [ ] Kubernetes deployment files
- [ ] GraphQL support
- [ ] gRPC services
- [ ] Event Sourcing pattern
- [ ] CQRS implementation
- [ ] Multi-tenancy support

## 💡 نکات و ترفندها

### نکته 1: استفاده مجدد از Config
Config فایل‌های خود را در Git ذخیره کنید تا همیشه همان ساختار را داشته باشید.

### نکته 2: Team Templates
یک repository مشترک برای Template های تیمی بسازید.

### نکته 3: Automation
با Task Scheduler یا Cron Job، به صورت خودکار پروژه‌های نمونه بسازید.

---

**برای پیشنهادات و گزارش باگ، Issue ایجاد کنید!**
