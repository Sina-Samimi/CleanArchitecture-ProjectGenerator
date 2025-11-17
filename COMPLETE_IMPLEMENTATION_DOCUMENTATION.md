# مستند کامل پیاده‌سازی سیستم مدیریت فروشگاه

این مستند شامل جزئیات کامل پیاده‌سازی تمام بخش‌های سیستم از صفر تا 100 است.

---

## 📋 فهرست مطالب

1. [پنل ادمین](#پنل-ادمین)
2. [مدیریت فروشندگان](#مدیریت-فروشندگان)
3. [دسته‌بندی سایت](#دسته‌بندی-سایت)
4. [قسمت فروشگاه](#قسمت-فروشگاه)
5. [قسمت وبلاگ](#قسمت-وبلاگ)
6. [قسمت تنظیمات](#قسمت-تنظیمات)
7. [پنل کاربری](#پنل-کاربری)
8. [پنل فروشنده](#پنل-فروشنده)
9. [صفحات اصلی سایت](#صفحات-اصلی-سایت)

---

## 🎯 پنل ادمین

### 1. مدیریت کاربران

#### مسیرها (Routes)
```
GET  /Admin/Users              - لیست کاربران
GET  /Admin/Users/Create       - فرم ایجاد کاربر
POST /Admin/Users/Create       - ثبت کاربر جدید
GET  /Admin/Users/Edit/{id}    - فرم ویرایش کاربر
POST /Admin/Users/Edit/{id}    - به‌روزرسانی کاربر
POST /Admin/Users/Activate/{id}    - فعال‌سازی کاربر
POST /Admin/Users/Deactivate/{id}  - غیرفعال‌سازی کاربر
POST /Admin/Users/Delete/{id}      - حذف کاربر
```

#### Controller: `UsersController`
**Location:** `EndPoint.WebSite/Areas/Admin/Controllers/UsersController.cs`

**Actions:**
- `Index([FromQuery] UserListFilterInput? filters)` - نمایش لیست کاربران با فیلتر
- `Create()` - نمایش فرم ایجاد کاربر (Modal یا Full Page)
- `Create(CreateUserViewModel model)` - ثبت کاربر جدید
- `Edit(string id)` - نمایش فرم ویرایش کاربر
- `Edit(string id, EditUserViewModel model)` - به‌روزرسانی کاربر
- `Activate(string id, ActivateUserViewModel model)` - فعال‌سازی کاربر
- `Deactivate(string id, DeactivateUserViewModel model)` - غیرفعال‌سازی کاربر
- `Delete(string id, DeleteUserViewModel model)` - حذف کاربر

**ViewModels:**
- `UserListViewModel` - شامل لیست کاربران، فیلترها، pagination
- `CreateUserViewModel` - شامل: Email, Password, FullName, PhoneNumber, SelectedRoles, IsActive, Avatar
- `EditUserViewModel` - شامل: Id, Email, FullName, PhoneNumber, SelectedRoles, IsActive, AvatarPath
- `UserListItemViewModel` - شامل: Id, Email, FullName, IsActive, IsDeleted, Roles, AvatarPath

**فیلترها:**
- FullName (نام کامل)
- PhoneNumber (شماره تلفن)
- Role (نقش)
- Status (وضعیت: All, Active, Inactive, Deleted)
- RegisteredFrom/RegisteredTo (تاریخ ثبت‌نام)

**ویژگی‌ها:**
- Pagination با PageSize قابل تنظیم (10-50)
- فیلتر پیشرفته بر اساس نقش، وضعیت، تاریخ
- آپلود آواتار (حداکثر 2MB، فرمت‌های: PNG, JPEG, WebP)
- مدیریت نقش‌های چندگانه برای هر کاربر
- فعال/غیرفعال‌سازی کاربران
- حذف نرم (Soft Delete)

**Views:**
- `Index.cshtml` - لیست کاربران با جدول و فیلترها
- `_CreateUserModal.cshtml` - Modal ایجاد کاربر
- `_EditUserModal.cshtml` - Modal ویرایش کاربر
- `_ActivateUserModal.cshtml` - Modal فعال‌سازی
- `_DeactivateUserModal.cshtml` - Modal غیرفعال‌سازی
- `_DeleteUserModal.cshtml` - Modal حذف

**Commands/Queries:**
- `GetUsersQuery` - دریافت لیست کاربران
- `GetUserByIdQuery` - دریافت کاربر بر اساس ID
- `GetRolesQuery` - دریافت لیست نقش‌ها
- `RegisterUserCommand` - ثبت کاربر جدید
- `UpdateUserCommand` - به‌روزرسانی کاربر
- `ActivateUserCommand` - فعال‌سازی
- `DeactivateUserCommand` - غیرفعال‌سازی
- `DeleteUserCommand` - حذف

---

### 2. سطوح دسترسی (Access Levels / Roles)

#### مسیرها
```
GET  /Admin/AccessLevels           - لیست نقش‌ها
GET  /Admin/AccessLevels/Create    - فرم ایجاد نقش
POST /Admin/AccessLevels/Save     - ثبت/ویرایش نقش
GET  /Admin/AccessLevels/Edit/{id} - فرم ویرایش نقش
```

#### Controller: `AccessLevelsController`
**Location:** `EndPoint.WebSite/Areas/Admin/Controllers/AccessLevelsController.cs`

**Actions:**
- `Index()` - نمایش کارت‌های نقش‌ها با مجوزهای هر نقش
- `Create()` - نمایش Modal ایجاد نقش
- `Edit(string id)` - نمایش Modal ویرایش نقش
- `Save(EditAccessLevelViewModel model)` - ثبت یا ویرایش نقش

**ViewModels:**
- `AccessLevelListViewModel` - شامل: Roles (کارت‌ها), PermissionGroups
- `AccessLevelCardViewModel` - شامل: Id, Name, DisplayName, UserCount, Permissions
- `EditAccessLevelViewModel` - شامل: Id, Name, DisplayName, SelectedPermissions, PermissionGroups

**ویژگی‌ها:**
- نمایش نقش‌ها به صورت کارت
- نمایش تعداد کاربران هر نقش
- انتخاب مجوزها به صورت گروه‌بندی شده
- نمایش مجوزهای Core و Custom
- امکان انتخاب چند مجوز برای هر نقش

**Views:**
- `Index.cshtml` - لیست کارت‌های نقش‌ها
- `_AccessLevelModal.cshtml` - Modal ایجاد/ویرایش نقش

**Commands/Queries:**
- `GetRoleAccessLevelsQuery` - دریافت لیست نقش‌ها
- `GetRoleAccessLevelByIdQuery` - دریافت نقش بر اساس ID
- `GetPermissionCatalogQuery` - دریافت کاتالوگ مجوزها
- `SaveRoleAccessLevelCommand` - ثبت/ویرایش نقش

---

### 3. مجوزها (Permissions)

#### مسیرها
```
GET  /Admin/Permissions                    - لیست مجوزها
GET  /Admin/Permissions/Create?group=...  - فرم ایجاد مجوز
POST /Admin/Permissions/Save              - ثبت/ویرایش مجوز
GET  /Admin/Permissions/Edit/{id}         - فرم ویرایش مجوز
POST /Admin/Permissions/Delete/{id}       - حذف مجوز
```

#### Controller: `PermissionsController`
**Location:** `EndPoint.WebSite/Areas/Admin/Controllers/PermissionsController.cs`

**Actions:**
- `Index(int page, int pageSize, string? search, string? group, bool includeCore, bool includeCustom)` - لیست مجوزها با فیلتر
- `Create(string? group)` - نمایش Modal ایجاد مجوز
- `Edit(Guid id)` - نمایش Modal ویرایش مجوز
- `Save(EditPermissionViewModel model)` - ثبت/ویرایش مجوز
- `Delete(Guid id)` - حذف مجوز

**ViewModels:**
- `PermissionListViewModel` - شامل: Groups, PageNumber, PageSize, TotalCount, Filters
- `PermissionListGroupViewModel` - شامل: Key, DisplayName, Permissions
- `PermissionListItemViewModel` - شامل: Id, Key, DisplayName, Description, IsCore, IsCustom, AssignedRoles
- `EditPermissionViewModel` - شامل: Id, Key, DisplayName, Description, IsCore, GroupKey, GroupDisplayName, GroupOptions

**فیلترها:**
- Search (جستجو در نام و توضیحات)
- Group (گروه مجوز)
- IncludeCore (شامل مجوزهای Core)
- IncludeCustom (شامل مجوزهای Custom)

**ویژگی‌ها:**
- گروه‌بندی مجوزها
- نمایش مجوزهای Core (سیستمی) و Custom (سفارشی)
- نمایش نقش‌های مرتبط با هر مجوز
- Pagination
- جستجو و فیلتر پیشرفته

**Views:**
- `Index.cshtml` - لیست مجوزها به صورت گروه‌بندی شده
- `_PermissionModal.cshtml` - Modal ایجاد/ویرایش مجوز

**Commands/Queries:**
- `GetPermissionsQuery` - دریافت لیست مجوزها با فیلتر
- `GetPermissionByIdQuery` - دریافت مجوز بر اساس ID
- `GetPermissionCatalogQuery` - دریافت کاتالوگ مجوزها
- `SavePermissionCommand` - ثبت/ویرایش مجوز
- `DeletePermissionCommand` - حذف مجوز

---

### 4. دسترسی صفحات (Page Access)

#### مسیرها
```
GET  /Admin/PageAccess                    - لیست صفحات و مجوزهایشان
GET  /Admin/PageAccess/Edit?controller=...&action=...&area=... - ویرایش دسترسی صفحه
POST /Admin/PageAccess/Save               - ذخیره دسترسی صفحه
```

#### Controller: `PageAccessController`
**Location:** `EndPoint.WebSite/Areas/Admin/Controllers/PageAccessController.cs`

**Actions:**
- `Index([FromQuery] PageAccessIndexRequest? request)` - لیست صفحات با مجوزهایشان
- `Edit(string controller, string action, string? area)` - ویرایش دسترسی یک صفحه
- `Save(SavePageAccessInputModel input)` - ذخیره دسترسی صفحه

**ViewModels:**
- `PageAccessIndexViewModel` - شامل: Pages, PermissionOptions, AreaOptions, Filters, Pagination
- `PageAccessPageViewModel` - شامل: Area, Controller, Action, DisplayName, Permissions
- `EditPageAccessViewModel` - شامل: Area, Controller, Action, DisplayName, SelectedPermissions, AvailablePermissions

**فیلترها:**
- Search (جستجو در نام صفحه، Controller، Action)
- Area (فیلتر بر اساس Area)
- Permission (فیلتر بر اساس مجوز)
- Restriction (Restricted/Unrestricted/All)

**ویژگی‌ها:**
- نمایش تمام صفحات Admin با مجوزهای مرتبط
- امکان اختصاص چند مجوز به هر صفحه
- فیلتر و جستجو پیشرفته
- Cache invalidation بعد از تغییر

**Views:**
- `Index.cshtml` - لیست صفحات با مجوزها
- `_EditModal.cshtml` - Modal ویرایش دسترسی صفحه

**Commands/Queries:**
- `GetAdminPageAccessOverviewQuery` - دریافت لیست صفحات و مجوزها
- `GetPermissionCatalogQuery` - دریافت کاتالوگ مجوزها
- `SavePageAccessPolicyCommand` - ذخیره دسترسی صفحه

**Services:**
- `IPageDescriptorProvider` - شناسایی صفحات
- `IPageAccessCache` - Cache دسترسی صفحات

---

## 👥 مدیریت فروشندگان (Sellers)

**نکته:** در پروژه فعلی "Teachers" است، در پروژه جدید باید "Sellers" باشد.

#### مسیرها
```
GET  /Admin/Sellers           - لیست فروشندگان
GET  /Admin/Sellers/Create    - فرم ایجاد فروشنده
POST /Admin/Sellers/Create    - ثبت فروشنده جدید
GET  /Admin/Sellers/Edit/{id} - فرم ویرایش فروشنده
POST /Admin/Sellers/Edit/{id} - به‌روزرسانی فروشنده
POST /Admin/Sellers/Activate/{id}   - فعال‌سازی فروشنده
POST /Admin/Sellers/Deactivate/{id}  - غیرفعال‌سازی فروشنده
POST /Admin/Sellers/Delete/{id}     - حذف فروشنده
```

#### Controller: `SellersController` (یا `TeachersController`)
**Location:** `EndPoint.WebSite/Areas/Admin/Controllers/TeachersController.cs`

**Actions:**
- `Index()` - لیست فروشندگان
- `Create()` - نمایش فرم ایجاد فروشنده
- `Create(TeacherProfileFormViewModel model)` - ثبت فروشنده
- `Edit(Guid id)` - نمایش فرم ویرایش
- `Edit(Guid id, TeacherProfileFormViewModel model)` - به‌روزرسانی
- `Activate(Guid id)` - فعال‌سازی
- `Deactivate(Guid id)` - غیرفعال‌سازی
- `Delete(Guid id)` - حذف

**ViewModels:**
- `TeacherProfilesIndexViewModel` - شامل: Profiles, Statistics
- `TeacherProfileFormViewModel` - شامل: DisplayName, Degree, Specialty, Bio, AvatarFile, ContactEmail, ContactPhone, UserId, IsActive

**ویژگی‌ها:**
- اتصال به کاربر سیستم (UserId)
- آپلود آواتار (حداکثر 2MB)
- اطلاعات تماس (Email, Phone)
- اطلاعات تحصیلی (Degree, Specialty)
- فعال/غیرفعال‌سازی
- حذف نرم

**Views:**
- `Index.cshtml` - لیست فروشندگان
- `Form.cshtml` - فرم ایجاد/ویرایش

**Commands/Queries:**
- `GetTeacherProfilesQuery` - دریافت لیست فروشندگان
- `GetTeacherProfileByIdQuery` - دریافت فروشنده بر اساس ID
- `GetUserLookupsQuery` - دریافت لیست کاربران برای اتصال
- `CreateTeacherProfileCommand` - ایجاد فروشنده
- `UpdateTeacherProfileCommand` - به‌روزرسانی
- `ActivateTeacherCommand` - فعال‌سازی
- `DeactivateTeacherCommand` - غیرفعال‌سازی
- `RemoveTeacherProfileCommand` - حذف

**Entity:**
- `TeacherProfile` (در پروژه جدید: `SellerProfile`)
  - DisplayName (required)
  - Degree (optional)
  - Specialty (optional)
  - Bio (optional)
  - AvatarUrl (optional)
  - ContactEmail (optional)
  - ContactPhone (optional)
  - UserId (optional - اتصال به ApplicationUser)
  - IsActive (boolean)

---

## 📁 دسته‌بندی سایت

#### مسیرها
```
GET  /Admin/Catalog/Categories           - مدیریت دسته‌بندی‌های محصول
POST /Admin/Catalog/CreateCategory       - ایجاد دسته‌بندی
POST /Admin/Catalog/UpdateCategory        - ویرایش دسته‌بندی
POST /Admin/Catalog/DeleteCategory/{id}   - حذف دسته‌بندی
```

#### Controller: `CatalogController` - Action: `Categories`
**Location:** `EndPoint.WebSite/Areas/Admin/Controllers/CatalogController.cs`

**Actions:**
- `Categories(Guid? highlightId)` - نمایش درخت دسته‌بندی‌ها
- `CreateCategory(ProductCategoryFormModel model)` - ایجاد دسته‌بندی
- `UpdateCategory(ProductCategoryUpdateFormModel model)` - ویرایش دسته‌بندی
- `DeleteCategory(Guid id)` - حذف دسته‌بندی

**ViewModels:**
- `ProductCategoriesViewModel` - شامل: Tree, Categories (flat), Statistics, CreateCategory, EditCategory, ParentOptions
- `ProductCategoryTreeItemViewModel` - شامل: Id, Name, Slug, Description, ParentId, Depth, Children, DescendantIds
- `ProductCategoryFormModel` - شامل: Name, Slug, Description, ParentId
- `ProductCategoryUpdateFormModel` - شامل: Id, Name, Slug, Description, ParentId

**ویژگی‌ها:**
- ساختار درختی (Tree Structure)
- امکان تعیین Parent برای هر دسته‌بندی
- نمایش عمق (Depth)
- Slug برای SEO
- آمار: تعداد کل، والدین، فرزندان، عمق

**Views:**
- `Categories.cshtml` - نمایش درخت دسته‌بندی‌ها با فرم ایجاد/ویرایش
- `_CategoryTree.cshtml` - Partial View برای نمایش درخت

**Commands/Queries:**
- `GetProductLookupsQuery` - دریافت دسته‌بندی‌ها
- `CreateSiteCategoryCommand` - ایجاد دسته‌بندی
- `UpdateSiteCategoryCommand` - ویرایش
- `DeleteSiteCategoryCommand` - حذف

**Entity:**
- `SiteCategory`
  - Name (required)
  - Slug (optional)
  - Description (optional)
  - ParentId (optional)
  - Scope (Product/Blog)
  - SEO fields

---

## 🛒 قسمت فروشگاه

### 1. لیست محصولات

#### مسیرها
```
GET  /Admin/Catalog              - لیست محصولات
GET  /Admin/Catalog/Create       - فرم ایجاد محصول
POST /Admin/Catalog/Create       - ثبت محصول
GET  /Admin/Catalog/Edit/{id}    - فرم ویرایش محصول
POST /Admin/Catalog/Edit/{id}    - به‌روزرسانی محصول
GET  /Admin/Catalog/Details/{id} - جزئیات محصول
```

#### Controller: `CatalogController`
**Location:** `EndPoint.WebSite/Areas/Admin/Controllers/CatalogController.cs`

**Actions:**
- `Index([FromQuery] ProductIndexRequest? request)` - لیست محصولات با فیلتر
- `Create()` - فرم ایجاد محصول
- `Create(ProductFormViewModel model)` - ثبت محصول
- `Edit(Guid id)` - فرم ویرایش
- `Edit(Guid id, ProductFormViewModel model)` - به‌روزرسانی
- `Details(Guid id)` - جزئیات محصول با آمار فروش

**ViewModels:**
- `ProductIndexViewModel` - شامل: Products, Statistics, Filters, CategoryOptions, TypeOptions, StatusOptions, Pagination
- `ProductFormViewModel` - شامل تمام فیلدهای محصول
- `ProductDetailViewModel` - شامل جزئیات کامل + آمار فروش

**فیلترها:**
- Search (نام، توضیحات)
- CategoryId
- Type (Physical/Digital)
- IsPublished
- MinPrice/MaxPrice
- Page, PageSize

**ویژگی‌ها:**
- دو نوع محصول: Physical (فیزیکی) و Digital (دانلودی)
- مدیریت موجودی (TrackInventory, StockQuantity)
- قیمت و قیمت مقایسه‌ای (CompareAtPrice)
- آپلود تصویر شاخص (Featured Image)
- گالری تصاویر (Gallery)
- فایل دانلودی برای محصولات Digital
- SEO: Title, Description, Keywords, Slug, Robots
- Tags (برچسب‌ها)
- وضعیت انتشار: Draft, Published, Scheduled
- تاریخ انتشار (PublishedAt)
- اتصال به فروشنده (SellerId/TeacherId)
- آمار فروش: تعداد سفارش، درآمد، روند

**Views:**
- `Index.cshtml` - لیست محصولات با فیلتر و آمار
- `Form.cshtml` - فرم ایجاد/ویرایش محصول
- `Details.cshtml` - جزئیات محصول

**Commands/Queries:**
- `GetProductListQuery` - لیست محصولات
- `GetProductDetailQuery` - جزئیات محصول
- `GetProductLookupsQuery` - دسته‌بندی‌ها و Tags
- `GetProductSalesSummaryQuery` - آمار فروش
- `CreateProductCommand` - ایجاد محصول
- `UpdateProductCommand` - به‌روزرسانی

**Sub-features:**
- Execution Steps (گام‌های اجرایی)
  - `ExecutionSteps(Guid id)` - مدیریت گام‌های اجرایی
  - `CreateExecutionStep`, `UpdateExecutionStep`, `DeleteExecutionStep`
- FAQs (سوالات متداول)
  - `Faqs(Guid id)` - مدیریت FAQ
  - `CreateFaq`, `UpdateFaq`, `DeleteFaq`
- Comments (نظرات)
  - `Comments(Guid id)` - مدیریت نظرات
  - `ModerateComment` - تایید/رد نظر

---

### 2. کد تخفیف

#### مسیرها
```
GET  /Admin/DiscountCodes           - لیست کدهای تخفیف
GET  /Admin/DiscountCodes/Create    - فرم ایجاد کد تخفیف
POST /Admin/DiscountCodes/Save      - ثبت/ویرایش کد تخفیف
GET  /Admin/DiscountCodes/Edit/{id} - فرم ویرایش
```

#### Controller: `DiscountCodesController`
**Location:** `EndPoint.WebSite/Areas/Admin/Controllers/DiscountCodesController.cs`

**Actions:**
- `Index()` - لیست کدهای تخفیف با آمار
- `Create()` - Modal ایجاد کد تخفیف
- `Edit(Guid id)` - Modal ویرایش
- `Save(DiscountCodeFormViewModel model)` - ثبت/ویرایش

**ViewModels:**
- `DiscountCodeIndexViewModel` - شامل: Items, Summary, GeneratedAt
- `DiscountCodeFormViewModel` - شامل تمام فیلدهای کد تخفیف
- `DiscountCodeSummaryViewModel` - آمار: TotalCodes, ActiveCodes, ScheduledCodes, ExpiredCodes, etc.

**ویژگی‌ها:**
- Code (کد تخفیف - unique)
- Name (نام)
- Description (توضیحات)
- DiscountType: Percentage (درصدی) یا FixedAmount (مبلغ ثابت)
- DiscountValue (مقدار تخفیف)
- MaxDiscountAmount (حداکثر مبلغ تخفیف - برای درصدی)
- MinimumOrderAmount (حداقل مبلغ سفارش)
- StartsAt/EndsAt (زمان شروع و پایان - با تاریخ شمسی)
- IsActive (فعال/غیرفعال)
- GlobalUsageLimit (حد استفاده کل)
- GroupRules (قوانین گروهی - برای محدودیت استفاده بر اساس گروه)

**Views:**
- `Index.cshtml` - لیست کدهای تخفیف
- `_DiscountCodeModal.cshtml` - Modal ایجاد/ویرایش

**Commands/Queries:**
- `GetDiscountCodeListQuery` - لیست کدهای تخفیف
- `GetDiscountCodeDetailsQuery` - جزئیات کد تخفیف
- `CreateDiscountCodeCommand` - ایجاد
- `UpdateDiscountCodeCommand` - ویرایش

**Entity:**
- `DiscountCode`
  - Code (required, unique)
  - Name (required)
  - Description (optional)
  - DiscountType (enum)
  - DiscountValue (decimal)
  - MaxDiscountAmount (decimal?)
  - MinimumOrderAmount (decimal?)
  - StartsAt (DateTimeOffset)
  - EndsAt (DateTimeOffset?)
  - IsActive (boolean)
  - GlobalUsageLimit (int?)
  - GroupRules (collection)

---

### 3. فاکتور (Invoice)

#### مسیرها
```
GET  /Admin/Invoices              - لیست فاکتورها
GET  /Admin/Invoices/Create       - فرم ایجاد فاکتور
POST /Admin/Invoices/Create       - ثبت فاکتور
GET  /Admin/Invoices/Edit/{id}    - فرم ویرایش
POST /Admin/Invoices/Edit/{id}    - به‌روزرسانی
GET  /Admin/Invoices/Details/{id} - جزئیات فاکتور
GET  /Admin/Invoices/DownloadPdf/{id} - دانلود PDF فاکتور
POST /Admin/Invoices/Cancel/{id}  - لغو فاکتور
POST /Admin/Invoices/Reopen/{id}  - فعال‌سازی مجدد
POST /Admin/Invoices/RecordTransaction - ثبت تراکنش
```

#### Controller: `InvoicesController`
**Location:** `EndPoint.WebSite/Areas/Admin/Controllers/InvoicesController.cs`

**Actions:**
- `Index([FromQuery] InvoiceFilterInput filter)` - لیست فاکتورها
- `Create()` - فرم ایجاد فاکتور
- `Create(InvoiceFormViewModel model)` - ثبت فاکتور
- `Edit(Guid id)` - فرم ویرایش
- `Edit(InvoiceFormViewModel model)` - به‌روزرسانی
- `Details(Guid id)` - جزئیات فاکتور
- `DownloadPdf(Guid id)` - دانلود PDF
- `Cancel(Guid id)` - لغو فاکتور
- `Reopen(Guid id)` - فعال‌سازی مجدد
- `RecordTransaction(InvoiceTransactionFormViewModel model)` - ثبت تراکنش

**ViewModels:**
- `InvoiceIndexViewModel` - شامل: Invoices, Summary, Filter, UserOptions, Pagination
- `InvoiceFormViewModel` - شامل تمام فیلدهای فاکتور
- `InvoiceDetailViewModel` - شامل: Invoice details, Items, Transactions, NewTransaction form

**فیلترها:**
- SearchTerm
- Status (Draft, Pending, Paid, PartiallyPaid, Cancelled, Overdue)
- UserId
- IssueDateFrom/IssueDateTo

**ویژگی‌ها:**
- InvoiceNumber (شماره فاکتور - auto-generate یا manual)
- Title (عنوان)
- Description (توضیحات)
- Currency (IRT, USD, etc.)
- UserId (کاربر)
- IssueDate (تاریخ صدور - شمسی)
- DueDate (تاریخ سررسید - شمسی)
- Items (آیتم‌های فاکتور):
  - Name, Description
  - ItemType (Product, Service, etc.)
  - ReferenceId (ID محصول/سرویس)
  - Quantity, UnitPrice
  - DiscountAmount
  - Attributes (key-value pairs)
- Subtotal (جمع آیتم‌ها)
- DiscountTotal (جمع تخفیف‌ها)
- TaxAmount (مالیات)
- AdjustmentAmount (تعدیل)
- GrandTotal (جمع کل)
- PaidAmount (پرداخت شده)
- OutstandingAmount (باقیمانده)
- Status (وضعیت)
- ExternalReference (مرجع خارجی)
- Transactions (تراکنش‌های پرداخت)
- PDF Generation با QuestPDF

**Views:**
- `Index.cshtml` - لیست فاکتورها
- `Form.cshtml` - فرم ایجاد/ویرایش
- `Details.cshtml` - جزئیات فاکتور

**Commands/Queries:**
- `GetInvoiceListQuery` - لیست فاکتورها
- `GetInvoiceDetailsQuery` - جزئیات فاکتور
- `GetUserLookupsQuery` - لیست کاربران
- `CreateInvoiceCommand` - ایجاد فاکتور
- `UpdateInvoiceCommand` - به‌روزرسانی
- `CancelInvoiceCommand` - لغو
- `ReopenInvoiceCommand` - فعال‌سازی مجدد
- `RecordInvoiceTransactionCommand` - ثبت تراکنش

**Entity:**
- `Invoice`
  - InvoiceNumber (string)
  - Title (string)
  - Description (string?)
  - Currency (string)
  - UserId (string?)
  - IssueDate (DateTimeOffset)
  - DueDate (DateTimeOffset?)
  - Status (enum)
  - Items (collection)
  - Transactions (collection)
  - ExternalReference (string?)

---

### 4. تراکنش (Transaction)

تراکنش‌ها به صورت جزئی از فاکتورها مدیریت می‌شوند. در صفحه Details فاکتور امکان ثبت تراکنش جدید وجود دارد.

**ویژگی‌ها:**
- Amount (مبلغ)
- Method (روش پرداخت: Wallet, OnlineGateway, Cash, BankTransfer, etc.)
- Status (وضعیت: Pending, Succeeded, Failed, Cancelled)
- Reference (شماره مرجع)
- GatewayName (نام درگاه - برای OnlineGateway)
- Description (توضیحات)
- Metadata (JSON - اطلاعات اضافی)
- OccurredAt (تاریخ و زمان تراکنش - شمسی)

**Entity:**
- `InvoiceTransaction`
  - InvoiceId (Guid)
  - Amount (decimal)
  - Method (enum)
  - Status (enum)
  - Reference (string?)
  - GatewayName (string?)
  - Description (string?)
  - Metadata (string? - JSON)
  - OccurredAt (DateTimeOffset?)

---

### 5. کیف پول (Wallet)

#### مسیرها
```
GET  /Admin/Wallets/Charge        - فرم شارژ کیف پول کاربر
POST /Admin/Wallets/Charge        - شارژ کیف پول
```

#### Controller: `WalletsController`
**Location:** `EndPoint.WebSite/Areas/Admin/Controllers/WalletsController.cs`

**Actions:**
- `Charge()` - فرم شارژ کیف پول
- `Charge(WalletChargeFormViewModel model)` - شارژ کیف پول

**ViewModels:**
- `WalletChargeFormViewModel` - شامل: UserId, Amount, Currency, InvoiceTitle, InvoiceDescription, TransactionDescription, PaymentReference, PaymentMethod, UserOptions, PaymentMethodOptions

**ویژگی‌ها:**
- انتخاب کاربر
- مبلغ شارژ
- ارز (Currency)
- عنوان و توضیحات فاکتور
- توضیحات تراکنش
- شماره مرجع پرداخت
- روش پرداخت (Cash, BankTransfer, etc.)
- ایجاد فاکتور خودکار
- ثبت تراکنش خودکار

**Views:**
- `Charge.cshtml` - فرم شارژ کیف پول

**Commands:**
- `GetUserLookupsQuery` - لیست کاربران
- `AdminChargeWalletCommand` - شارژ کیف پول (ایجاد فاکتور + تراکنش)

**Entity:**
- `Wallet`
  - UserId (string - FK to ApplicationUser)
  - Balance (decimal)
  - Currency (string)
  - IsLocked (boolean)
  - LastActivityOn (DateTimeOffset)

- `WalletTransaction`
  - WalletId (Guid)
  - Amount (decimal)
  - Type (enum: Credit, Debit)
  - Status (enum)
  - BalanceAfter (decimal)
  - Reference (string?)
  - Description (string?)
  - InvoiceId (Guid?)
  - OccurredAt (DateTimeOffset)

---

## 📝 قسمت وبلاگ

### 1. لیست وبلاگ‌ها

#### مسیرها
```
GET  /Admin/Blog              - لیست وبلاگ‌ها
GET  /Admin/Blog/Create       - فرم ایجاد وبلاگ
POST /Admin/Blog/Create       - ثبت وبلاگ
GET  /Admin/Blog/Edit/{id}    - فرم ویرایش
POST /Admin/Blog/Edit/{id}    - به‌روزرسانی
POST /Admin/Blog/Delete/{id}  - حذف وبلاگ
GET  /Admin/Blog/Comments/{id} - نظرات وبلاگ
POST /Admin/Blog/ModerateComment - تایید/رد نظر
```

#### Controller: `BlogController`
**Location:** `EndPoint.WebSite/Areas/Admin/Controllers/BlogController.cs`

**Actions:**
- `Index([FromQuery] BlogIndexRequest? request)` - لیست وبلاگ‌ها
- `Create()` - فرم ایجاد
- `Create(BlogFormViewModel model)` - ثبت
- `Edit(Guid id)` - فرم ویرایش
- `Edit(Guid id, BlogFormViewModel model)` - به‌روزرسانی
- `Delete(Guid id)` - حذف
- `Comments(Guid id)` - نظرات وبلاگ
- `ModerateComment(Guid id, Guid commentId, bool approve)` - تایید/رد نظر
- `UploadContentImage(IFormFile? file)` - آپلود تصویر در محتوا

**ViewModels:**
- `BlogIndexViewModel` - شامل: Blogs, Statistics, Filters, CategoryOptions, AuthorOptions, StatusOptions, Pagination
- `BlogFormViewModel` - شامل تمام فیلدهای وبلاگ
- `BlogCommentListViewModel` - شامل: BlogId, BlogTitle, Comments, TotalCount, ApprovedCount, PendingCount

**فیلترها:**
- Search
- CategoryId
- AuthorId
- Status (Published, Draft, Trash)
- FromDate/ToDate

**ویژگی‌ها:**
- Title (عنوان)
- Summary (خلاصه)
- Content (محتوا - HTML با Rich Text Editor)
- CategoryId (دسته‌بندی)
- AuthorId (نویسنده)
- Status (Published, Draft, Trash)
- ReadingTimeMinutes (زمان مطالعه)
- PublishedAt (تاریخ انتشار - شمسی)
- SEO: Title, Description, Keywords, Slug, Robots
- FeaturedImage (تصویر شاخص - حداکثر 5MB)
- Tags (برچسب‌ها)
- آمار: LikeCount, DislikeCount, CommentCount, ViewCount
- Upload Content Images (آپلود تصویر در محتوا)

**Views:**
- `Index.cshtml` - لیست وبلاگ‌ها
- `Form.cshtml` - فرم ایجاد/ویرایش
- `Comments.cshtml` - نظرات وبلاگ

**Commands/Queries:**
- `GetBlogListQuery` - لیست وبلاگ‌ها
- `GetBlogDetailQuery` - جزئیات وبلاگ
- `GetBlogLookupsQuery` - دسته‌بندی‌ها و نویسندگان
- `GetBlogCommentsQuery` - نظرات وبلاگ
- `CreateBlogCommand` - ایجاد
- `UpdateBlogCommand` - ویرایش
- `DeleteBlogCommand` - حذف
- `SetBlogCommentApprovalCommand` - تایید/رد نظر

---

### 2. دسته‌بندی‌های وبلاگ

#### مسیرها
```
GET  /Admin/Blog/Categories           - مدیریت دسته‌بندی‌های وبلاگ
POST /Admin/Blog/CreateCategory       - ایجاد دسته‌بندی
POST /Admin/Blog/UpdateCategory       - ویرایش دسته‌بندی
POST /Admin/Blog/DeleteCategory/{id}  - حذف دسته‌بندی
```

#### Controller: `BlogController` - Actions: `Categories`, `CreateCategory`, `UpdateCategory`, `DeleteCategory`

**ویژگی‌ها:**
- ساختار درختی (Tree Structure)
- Name, Slug, Description
- ParentId (والد)
- Depth (عمق)

**Views:**
- `Categories.cshtml` - نمایش درخت دسته‌بندی‌ها

**Commands:**
- `CreateBlogCategoryCommand`
- `UpdateBlogCategoryCommand`
- `DeleteBlogCategoryCommand`

---

### 3. نویسندگان

#### مسیرها
```
GET  /Admin/Blog/Authors              - لیست نویسندگان
POST /Admin/Blog/CreateAuthor        - ایجاد نویسنده
POST /Admin/Blog/UpdateAuthor        - ویرایش نویسنده
POST /Admin/Blog/DeleteAuthor/{id}   - حذف نویسنده
```

#### Controller: `BlogController` - Actions: `Authors`, `CreateAuthor`, `UpdateAuthor`, `DeleteAuthor`

**ViewModels:**
- `BlogAuthorsViewModel` - شامل: Authors, UserOptions, TotalCount, ActiveCount, InactiveCount
- `BlogAuthorFormModel` - شامل: DisplayName, Bio, AvatarUrl, IsActive, UserId

**ویژگی‌ها:**
- DisplayName (نام نمایشی)
- Bio (بیوگرافی)
- AvatarUrl (آواتار)
- IsActive (فعال/غیرفعال)
- UserId (اتصال به ApplicationUser - اختیاری)
- نمایش اطلاعات کاربر مرتبط (Email, Phone)

**Views:**
- `Authors.cshtml` - لیست نویسندگان

**Commands:**
- `GetBlogAuthorsQuery`
- `CreateBlogAuthorCommand`
- `UpdateBlogAuthorCommand`
- `DeleteBlogAuthorCommand`

---

## ⚙️ قسمت تنظیمات

### 1. تنظیمات سایت

#### مسیرها
```
GET  /Admin/SiteSettings      - نمایش تنظیمات
POST /Admin/SiteSettings      - ذخیره تنظیمات
```

#### Controller: `SiteSettingsController`
**Location:** `EndPoint.WebSite/Areas/Admin/Controllers/SiteSettingsController.cs`

**Actions:**
- `Index()` - نمایش تنظیمات
- `Index(SiteSettingsViewModel model)` - ذخیره تنظیمات

**ViewModels:**
- `SiteSettingsViewModel` - شامل تمام تنظیمات سایت

**ویژگی‌ها:**
- SiteName (نام سایت)
- SiteDescription (توضیحات)
- ContactEmail (ایمیل تماس)
- ContactPhone (تلفن تماس)
- Address (آدرس)
- SocialMedia (شبکه‌های اجتماعی)
- Logo (لوگو)
- Favicon (فاوآیکون)
- و سایر تنظیمات عمومی

**Views:**
- `Index.cshtml` - فرم تنظیمات

**Commands/Queries:**
- `GetSiteSettingsQuery`
- `SaveSiteSettingsCommand`

---

### 2. مدیریت منو

#### مسیرها
```
GET  /Admin/NavigationMenu           - مدیریت منو
POST /Admin/NavigationMenu/Create    - ایجاد آیتم منو
POST /Admin/NavigationMenu/Edit      - ویرایش آیتم منو
POST /Admin/NavigationMenu/Delete/{id} - حذف آیتم منو
```

#### Controller: `NavigationMenuController`
**Location:** `EndPoint.WebSite/Areas/Admin/Controllers/NavigationMenuController.cs`

**Actions:**
- `Index(Guid? id)` - نمایش درخت منو + فرم
- `Create(NavigationMenuItemFormViewModel form)` - ایجاد آیتم
- `Edit(NavigationMenuItemFormViewModel form)` - ویرایش آیتم
- `Delete(Guid id)` - حذف آیتم

**ViewModels:**
- `NavigationMenuPageViewModel` - شامل: Items (tree), Form, ParentOptions
- `NavigationMenuItemFormViewModel` - شامل: Id, Title, Url, Icon, DisplayOrder, ParentId, IsActive, Target (blank/self)

**ویژگی‌ها:**
- ساختار درختی (Tree Structure)
- Title (عنوان)
- Url (لینک)
- Icon (آیکون)
- DisplayOrder (ترتیب نمایش)
- ParentId (والد)
- IsActive (فعال/غیرفعال)
- Target (_blank یا _self)

**Views:**
- `Index.cshtml` - نمایش درخت منو + فرم

**Commands/Queries:**
- `GetNavigationMenuTreeQuery`
- `GetNavigationMenuItemQuery`
- `CreateNavigationMenuItemCommand`
- `UpdateNavigationMenuItemCommand`
- `DeleteNavigationMenuItemCommand`

---

### 3. تنظیمات مالی

#### مسیرها
```
GET  /Admin/FinancialSettings      - نمایش تنظیمات مالی
POST /Admin/FinancialSettings      - ذخیره تنظیمات
```

#### Controller: `FinancialSettingsController`
**Location:** `EndPoint.WebSite/Areas/Admin/Controllers/FinancialSettingsController.cs`

**Actions:**
- `Index()` - نمایش تنظیمات
- `Index(FinancialSettingsViewModel model)` - ذخیره تنظیمات

**ViewModels:**
- `FinancialSettingsViewModel` - شامل تمام تنظیمات مالی

**ویژگی‌ها:**
- SellerCommissionPercent (درصد کمیسیون فروشنده)
- TaxRatePercent (نرخ مالیات)
- PlatformFeePercent (کارمزد پلتفرم)
- MinimumWithdrawalAmount (حداقل مبلغ برداشت)
- PaymentGatewaySettings (تنظیمات درگاه پرداخت)
- CurrencySettings (تنظیمات ارز)

**Views:**
- `Index.cshtml` - فرم تنظیمات مالی

**Commands/Queries:**
- `GetFinancialSettingsQuery`
- `SaveFinancialSettingsCommand`

---

## 👤 پنل کاربری

#### مسیرها
```
GET  /User/Profile           - پروفایل کاربر
GET  /User/Products         - محصولات خریداری شده
GET  /User/Invoice          - فاکتورهای من
GET  /User/Wallet           - کیف پول
GET  /User/Test             - آزمون‌های من
```

### 1. پروفایل کاربر

#### Controller: `ProfileController`
**Location:** `EndPoint.WebSite/Areas/User/Controllers/ProfileController.cs`

**Actions:**
- `Index()` - نمایش پروفایل
- `Index(UserSettingsViewModel model)` - به‌روزرسانی پروفایل

**ویژگی‌ها:**
- نمایش اطلاعات کاربر
- ویرایش FullName, Email, PhoneNumber
- آپلود آواتار
- تغییر رمز عبور

**Views:**
- `Index.cshtml` - پروفایل کاربر

---

### 2. محصولات خریداری شده

#### Controller: `ProductsController`
**Location:** `EndPoint.WebSite/Areas/User/Controllers/ProductsController.cs`

**Actions:**
- `Index()` - لیست محصولات خریداری شده

**ویژگی‌ها:**
- نمایش محصولات خریداری شده
- لینک دانلود برای محصولات Digital
- تاریخ خرید
- وضعیت دسترسی

**Views:**
- `Index.cshtml` - لیست محصولات

---

### 3. فاکتورهای من

#### Controller: `InvoiceController`
**Location:** `EndPoint.WebSite/Areas/User/Controllers/InvoiceController.cs`

**Actions:**
- `Index()` - لیست فاکتورها
- `Details(Guid id)` - جزئیات فاکتور

**ویژگی‌ها:**
- نمایش فاکتورهای کاربر
- جزئیات کامل فاکتور
- تراکنش‌های پرداخت
- دانلود PDF

**Views:**
- `Index.cshtml` - لیست فاکتورها
- `Details.cshtml` - جزئیات فاکتور

---

### 4. کیف پول

#### Controller: `WalletController`
**Location:** `EndPoint.WebSite/Areas/User/Controllers/WalletController.cs`

**Actions:**
- `Index()` - داشبورد کیف پول
- `Charge(ChargeWalletInputModel model)` - شارژ کیف پول
- `PayInvoice(Guid id)` - انتخاب روش پرداخت فاکتور
- `PayInvoice(Guid invoiceId, PaymentMethod method)` - پرداخت فاکتور
- `ConfirmBankPayment(Guid invoiceId, string reference)` - تایید پرداخت بانکی
- `InvoiceDetails(Guid id)` - جزئیات فاکتور

**ویژگی‌ها:**
- نمایش موجودی کیف پول
- شارژ کیف پول
- لیست تراکنش‌ها
- لیست فاکتورها
- پرداخت فاکتور از کیف پول
- پرداخت از طریق درگاه بانکی
- نمایش سبد خرید

**Views:**
- `Index.cshtml` - داشبورد کیف پول
- `PayInvoice.cshtml` - انتخاب روش پرداخت
- `BankPaymentSession.cshtml` - اتصال به درگاه
- `InvoiceDetails.cshtml` - جزئیات فاکتور

---

## 🏪 پنل فروشنده (Seller Panel)

**نکته:** در پروژه فعلی "Teacher" است، در پروژه جدید باید "Seller" باشد.

#### مسیرها
```
GET  /Seller/Products           - محصولات من
GET  /Seller/Products/Create    - درخواست افزودن محصول
POST /Seller/Products/Create    - ثبت درخواست
GET  /Seller/Products/Edit/{id} - ویرایش محصول
POST /Seller/Products/Edit/{id} - به‌روزرسانی
GET  /Seller/Products/Details/{id} - جزئیات محصول
```

#### Controller: `ProductsController`
**Location:** `EndPoint.WebSite/Areas/Teacher/Controllers/ProductsController.cs`

**Actions:**
- `Index([FromQuery] TeacherProductFilterRequest? filters)` - لیست محصولات فروشنده
- `Create()` - فرم درخواست افزودن محصول
- `Create(TeacherProductFormViewModel model)` - ثبت درخواست
- `Edit(Guid id)` - فرم ویرایش
- `Edit(Guid id, TeacherProductFormViewModel model)` - به‌روزرسانی
- `Details(Guid id)` - جزئیات محصول با آمار فروش

**ویژگی‌ها:**
- فقط محصولات خود فروشنده
- درخواست افزودن محصول (نیاز به تایید Admin)
- ویرایش محصول (نیاز به تایید مجدد Admin)
- مشاهده آمار فروش
- فیلتر بر اساس Type و Status

**Views:**
- `Index.cshtml` - لیست محصولات
- `Create.cshtml` - فرم ایجاد
- `Edit.cshtml` - فرم ویرایش
- `Details.cshtml` - جزئیات با آمار

**Commands:**
- `GetTeacherProductsQuery` - محصولات فروشنده
- `GetTeacherProductDetailQuery` - جزئیات
- `GetProductSalesSummaryQuery` - آمار فروش
- `SubmitTeacherProductCommand` - ثبت درخواست
- `UpdateTeacherProductCommand` - به‌روزرسانی

---

## 🌐 صفحات اصلی سایت

### 1. صفحه اصلی (Home)

#### Controller: `HomeController`
**Location:** `EndPoint.WebSite/Controllers/HomeController.cs`

**Actions:**
- `Index()` - صفحه اصلی

**ویژگی‌ها:**
- نمایش آخرین پست‌های وبلاگ (4 مورد)
- نمایش محصولات ویژه (6 مورد)
- Hero Section
- Call-to-Action

**Views:**
- `Index.cshtml` - صفحه اصلی

---

### 2. لیست محصولات

#### Controller: `ProductController`
**Location:** `EndPoint.WebSite/Controllers/ProductController.cs`

**Actions:**
- `Index(string? search, string? category, string? format, decimal? minPrice, decimal? maxPrice, double? rating, string? sort)` - لیست محصولات
- `Details(string slug)` - جزئیات محصول
- `AddComment(string slug, ProductCommentFormModel form)` - افزودن نظر

**ویژگی‌ها:**
- فیلتر: Search, Category, DeliveryFormat, Price Range, Rating
- Sort: Newest, Price (Asc/Desc), Rating
- نمایش محصولات با Card Layout
- جزئیات محصول:
  - تصاویر (Hero + Gallery)
  - توضیحات کامل
  - Modules (ماژول‌ها)
  - Statistics (آمار)
  - FAQs
  - Comments (نظرات با Rating)
  - Related Products
- افزودن نظر با Rating

**Views:**
- `Index.cshtml` - لیست محصولات
- `Details.cshtml` - جزئیات محصول
- `_ProductComment.cshtml` - Partial View نظرات

---

### 3. وبلاگ

#### Controller: `BlogController`
**Location:** `EndPoint.WebSite/Controllers/BlogController.cs`

**Actions:**
- `Index()` - لیست پست‌های وبلاگ
- `Details(string slug)` - جزئیات پست
- `AddComment(string slug, BlogCommentFormModel form)` - افزودن نظر

**ویژگی‌ها:**
- لیست پست‌ها با Card Layout
- فیلتر بر اساس Category
- جزئیات پست:
  - محتوای کامل
  - نویسنده
  - تاریخ انتشار
  - زمان مطالعه
  - Tags
  - نظرات (Threaded Comments)
- افزودن نظر

**Views:**
- `Index.cshtml` - لیست پست‌ها
- `Details.cshtml` - جزئیات پست
- `_CommentThread.cshtml` - Partial View نظرات

---

### 4. سبد خرید

#### Controller: `CartController`
**Location:** `EndPoint.WebSite/Controllers/CartController.cs`

**Actions:**
- `Index()` - نمایش سبد خرید
- `Add(Guid productId, int quantity)` - افزودن به سبد
- `Update(Guid productId, int quantity)` - به‌روزرسانی تعداد
- `Remove(Guid productId)` - حذف از سبد
- `Clear()` - خالی کردن سبد

**ویژگی‌ها:**
- مدیریت سبد خرید با Cookie
- نمایش آیتم‌ها
- محاسبه جمع کل
- اعمال کد تخفیف
- لینک به Checkout

**Views:**
- `Index.cshtml` - سبد خرید

**Services:**
- `ICartCookieService` - مدیریت سبد با Cookie

---

### 5. تسویه حساب (Checkout)

#### Controller: `CheckoutController`
**Location:** `EndPoint.WebSite/Controllers/CheckoutController.cs`

**Actions:**
- `Index()` - صفحه تسویه حساب
- `Process()` - پردازش سفارش

**ویژگی‌ها:**
- نمایش آیتم‌های سبد
- انتخاب روش پرداخت
- اعمال کد تخفیف
- ایجاد فاکتور
- پرداخت از کیف پول یا درگاه بانکی

**Views:**
- `Index.cshtml` - تسویه حساب

---

## 🎨 ساختار UI و Layout

### Layout اصلی سایت
**File:** `EndPoint.WebSite/Views/Shared/_Layout.cshtml`

**ویژگی‌ها:**
- Navbar با منوی اصلی
- Mobile Menu
- Footer
- Scroll to Top Button
- Alert Modal
- Cart Preview Component

**منوی اصلی:**
- خانه
- محصولات
- آزمون‌ها
- سبد خرید
- بلاگ
- ورود/ثبت نام (یا منوی کاربر)

---

### Layout پنل ادمین
**File:** `EndPoint.WebSite/Areas/Admin/Views/Shared/_AdminLayout.cshtml`

**ویژگی‌ها:**
- Sidebar با منوی Admin
- Header با جستجو و پروفایل
- Content Area
- Responsive Design
- RTL Support

**Sidebar Menu:**
- Dashboard
- مدیریت کاربران
- سطوح دسترسی
- مجوزها
- دسترسی صفحات
- مدیریت فروشندگان
- محصولات
- کدهای تخفیف
- فاکتورها
- کیف پول
- وبلاگ
- تنظیمات سایت
- مدیریت منو
- تنظیمات مالی

**Component:**
- `AdminSidebar` - ViewComponent برای Sidebar

---

### Layout پنل کاربری
**File:** `EndPoint.WebSite/Areas/User/Views/Shared/_UserLayout.cshtml`

**ویژگی‌ها:**
- Sidebar با منوی کاربر
- Header
- Content Area

**Sidebar Menu:**
- پروفایل
- محصولات من
- فاکتورها
- کیف پول
- آزمون‌های من

**Component:**
- `UserSidebar` - ViewComponent

---

### Layout پنل فروشنده
**File:** `EndPoint.WebSite/Areas/Teacher/Views/Shared/_TeacherLayout.cshtml`

**ویژگی‌ها:**
- Sidebar با منوی فروشنده
- Header
- Content Area

**Sidebar Menu:**
- محصولات من
- آمار فروش

**Component:**
- `TeacherSidebar` - ViewComponent

---

## 🔐 سیستم احراز هویت و دسترسی

### Authentication
- استفاده از ASP.NET Core Identity
- Phone-based Login (ورود با شماره تلفن)
- OTP Verification
- Cookie Authentication

### Authorization
- Role-based (RBAC)
- Permission-based
- Policy-based
- Admin Bypass (کاربران Admin به همه چیز دسترسی دارند)

### Permission System
- `PermissionCatalog` - کاتالوگ مجوزها
- `PermissionAuthorizationHandler` - Handler برای بررسی مجوز
- `RequirePermissionAttribute` - Attribute برای Controller/Action
- `PermissionTagHelper` - Tag Helper برای UI

### Page Access
- `PageAccessPolicy` - Entity برای ذخیره مجوزهای صفحات
- `IPageAccessCache` - Cache برای دسترسی صفحات
- `AdminPagePermissionFilter` - Filter برای بررسی دسترسی صفحات Admin

---

## 📦 ساختار پروژه

```
EndPoint.WebSite/
├── Areas/
│   ├── Admin/
│   │   ├── Controllers/
│   │   ├── Models/
│   │   └── Views/
│   ├── User/
│   │   ├── Controllers/
│   │   ├── Models/
│   │   └── Views/
│   └── Teacher/ (یا Seller)
│       ├── Controllers/
│       ├── Models/
│       └── Views/
├── Controllers/
├── Models/
├── Views/
├── Services/
└── wwwroot/

src/
├── Application/
│   ├── Commands/
│   ├── Queries/
│   ├── DTOs/
│   └── Interfaces/
├── Domain/
│   └── Entities/
├── Infrastructure/
│   ├── Persistence/
│   └── Services/
└── SharedKernel/
    └── Authorization/
```

---

## 🗄️ Entity های اصلی

### Identity
- `ApplicationUser` - کاربر
- `ApplicationRole` - نقش
- `AccessPermission` - مجوز
- `PageAccessPolicy` - دسترسی صفحه

### Catalog
- `Product` - محصول
- `ProductImage` - تصویر محصول
- `ProductExecutionStep` - گام اجرایی
- `ProductFaq` - سوال متداول
- `ProductComment` - نظر محصول
- `SiteCategory` - دسته‌بندی

### Blog
- `BlogPost` - پست وبلاگ
- `BlogCategory` - دسته‌بندی وبلاگ
- `BlogAuthor` - نویسنده
- `BlogComment` - نظر وبلاگ

### Billing
- `Invoice` - فاکتور
- `InvoiceItem` - آیتم فاکتور
- `InvoiceTransaction` - تراکنش فاکتور
- `DiscountCode` - کد تخفیف
- `Wallet` - کیف پول
- `WalletTransaction` - تراکنش کیف پول

### Settings
- `SiteSettings` - تنظیمات سایت
- `NavigationMenuItem` - آیتم منو
- `FinancialSettings` - تنظیمات مالی

### Seller
- `SellerProfile` (یا `TeacherProfile`) - پروفایل فروشنده

---

## 🎯 نکات مهم پیاده‌سازی

### 1. تاریخ شمسی
- استفاده از `PersianDateTime` برای تبدیل تاریخ
- Input/Output با فرمت شمسی
- ذخیره در دیتابیس به صورت UTC

### 2. فایل‌ها
- آپلود در `wwwroot/uploads/`
- ساختار پوشه‌بندی: `users/profile/`, `products/featured/`, `blogs/content/`, etc.
- اعتبارسنجی: حجم، فرمت، ContentType

### 3. Validation
- Data Annotations
- Fluent Validation (در Application Layer)
- Client-side Validation

### 4. Error Handling
- TempData برای پیام‌های Success/Error
- ModelState برای Validation Errors
- Logging با Serilog

### 5. Caching
- `IPageAccessCache` برای دسترسی صفحات
- Memory Cache برای Lookups

### 6. Pagination
- PageSize قابل تنظیم
- نمایش FirstItemIndex و LastItemIndex
- TotalPages محاسبه

### 7. Search & Filter
- فیلتر بر اساس چند معیار
- جستجو در چند فیلد
- URL Query Parameters

### 8. Modals
- استفاده از Bootstrap Modals
- Partial Views برای Modal Content
- AJAX برای Load/Save

### 9. File Upload
- `IFormFileSettingServices` برای مدیریت فایل‌ها
- Validation قبل از Save
- Rollback در صورت خطا

### 10. PDF Generation
- استفاده از QuestPDF
- فونت فارسی (Vazirmatn)
- Template برای Invoice

---

## 📝 خلاصه Routes

### Admin Panel
```
/Admin/Users
/Admin/AccessLevels
/Admin/Permissions
/Admin/PageAccess
/Admin/Sellers (یا Teachers)
/Admin/Catalog
/Admin/Catalog/Categories
/Admin/DiscountCodes
/Admin/Invoices
/Admin/Wallets/Charge
/Admin/Blog
/Admin/Blog/Categories
/Admin/Blog/Authors
/Admin/SiteSettings
/Admin/NavigationMenu
/Admin/FinancialSettings
```

### User Panel
```
/User/Profile
/User/Products
/User/Invoice
/User/Wallet
/User/Test
```

### Seller Panel
```
/Seller/Products (یا /Teacher/Products)
```

### Main Site
```
/
/Product
/Product/{slug}
/Blog
/Blog/{slug}
/Cart
/Checkout
/Account/PhoneLogin
/Account/PhoneVerification
```

---

این مستند شامل تمام جزئیات لازم برای پیاده‌سازی مجدد سیستم است. برای هر بخش، Controller، ViewModel، View، Command/Query، و Entity مشخص شده است.

