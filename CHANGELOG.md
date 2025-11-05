# تغییرات پروژه Project Generator

## نسخه 1.0.2 - 2025-11-05

### 🐛 رفع خطای Windows Forms

- ✅ **رفع خطای دکمه‌های "تنظیم نقش‌ها" و "تنظیم کاربران"**
  - کلاس‌های داخلی به فایل‌های جداگانه منتقل شدند
  - `RoleEditForm.cs` جدید ایجاد شد
  - `UserEditForm.cs` جدید ایجاد شد
  - `RolesConfigForm.cs` تمیز شد
  - `UsersConfigForm.cs` تمیز شد

### 🔧 بهبودها

- ✅ **ساختار بهتر**
  - هر Form در فایل جداگانه
  - Namespace های یکسان
  - کد تمیزتر و قابل نگهداری‌تر

- ✅ **اسکریپت‌های جدید**
  - `RUN_WINFORMS.bat` - اجرای سریع Windows Forms
  - `RUN_WINFORMS.sh` - اجرای Console در Linux/Mac

### 📚 مستندات

- ✅ `WINFORMS_FIX.md` - توضیح کامل رفع مشکل

---

## نسخه 1.0.1 - 2025-11-05

### 🐛 رفع خطاها

- ✅ **رفع خطای "Missing partial modifier"**
  - تمام فایل‌های TemplateProvider حالا `partial class` هستند
  - `TemplateProvider.cs` 
  - `DomainEntityTemplates.cs`
  - `ApplicationLayerTemplates.cs`
  - `InfrastructureTemplates.cs`

### 🔧 بهبودها

- ✅ **ادغام دو پروژه**
  - ایجاد `ProjectGenerator.sln`
  - ProjectGenerator (Console)
  - ProjectGenerator.UI (Windows Forms)

- ✅ **اسکریپت‌های اجرا**
  - `BUILD_AND_RUN.bat` برای Windows
  - `build-and-run.sh` برای Linux/Mac

- ✅ **مستندات بهتر**
  - `HOW_TO_RUN.md` - راهنمای اجرا
  - `README_GENERATOR.md` - README اصلی
  - `CHANGELOG.md` - این فایل

---

## نسخه 1.0.0 - 2025-11-05

### ✨ امکانات اولیه

#### Windows Forms Application
- رابط کاربری گرافیکی فارسی
- مدیریت نقش‌ها با DataGridView
- مدیریت کاربران اولیه
- ذخیره/بارگذاری تنظیمات JSON
- نوار پیشرفت

#### Project Config
- پشتیبانی از تمام فیچرها
- UserManagement
- SellerPanel
- ProductCatalog
- ShoppingCart
- Invoicing
- BlogSystem

#### Domain Entities
15+ Entity کامل:
- Product, Category, ProductImage
- Order, OrderItem, Invoice
- Cart, CartItem
- Blog, BlogComment, BlogCategory
- و Enum های مربوطه

#### Application Layer
- 6 Service Interface
- DTOs کامل
- DependencyInjection

#### Infrastructure Layer
- ApplicationDbContext با پیکربندی کامل
- GenericRepository
- EF Core Configurations
- DependencyInjection

#### WebSite Layer
**3 Area کامل:**

**Admin Area:**
- HomeController (Dashboard)
- UsersController (CRUD)
- RolesController
- ProductsController
- CategoriesController
- OrdersController
- BlogsController

**Seller Area:**
- HomeController (Dashboard)
- ProductsController (محصولات خود)
- OrdersController

**User Area:**
- HomeController (Dashboard)
- ProfileController
- OrdersController

**Main Controllers:**
- HomeController
- AccountController
- ProductController
- CartController
- CheckoutController
- BlogController

#### Views و Layouts
- Layout اصلی
- AdminLayout
- SellerLayout
- UserLayout
- Dashboard Views
- CRUD Views

#### Authentication & Authorization
- ASP.NET Core Identity
- Role-Based Authorization
- Cookie Authentication
- Session Management

#### مستندات
- README.md کامل
- QUICKSTART_FA.md
- FEATURES_SUMMARY.md
- GENERATOR_COMPLETE_SUMMARY.md
- example-config.json
- example-full-config.json

---

## برنامه آینده

### نسخه 1.1.0 (در آینده)
- [ ] پشتیبانی از PostgreSQL
- [ ] پشتیبانی از MySQL
- [ ] تولید Docker Compose
- [ ] CI/CD Templates
- [ ] Swagger/OpenAPI

### نسخه 1.2.0 (در آینده)
- [ ] Blazor WebAssembly Option
- [ ] API-Only Mode
- [ ] GraphQL Support
- [ ] Multi-Language Support

### نسخه 2.0.0 (در آینده)
- [ ] Microservices Support
- [ ] Event-Driven Architecture
- [ ] CQRS با MediatR
- [ ] Redis Caching
- [ ] RabbitMQ/Kafka

---

## نکات نسخه فعلی

### نقاط قوت
✅ Clean Architecture کامل  
✅ تمام امکانات فروشگاهی  
✅ Windows Forms UI راحت  
✅ مستندات جامع فارسی  
✅ Production-Ready  

### نکات
⚠️ فقط SQL Server پشتیبانی می‌شود  
⚠️ Windows Forms فقط روی Windows  
⚠️ نیاز به .NET 9.0 SDK  

---

**برای گزارش باگ یا پیشنهاد: Issue در GitHub ایجاد کنید**
