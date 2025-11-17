using ProjectGenerator.Models;

namespace ProjectGenerator.Templates;

public class WebSiteTemplates
{
    private readonly string _namespace;
    private readonly string _projectName;

    public WebSiteTemplates(string namespaceName, string projectName)
    {
        _namespace = namespaceName;
        _projectName = projectName;
    }

    // ==================== Admin Area Controllers ====================
    
    public string GetAdminHomeControllerTemplate()
    {
        return $@"using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace {_projectName}.WebSite.Areas.Admin.Controllers;

[Area(""Admin"")]
[Authorize(Roles = ""Admin"")]
public class HomeController : Controller
{{
    public IActionResult Index()
    {{
        return View();
    }}
}}
";
    }

    public string GetAdminUsersControllerTemplate()
    {
        return $@"using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace {_projectName}.WebSite.Areas.Admin.Controllers;

[Area(""Admin"")]
[Authorize(Roles = ""Admin"")]
public class UsersController : Controller
{{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<ApplicationRole> _roleManager;

    public UsersController(
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager)
    {{
        _userManager = userManager;
        _roleManager = roleManager;
    }}

    public async Task<IActionResult> Index()
    {{
        var users = await _userManager.Users.ToListAsync();
        return View(users);
    }}

    public IActionResult Create()
    {{
        return View();
    }}

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateUserViewModel model)
    {{
        if (ModelState.IsValid)
        {{
            var user = new ApplicationUser
            {{
                UserName = model.Username,
                Email = model.Email,
                PhoneNumber = model.PhoneNumber
            }};

            var result = await _userManager.CreateAsync(user, model.Password);
            
            if (result.Succeeded)
            {{
                TempData[""SuccessMessage""] = ""کاربر با موفقیت ایجاد شد"";
                return RedirectToAction(nameof(Index));
            }}

            foreach (var error in result.Errors)
            {{
                ModelState.AddModelError(string.Empty, error.Description);
            }}
        }}

        return View(model);
    }}

    public async Task<IActionResult> Edit(int id)
    {{
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user == null)
        {{
            return NotFound();
        }}

        var model = new EditUserViewModel
        {{
            Id = user.Id,
            Username = user.UserName ?? """",
            Email = user.Email ?? """",
            PhoneNumber = user.PhoneNumber ?? """"
        }};

        return View(model);
    }}

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(EditUserViewModel model)
    {{
        if (ModelState.IsValid)
        {{
            var user = await _userManager.FindByIdAsync(model.Id.ToString());
            if (user == null)
            {{
                return NotFound();
            }}

            user.Email = model.Email;
            user.PhoneNumber = model.PhoneNumber;

            var result = await _userManager.UpdateAsync(user);
            
            if (result.Succeeded)
            {{
                TempData[""SuccessMessage""] = ""کاربر با موفقیت ویرایش شد"";
                return RedirectToAction(nameof(Index));
            }}

            foreach (var error in result.Errors)
            {{
                ModelState.AddModelError(string.Empty, error.Description);
            }}
        }}

        return View(model);
    }}

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {{
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user == null)
        {{
            return NotFound();
        }}

        var result = await _userManager.DeleteAsync(user);
        
        if (result.Succeeded)
        {{
            TempData[""SuccessMessage""] = ""کاربر با موفقیت حذف شد"";
        }}
        else
        {{
            TempData[""ErrorMessage""] = ""خطا در حذف کاربر"";
        }}

        return RedirectToAction(nameof(Index));
    }}
}}

public class CreateUserViewModel
{{
    public string Username {{ get; set; }} = string.Empty;
    public string Email {{ get; set; }} = string.Empty;
    public string PhoneNumber {{ get; set; }} = string.Empty;
    public string Password {{ get; set; }} = string.Empty;
}}

public class EditUserViewModel
{{
    public int Id {{ get; set; }}
    public string Username {{ get; set; }} = string.Empty;
    public string Email {{ get; set; }} = string.Empty;
    public string PhoneNumber {{ get; set; }} = string.Empty;
}}

public class ApplicationUser : Microsoft.AspNetCore.Identity.IdentityUser<int> {{ }}
public class ApplicationRole : Microsoft.AspNetCore.Identity.IdentityRole<int> {{ }}
";
    }

    public string GetAdminRolesControllerTemplate()
    {
        return $@"using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace {_projectName}.WebSite.Areas.Admin.Controllers;

[Area(""Admin"")]
[Authorize(Roles = ""Admin"")]
public class RolesController : Controller
{{
    private readonly RoleManager<ApplicationRole> _roleManager;

    public RolesController(RoleManager<ApplicationRole> roleManager)
    {{
        _roleManager = roleManager;
    }}

    public async Task<IActionResult> Index()
    {{
        var roles = await _roleManager.Roles.ToListAsync();
        return View(roles);
    }}

    public IActionResult Create()
    {{
        return View();
    }}

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateRoleViewModel model)
    {{
        if (ModelState.IsValid)
        {{
            var role = new ApplicationRole {{ Name = model.Name }};
            var result = await _roleManager.CreateAsync(role);
            
            if (result.Succeeded)
            {{
                TempData[""SuccessMessage""] = ""نقش با موفقیت ایجاد شد"";
                return RedirectToAction(nameof(Index));
            }}

            foreach (var error in result.Errors)
            {{
                ModelState.AddModelError(string.Empty, error.Description);
            }}
        }}

        return View(model);
    }}
}}

public class CreateRoleViewModel
{{
    public string Name {{ get; set; }} = string.Empty;
}}
";
    }

    public string GetAdminAccessLevelsControllerTemplate()
    {
        return $@"using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using {_namespace}.Domain.Entities;
using {_namespace}.Infrastructure.Persistence;

namespace {_projectName}.WebSite.Areas.Admin.Controllers;

[Area(""Admin"")]
[Authorize(Roles = ""Admin"")]
public class AccessLevelsController : Controller
{{
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly ApplicationDbContext _context;

    public AccessLevelsController(
        RoleManager<IdentityRole> roleManager,
        ApplicationDbContext context)
    {{
        _roleManager = roleManager;
        _context = context;
    }}

    public async Task<IActionResult> Index()
    {{
        var roles = await _roleManager.Roles.ToListAsync();
        return View(roles);
    }}

    public async Task<IActionResult> Permissions(string roleId)
    {{
        var role = await _roleManager.FindByIdAsync(roleId);
        if (role == null)
        {{
            return NotFound();
        }}

        var allPermissions = await _context.AccessPermissions.ToListAsync();
        // Get role claims (permissions)
        var roleClaims = await _context.RoleClaims
            .Where(rc => rc.RoleId == roleId && rc.ClaimType == ""Permission"")
            .Select(rc => rc.ClaimValue)
            .ToListAsync();

        ViewBag.RoleName = role.Name;
        ViewBag.RoleId = roleId;
        ViewBag.AssignedPermissions = roleClaims;
        
        return View(allPermissions);
    }}

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AssignPermissions(string roleId, List<string> permissionKeys)
    {{
        var role = await _roleManager.FindByIdAsync(roleId);
        if (role == null)
        {{
            return NotFound();
        }}

        // Remove existing permission claims
        var existingClaims = await _context.RoleClaims
            .Where(rc => rc.RoleId == roleId && rc.ClaimType == ""Permission"")
            .ToListAsync();
        
        _context.RoleClaims.RemoveRange(existingClaims);

        // Add new claims
        foreach (var permissionKey in permissionKeys ?? new List<string>())
        {{
            _context.RoleClaims.Add(new IdentityRoleClaim<string>
            {{
                RoleId = roleId,
                ClaimType = ""Permission"",
                ClaimValue = permissionKey
            }});
        }}

        await _context.SaveChangesAsync();

        TempData[""SuccessMessage""] = ""مجوزها با موفقیت تخصیص داده شد"";
        return RedirectToAction(nameof(Index));
    }}
}}
";
    }

    public string GetAdminPermissionsControllerTemplate()
    {
        return $@"using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using {_namespace}.Domain.Entities;
using {_namespace}.Infrastructure.Persistence;

namespace {_projectName}.WebSite.Areas.Admin.Controllers;

[Area(""Admin"")]
[Authorize(Roles = ""Admin"")]
public class PermissionsController : Controller
{{
    private readonly ApplicationDbContext _context;

    public PermissionsController(ApplicationDbContext context)
    {{
        _context = context;
    }}

    public async Task<IActionResult> Index()
    {{
        var permissions = await _context.AccessPermissions
            .OrderBy(p => p.GroupKey)
            .ThenBy(p => p.DisplayName)
            .ToListAsync();
        
        return View(permissions);
    }}

    public IActionResult Create()
    {{
        return View();
    }}

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreatePermissionViewModel model)
    {{
        if (ModelState.IsValid)
        {{
            var exists = await _context.AccessPermissions
                .AnyAsync(p => p.Key == model.Key);
            
            if (exists)
            {{
                ModelState.AddModelError(nameof(model.Key), ""این کلید قبلاً استفاده شده است"");
                return View(model);
            }}

            var permission = new AccessPermission(
                model.Key,
                model.DisplayName,
                model.Description,
                model.IsCore,
                model.GroupKey ?? ""custom"",
                model.GroupDisplayName ?? ""سفارشی"");

            _context.AccessPermissions.Add(permission);
            await _context.SaveChangesAsync();

            TempData[""SuccessMessage""] = ""مجوز با موفقیت ایجاد شد"";
            return RedirectToAction(nameof(Index));
        }}

        return View(model);
    }}

    public async Task<IActionResult> Edit(Guid id)
    {{
        var permission = await _context.AccessPermissions.FindAsync(id);
        if (permission == null)
        {{
            return NotFound();
        }}

        if (permission.IsCore)
        {{
            TempData[""ErrorMessage""] = ""نمی‌توانید مجوزهای اصلی را ویرایش کنید"";
            return RedirectToAction(nameof(Index));
        }}

        var model = new EditPermissionViewModel
        {{
            Id = permission.Id,
            Key = permission.Key,
            DisplayName = permission.DisplayName,
            Description = permission.Description,
            GroupKey = permission.GroupKey,
            GroupDisplayName = permission.GroupDisplayName
        }};

        return View(model);
    }}

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(EditPermissionViewModel model)
    {{
        if (ModelState.IsValid)
        {{
            var permission = await _context.AccessPermissions.FindAsync(model.Id);
            if (permission == null)
            {{
                return NotFound();
            }}

            if (permission.IsCore)
            {{
                TempData[""ErrorMessage""] = ""نمی‌توانید مجوزهای اصلی را ویرایش کنید"";
                return RedirectToAction(nameof(Index));
            }}

            permission.UpdateDetails(
                model.DisplayName,
                model.Description,
                false,
                model.GroupKey ?? ""custom"",
                model.GroupDisplayName ?? ""سفارشی"");

            await _context.SaveChangesAsync();

            TempData[""SuccessMessage""] = ""مجوز با موفقیت به‌روز شد"";
            return RedirectToAction(nameof(Index));
        }}

        return View(model);
    }}

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id)
    {{
        var permission = await _context.AccessPermissions.FindAsync(id);
        if (permission == null)
        {{
            return NotFound();
        }}

        if (permission.IsCore)
        {{
            TempData[""ErrorMessage""] = ""نمی‌توانید مجوزهای اصلی را حذف کنید"";
            return RedirectToAction(nameof(Index));
        }}

        _context.AccessPermissions.Remove(permission);
        await _context.SaveChangesAsync();

        TempData[""SuccessMessage""] = ""مجوز با موفقیت حذف شد"";
        return RedirectToAction(nameof(Index));
    }}
}}

public class CreatePermissionViewModel
{{
    public string Key {{ get; set; }} = string.Empty;
    public string DisplayName {{ get; set; }} = string.Empty;
    public string? Description {{ get; set; }}
    public bool IsCore {{ get; set; }}
    public string? GroupKey {{ get; set; }}
    public string? GroupDisplayName {{ get; set; }}
}}

public class EditPermissionViewModel
{{
    public Guid Id {{ get; set; }}
    public string Key {{ get; set; }} = string.Empty;
    public string DisplayName {{ get; set; }} = string.Empty;
    public string? Description {{ get; set; }}
    public string? GroupKey {{ get; set; }}
    public string? GroupDisplayName {{ get; set; }}
}}
";
    }

    public string GetAdminPageAccessControllerTemplate()
    {
        return $@"using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using {_namespace}.Domain.Entities;
using {_namespace}.Infrastructure.Persistence;

namespace {_projectName}.WebSite.Areas.Admin.Controllers;

[Area(""Admin"")]
[Authorize(Roles = ""Admin"")]
public class PageAccessController : Controller
{{
    private readonly ApplicationDbContext _context;

    public PageAccessController(ApplicationDbContext context)
    {{
        _context = context;
    }}

    public async Task<IActionResult> Index()
    {{
        var policies = await _context.PageAccessPolicies
            .OrderBy(p => p.Area)
            .ThenBy(p => p.Controller)
            .ThenBy(p => p.Action)
            .ToListAsync();
        
        return View(policies);
    }}

    public async Task<IActionResult> Create()
    {{
        ViewBag.Permissions = await _context.AccessPermissions.ToListAsync();
        return View();
    }}

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreatePageAccessViewModel model)
    {{
        if (ModelState.IsValid)
        {{
            var exists = await _context.PageAccessPolicies
                .AnyAsync(p => 
                    p.Area == (model.Area ?? string.Empty) &&
                    p.Controller == model.Controller &&
                    p.Action == model.Action);
            
            if (exists)
            {{
                ModelState.AddModelError(string.Empty, ""این مسیر قبلاً تعریف شده است"");
                ViewBag.Permissions = await _context.AccessPermissions.ToListAsync();
                return View(model);
            }}

            var policy = new PageAccessPolicy(
                model.Area ?? string.Empty,
                model.Controller,
                model.Action,
                model.PermissionKey);

            _context.PageAccessPolicies.Add(policy);
            await _context.SaveChangesAsync();

            TempData[""SuccessMessage""] = ""سیاست دسترسی با موفقیت ایجاد شد"";
            return RedirectToAction(nameof(Index));
        }}

        ViewBag.Permissions = await _context.AccessPermissions.ToListAsync();
        return View(model);
    }}

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id)
    {{
        var policy = await _context.PageAccessPolicies.FindAsync(id);
        if (policy == null)
        {{
            return NotFound();
        }}

        _context.PageAccessPolicies.Remove(policy);
        await _context.SaveChangesAsync();

        TempData[""SuccessMessage""] = ""سیاست دسترسی با موفقیت حذف شد"";
        return RedirectToAction(nameof(Index));
    }}
}}

public class CreatePageAccessViewModel
{{
    public string? Area {{ get; set; }}
    public string Controller {{ get; set; }} = string.Empty;
    public string Action {{ get; set; }} = string.Empty;
    public string PermissionKey {{ get; set; }} = string.Empty;
}}
";
    }

    public string GetAdminSellersControllerTemplate()
    {
        return $@"using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using {_namespace}.Domain.Entities;
using {_namespace}.Infrastructure.Persistence;
using {_namespace}.Application.Services;

namespace {_projectName}.WebSite.Areas.Admin.Controllers;

[Area(""Admin"")]
[Authorize(Roles = ""Admin"")]
public class SellersController : Controller
{{
    private readonly ApplicationDbContext _context;
    private readonly IFileService _fileService;

    public SellersController(ApplicationDbContext context, IFileService fileService)
    {{
        _context = context;
        _fileService = fileService;
    }}

    public async Task<IActionResult> Index()
    {{
        var sellers = await _context.SellerProfiles
            .Include(s => s.User)
            .OrderByDescending(s => s.CreateDate)
            .ToListAsync();
        
        return View(sellers);
    }}

    public IActionResult Create()
    {{
        return View();
    }}

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateSellerViewModel model)
    {{
        if (ModelState.IsValid)
        {{
            string? avatarPath = null;
            if (model.AvatarFile != null)
            {{
                avatarPath = await _fileService.SaveFileAsync(model.AvatarFile, ""sellers"");
            }}

            var seller = new SellerProfile(
                model.DisplayName,
                model.Degree,
                model.Specialty,
                model.Bio,
                avatarPath,
                model.ContactEmail,
                model.ContactPhone,
                model.UserId,
                model.IsActive);

            _context.SellerProfiles.Add(seller);
            await _context.SaveChangesAsync();

            TempData[""SuccessMessage""] = ""فروشنده با موفقیت ایجاد شد"";
            return RedirectToAction(nameof(Index));
        }}

        return View(model);
    }}

    public async Task<IActionResult> Edit(Guid id)
    {{
        var seller = await _context.SellerProfiles.FindAsync(id);
        if (seller == null)
        {{
            return NotFound();
        }}

        var model = new EditSellerViewModel
        {{
            Id = seller.Id,
            DisplayName = seller.DisplayName,
            Degree = seller.Degree,
            Specialty = seller.Specialty,
            Bio = seller.Bio,
            ContactEmail = seller.ContactEmail,
            ContactPhone = seller.ContactPhone,
            UserId = seller.UserId,
            IsActive = seller.IsActive,
            CurrentAvatarUrl = seller.AvatarUrl
        }};

        return View(model);
    }}

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(EditSellerViewModel model)
    {{
        if (ModelState.IsValid)
        {{
            var seller = await _context.SellerProfiles.FindAsync(model.Id);
            if (seller == null)
            {{
                return NotFound();
            }}

            string? avatarPath = seller.AvatarUrl;
            if (model.AvatarFile != null)
            {{
                if (avatarPath != null)
                {{
                    await _fileService.DeleteFileAsync(avatarPath);
                }}
                avatarPath = await _fileService.SaveFileAsync(model.AvatarFile, ""sellers"");
            }}

            seller.UpdateDisplayName(model.DisplayName);
            seller.UpdateAcademicInfo(model.Degree, model.Specialty, model.Bio);
            seller.UpdateMedia(avatarPath);
            seller.UpdateContact(model.ContactEmail, model.ContactPhone);
            seller.ConnectToUser(model.UserId);
            seller.SetActive(model.IsActive);

            await _context.SaveChangesAsync();

            TempData[""SuccessMessage""] = ""فروشنده با موفقیت به‌روز شد"";
            return RedirectToAction(nameof(Index));
        }}

        return View(model);
    }}

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id)
    {{
        var seller = await _context.SellerProfiles.FindAsync(id);
        if (seller == null)
        {{
            return NotFound();
        }}

        if (seller.AvatarUrl != null)
        {{
            await _fileService.DeleteFileAsync(seller.AvatarUrl);
        }}

        _context.SellerProfiles.Remove(seller);
        await _context.SaveChangesAsync();

        TempData[""SuccessMessage""] = ""فروشنده با موفقیت حذف شد"";
        return RedirectToAction(nameof(Index));
    }}
}}

public class CreateSellerViewModel
{{
    public string DisplayName {{ get; set; }} = string.Empty;
    public string? Degree {{ get; set; }}
    public string? Specialty {{ get; set; }}
    public string? Bio {{ get; set; }}
    public IFormFile? AvatarFile {{ get; set; }}
    public string? ContactEmail {{ get; set; }}
    public string? ContactPhone {{ get; set; }}
    public string? UserId {{ get; set; }}
    public bool IsActive {{ get; set; }} = true;
}}

public class EditSellerViewModel
{{
    public Guid Id {{ get; set; }}
    public string DisplayName {{ get; set; }} = string.Empty;
    public string? Degree {{ get; set; }}
    public string? Specialty {{ get; set; }}
    public string? Bio {{ get; set; }}
    public IFormFile? AvatarFile {{ get; set; }}
    public string? CurrentAvatarUrl {{ get; set; }}
    public string? ContactEmail {{ get; set; }}
    public string? ContactPhone {{ get; set; }}
    public string? UserId {{ get; set; }}
    public bool IsActive {{ get; set; }}
}}
";
    }

    public string GetAdminProductsControllerTemplate()
    {
        return $@"using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using {_namespace}.Application.Interfaces;
using {_namespace}.Application.DTOs.Product;

namespace {_projectName}.WebSite.Areas.Admin.Controllers;

[Area(""Admin"")]
[Authorize(Roles = ""Admin"")]
public class ProductsController : Controller
{{
    private readonly IProductService _productService;

    public ProductsController(IProductService productService)
    {{
        _productService = productService;
    }}

    public async Task<IActionResult> Index()
    {{
        var result = await _productService.GetAllAsync();
        return View(result.Data);
    }}

    public IActionResult Create()
    {{
        return View();
    }}

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateProductDto model)
    {{
        if (ModelState.IsValid)
        {{
            var result = await _productService.CreateAsync(model);
            
            if (result.IsSuccess)
            {{
                TempData[""SuccessMessage""] = ""محصول با موفقیت ایجاد شد"";
                return RedirectToAction(nameof(Index));
            }}

            ModelState.AddModelError(string.Empty, result.Message);
        }}

        return View(model);
    }}

    public async Task<IActionResult> Edit(int id)
    {{
        var result = await _productService.GetByIdAsync(id);
        if (!result.IsSuccess)
        {{
            return NotFound();
        }}

        return View(result.Data);
    }}

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, UpdateProductDto model)
    {{
        if (ModelState.IsValid)
        {{
            var result = await _productService.UpdateAsync(id, model);
            
            if (result.IsSuccess)
            {{
                TempData[""SuccessMessage""] = ""محصول با موفقیت ویرایش شد"";
                return RedirectToAction(nameof(Index));
            }}

            ModelState.AddModelError(string.Empty, result.Message);
        }}

        return View(model);
    }}

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {{
        var result = await _productService.DeleteAsync(id);
        
        if (result.IsSuccess)
        {{
            TempData[""SuccessMessage""] = ""محصول با موفقیت حذف شد"";
        }}
        else
        {{
            TempData[""ErrorMessage""] = result.Message;
        }}

        return RedirectToAction(nameof(Index));
    }}
}}
";
    }

    public string GetAdminCategoriesControllerTemplate()
    {
        return $@"using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using {_namespace}.Application.Interfaces;
using {_namespace}.Application.DTOs.Category;

namespace {_projectName}.WebSite.Areas.Admin.Controllers;

[Area(""Admin"")]
[Authorize(Roles = ""Admin"")]
public class CategoriesController : Controller
{{
    private readonly ICategoryService _categoryService;

    public CategoriesController(ICategoryService categoryService)
    {{
        _categoryService = categoryService;
    }}

    public async Task<IActionResult> Index()
    {{
        var result = await _categoryService.GetAllAsync();
        return View(result.Data);
    }}

    public IActionResult Create()
    {{
        return View();
    }}

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateCategoryDto model)
    {{
        if (ModelState.IsValid)
        {{
            var result = await _categoryService.CreateAsync(model);
            
            if (result.IsSuccess)
            {{
                TempData[""SuccessMessage""] = ""دسته‌بندی با موفقیت ایجاد شد"";
                return RedirectToAction(nameof(Index));
            }}

            ModelState.AddModelError(string.Empty, result.Message);
        }}

        return View(model);
    }}
}}
";
    }

    public string GetAdminOrdersControllerTemplate()
    {
        return $@"using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using {_namespace}.Application.Interfaces;

namespace {_projectName}.WebSite.Areas.Admin.Controllers;

[Area(""Admin"")]
[Authorize(Roles = ""Admin"")]
public class OrdersController : Controller
{{
    private readonly IOrderService _orderService;

    public OrdersController(IOrderService orderService)
    {{
        _orderService = orderService;
    }}

    public IActionResult Index()
    {{
        // Implement order listing
        return View();
    }}

    public async Task<IActionResult> Details(int id)
    {{
        var result = await _orderService.GetByIdAsync(id);
        if (!result.IsSuccess)
        {{
            return NotFound();
        }}

        return View(result.Data);
    }}
}}
";
    }

    public string GetAdminBlogsControllerTemplate()
    {
        return $@"using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using {_namespace}.Application.Interfaces;
using {_namespace}.Application.DTOs.Blog;

namespace {_projectName}.WebSite.Areas.Admin.Controllers;

[Area(""Admin"")]
[Authorize(Roles = ""Admin"")]
public class BlogsController : Controller
{{
    private readonly IBlogService _blogService;

    public BlogsController(IBlogService blogService)
    {{
        _blogService = blogService;
    }}

    public IActionResult Index()
    {{
        return View();
    }}

    public IActionResult Create()
    {{
        return View();
    }}

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateBlogDto model)
    {{
        if (ModelState.IsValid)
        {{
            var result = await _blogService.CreateAsync(model);
            
            if (result.IsSuccess)
            {{
                TempData[""SuccessMessage""] = ""پست با موفقیت ایجاد شد"";
                return RedirectToAction(nameof(Index));
            }}

            ModelState.AddModelError(string.Empty, result.Message);
        }}

        return View(model);
    }}
}}
";
    }

    // ==================== Seller Area Controllers ====================
    
    public string GetSellerHomeControllerTemplate()
    {
        return $@"using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace {_projectName}.WebSite.Areas.Seller.Controllers;

[Area(""Seller"")]
[Authorize(Roles = ""Seller"")]
public class HomeController : Controller
{{
    public IActionResult Index()
    {{
        return View();
    }}
}}
";
    }

    public string GetSellerProductsControllerTemplate()
    {
        return $@"using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using {_namespace}.Application.Interfaces;
using {_namespace}.Application.DTOs.Product;
using System.Security.Claims;

namespace {_projectName}.WebSite.Areas.Seller.Controllers;

[Area(""Seller"")]
[Authorize(Roles = ""Seller"")]
public class ProductsController : Controller
{{
    private readonly IProductService _productService;

    public ProductsController(IProductService productService)
    {{
        _productService = productService;
    }}

    public async Task<IActionResult> Index()
    {{
        var sellerId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? ""0"");
        var result = await _productService.GetBySellerIdAsync(sellerId);
        return View(result.Data);
    }}

    public IActionResult Create()
    {{
        return View();
    }}

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateProductDto model)
    {{
        if (ModelState.IsValid)
        {{
            model.SellerId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? ""0"");
            var result = await _productService.CreateAsync(model);
            
            if (result.IsSuccess)
            {{
                TempData[""SuccessMessage""] = ""محصول با موفقیت ایجاد شد"";
                return RedirectToAction(nameof(Index));
            }}

            ModelState.AddModelError(string.Empty, result.Message);
        }}

        return View(model);
    }}
}}
";
    }

    public string GetSellerOrdersControllerTemplate()
    {
        return $@"using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace {_projectName}.WebSite.Areas.Seller.Controllers;

[Area(""Seller"")]
[Authorize(Roles = ""Seller"")]
public class OrdersController : Controller
{{
    public IActionResult Index()
    {{
        return View();
    }}
}}
";
    }

    // ==================== User Area Controllers ====================
    
    public string GetUserHomeControllerTemplate()
    {
        return $@"using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace {_projectName}.WebSite.Areas.User.Controllers;

[Area(""User"")]
[Authorize]
public class HomeController : Controller
{{
    public IActionResult Index()
    {{
        return View();
    }}
}}
";
    }

    public string GetUserProfileControllerTemplate()
    {
        return $@"using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace {_projectName}.WebSite.Areas.User.Controllers;

[Area(""User"")]
[Authorize]
public class ProfileController : Controller
{{
    private readonly UserManager<ApplicationUser> _userManager;

    public ProfileController(UserManager<ApplicationUser> userManager)
    {{
        _userManager = userManager;
    }}

    public async Task<IActionResult> Index()
    {{
        var user = await _userManager.GetUserAsync(User);
        return View(user);
    }}

    public async Task<IActionResult> Edit()
    {{
        var user = await _userManager.GetUserAsync(User);
        return View(user);
    }}

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(ProfileEditViewModel model)
    {{
        if (ModelState.IsValid)
        {{
            var user = await _userManager.GetUserAsync(User);
            if (user != null)
            {{
                user.Email = model.Email;
                user.PhoneNumber = model.PhoneNumber;

                var result = await _userManager.UpdateAsync(user);
                
                if (result.Succeeded)
                {{
                    TempData[""SuccessMessage""] = ""پروفایل با موفقیت ویرایش شد"";
                    return RedirectToAction(nameof(Index));
                }}

                foreach (var error in result.Errors)
                {{
                    ModelState.AddModelError(string.Empty, error.Description);
                }}
            }}
        }}

        return View(model);
    }}
}}

public class ProfileEditViewModel
{{
    public string Email {{ get; set; }} = string.Empty;
    public string PhoneNumber {{ get; set; }} = string.Empty;
}}
";
    }

    public string GetUserOrdersControllerTemplate()
    {
        return $@"using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using {_namespace}.Application.Interfaces;
using System.Security.Claims;

namespace {_projectName}.WebSite.Areas.User.Controllers;

[Area(""User"")]
[Authorize]
public class OrdersController : Controller
{{
    private readonly IOrderService _orderService;

    public OrdersController(IOrderService orderService)
    {{
        _orderService = orderService;
    }}

    public async Task<IActionResult> Index()
    {{
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? ""0"");
        var result = await _orderService.GetByUserIdAsync(userId);
        return View(result.Data);
    }}

    public async Task<IActionResult> Details(int id)
    {{
        var result = await _orderService.GetByIdAsync(id);
        if (!result.IsSuccess)
        {{
            return NotFound();
        }}

        return View(result.Data);
    }}
}}
";
    }

    // Continue in next part...
    public string GetHomeControllerTemplate()
    {
        return $@"using Microsoft.AspNetCore.Mvc;

namespace {_projectName}.WebSite.Controllers;

public class HomeController : Controller
{{
    public IActionResult Index()
    {{
        return View();
    }}

    public IActionResult About()
    {{
        return View();
    }}

    public IActionResult Contact()
    {{
        return View();
    }}
}}
";
    }

    public string GetAccountControllerTemplate()
    {
        return $@"using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace {_projectName}.WebSite.Controllers;

public class AccountController : Controller
{{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;

    public AccountController(
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager)
    {{
        _signInManager = signInManager;
        _userManager = userManager;
    }}

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {{
        ViewData[""ReturnUrl""] = returnUrl;
        return View();
    }}

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {{
        if (ModelState.IsValid)
        {{
            var result = await _signInManager.PasswordSignInAsync(
                model.Username, 
                model.Password, 
                model.RememberMe, 
                lockoutOnFailure: false);

            if (result.Succeeded)
            {{
                return RedirectToLocal(returnUrl);
            }}

            ModelState.AddModelError(string.Empty, ""نام کاربری یا رمز عبور اشتباه است"");
        }}

        return View(model);
    }}

    [HttpGet]
    public IActionResult Register()
    {{
        return View();
    }}

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {{
        if (ModelState.IsValid)
        {{
            var user = new ApplicationUser
            {{
                UserName = model.Username,
                Email = model.Email,
                PhoneNumber = model.PhoneNumber
            }};

            var result = await _userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {{
                await _signInManager.SignInAsync(user, isPersistent: false);
                return RedirectToAction(nameof(HomeController.Index), ""Home"");
            }}

            foreach (var error in result.Errors)
            {{
                ModelState.AddModelError(string.Empty, error.Description);
            }}
        }}

        return View(model);
    }}

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {{
        await _signInManager.SignOutAsync();
        return RedirectToAction(nameof(HomeController.Index), ""Home"");
    }}

    private IActionResult RedirectToLocal(string? returnUrl)
    {{
        if (Url.IsLocalUrl(returnUrl))
        {{
            return Redirect(returnUrl);
        }}
        else
        {{
            return RedirectToAction(nameof(HomeController.Index), ""Home"");
        }}
    }}
}}

public class LoginViewModel
{{
    public string Username {{ get; set; }} = string.Empty;
    public string Password {{ get; set; }} = string.Empty;
    public bool RememberMe {{ get; set; }}
}}

public class RegisterViewModel
{{
    public string Username {{ get; set; }} = string.Empty;
    public string Email {{ get; set; }} = string.Empty;
    public string PhoneNumber {{ get; set; }} = string.Empty;
    public string Password {{ get; set; }} = string.Empty;
    public string ConfirmPassword {{ get; set; }} = string.Empty;
}}
";
    }

    public string GetProductControllerTemplate()
    {
        return $@"using Microsoft.AspNetCore.Mvc;
using {_namespace}.Application.Interfaces;

namespace {_projectName}.WebSite.Controllers;

public class ProductController : Controller
{{
    private readonly IProductService _productService;

    public ProductController(IProductService productService)
    {{
        _productService = productService;
    }}

    public async Task<IActionResult> Index(int? categoryId)
    {{
        var result = categoryId.HasValue
            ? await _productService.GetByCategoryIdAsync(categoryId.Value)
            : await _productService.GetAllAsync();

        return View(result.Data);
    }}

    public async Task<IActionResult> Details(int id)
    {{
        var result = await _productService.GetByIdAsync(id);
        if (!result.IsSuccess)
        {{
            return NotFound();
        }}

        return View(result.Data);
    }}
}}
";
    }

    public string GetCartControllerTemplate()
    {
        return $@"using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using {_namespace}.Application.Interfaces;
using System.Security.Claims;

namespace {_projectName}.WebSite.Controllers;

public class CartController : Controller
{{
    private readonly ICartService _cartService;

    public CartController(ICartService cartService)
    {{
        _cartService = cartService;
    }}

    public async Task<IActionResult> Index()
    {{
        var userId = GetCurrentUserId();
        if (userId == 0)
        {{
            return RedirectToAction(""Login"", ""Account"");
        }}

        var result = await _cartService.GetByUserIdAsync(userId);
        return View(result.Data);
    }}

    [HttpPost]
    public async Task<IActionResult> AddToCart(int productId, int quantity = 1)
    {{
        var userId = GetCurrentUserId();
        if (userId == 0)
        {{
            return RedirectToAction(""Login"", ""Account"");
        }}

        var result = await _cartService.AddItemAsync(userId, productId, quantity);
        
        if (result.IsSuccess)
        {{
            TempData[""SuccessMessage""] = ""محصول به سبد خرید اضافه شد"";
        }}

        return RedirectToAction(nameof(Index));
    }}

    [HttpPost]
    public async Task<IActionResult> RemoveFromCart(int productId)
    {{
        var userId = GetCurrentUserId();
        var result = await _cartService.RemoveItemAsync(userId, productId);
        
        return RedirectToAction(nameof(Index));
    }}

    private int GetCurrentUserId()
    {{
        return int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? ""0"");
    }}
}}
";
    }

    public string GetCheckoutControllerTemplate()
    {
        return $@"using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using {_namespace}.Application.Interfaces;
using {_namespace}.Application.DTOs.Order;
using System.Security.Claims;

namespace {_projectName}.WebSite.Controllers;

[Authorize]
public class CheckoutController : Controller
{{
    private readonly ICartService _cartService;
    private readonly IOrderService _orderService;

    public CheckoutController(
        ICartService cartService,
        IOrderService orderService)
    {{
        _cartService = cartService;
        _orderService = orderService;
    }}

    public async Task<IActionResult> Index()
    {{
        var userId = GetCurrentUserId();
        var cart = await _cartService.GetByUserIdAsync(userId);
        
        return View(cart.Data);
    }}

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PlaceOrder(CreateOrderDto model)
    {{
        if (ModelState.IsValid)
        {{
            model.UserId = GetCurrentUserId();
            var result = await _orderService.CreateAsync(model);
            
            if (result.IsSuccess)
            {{
                await _cartService.ClearCartAsync(model.UserId);
                TempData[""SuccessMessage""] = ""سفارش شما با موفقیت ثبت شد"";
                return RedirectToAction(""Details"", ""Orders"", new {{ area = ""User"", id = result.Data?.Id }});
            }}

            ModelState.AddModelError(string.Empty, result.Message);
        }}

        return View(nameof(Index));
    }}

    private int GetCurrentUserId()
    {{
        return int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? ""0"");
    }}
}}
";
    }

    public string GetBlogControllerTemplate()
    {
        return $@"using Microsoft.AspNetCore.Mvc;
using {_namespace}.Application.Interfaces;

namespace {_projectName}.WebSite.Controllers;

public class BlogController : Controller
{{
    private readonly IBlogService _blogService;

    public BlogController(IBlogService blogService)
    {{
        _blogService = blogService;
    }}

    public async Task<IActionResult> Index()
    {{
        var result = await _blogService.GetAllPublishedAsync();
        return View(result.Data);
    }}

    public async Task<IActionResult> Details(string slug)
    {{
        var result = await _blogService.GetBySlugAsync(slug);
        if (!result.IsSuccess)
        {{
            return NotFound();
        }}

        return View(result.Data);
    }}
}}
";
    }

    // View Templates will continue in next file...
    
    public string GetViewImportsTemplate()
    {
        return $@"@using {_projectName}.WebSite
@using Microsoft.AspNetCore.Identity
@addTagHelper *, Microsoft.AspNetCore.Mvc.TagHelpers
";
    }

    public string GetMainViewStartTemplate()
    {
        return @"@{
    Layout = ""_Layout"";
}
";
    }

    public string GetAdminViewStartTemplate()
    {
        return @"@{
    Layout = ""~/Views/Shared/_AdminLayout.cshtml"";
}
";
    }

    public string GetSellerViewStartTemplate()
    {
        return @"@{
    Layout = ""~/Views/Shared/_SellerLayout.cshtml"";
}
";
    }

    public string GetUserViewStartTemplate()
    {
        return @"@{
    Layout = ""~/Views/Shared/_UserLayout.cshtml"";
}
";
    }

    public string GetLayoutTemplate(ThemeSettings? theme = null)
    {
        theme ??= new ThemeSettings();
        var siteName = theme.SiteName;
        var faviconUrl = theme.FaviconUrl;
        return $@"<!DOCTYPE html>
<html lang=""fa"" dir=""rtl"">
<head>
    <meta charset=""utf-8"" />
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"" />
    <title>@ViewData[""Title""] - {siteName}</title>
    <link rel=""icon"" href=""{faviconUrl}"" />
    <link rel=""stylesheet"" href=""~/css/site.css"" asp-append-version=""true"" />
</head>
<body>
    <header>
        <nav class=""navbar navbar-expand-sm navbar-toggleable-sm navbar-light bg-white border-bottom box-shadow mb-3"">
            <div class=""container"">
                <a class=""navbar-brand"" asp-area="""" asp-controller=""Home"" asp-action=""Index"">وب‌سایت</a>
                <button class=""navbar-toggler"" type=""button"" data-bs-toggle=""collapse"" data-bs-target="".navbar-collapse"">
                    <span class=""navbar-toggler-icon""></span>
                </button>
                <div class=""navbar-collapse collapse d-sm-inline-flex justify-content-between"">
                    <ul class=""navbar-nav flex-grow-1"">
                        <li class=""nav-item"">
                            <a class=""nav-link text-dark"" asp-area="""" asp-controller=""Home"" asp-action=""Index"">خانه</a>
                        </li>
                        <li class=""nav-item"">
                            <a class=""nav-link text-dark"" asp-area="""" asp-controller=""Product"" asp-action=""Index"">محصولات</a>
                        </li>
                        <li class=""nav-item"">
                            <a class=""nav-link text-dark"" asp-area="""" asp-controller=""Blog"" asp-action=""Index"">بلاگ</a>
                        </li>
                    </ul>
                    <ul class=""navbar-nav"">
                        @if (User.Identity?.IsAuthenticated ?? false)
                        {{
                            <li class=""nav-item"">
                                <a class=""nav-link text-dark"" asp-area=""User"" asp-controller=""Home"" asp-action=""Index"">پنل کاربری</a>
                            </li>
                            <li class=""nav-item"">
                                <form method=""post"" asp-area="""" asp-controller=""Account"" asp-action=""Logout"">
                                    <button type=""submit"" class=""nav-link btn btn-link text-dark"">خروج</button>
                                </form>
                            </li>
                        }}
                        else
                        {{
                            <li class=""nav-item"">
                                <a class=""nav-link text-dark"" asp-area="""" asp-controller=""Account"" asp-action=""Login"">ورود</a>
                            </li>
                            <li class=""nav-item"">
                                <a class=""nav-link text-dark"" asp-area="""" asp-controller=""Account"" asp-action=""Register"">ثبت نام</a>
                            </li>
                        }}
                    </ul>
                </div>
            </div>
        </nav>
    </header>
    <div class=""container"">
        <main role=""main"" class=""pb-3"">
            @RenderBody()
        </main>
    </div>

    <footer class=""border-top footer text-muted"">
        <div class=""container"">
            &copy; 2024 - وب‌سایت
        </div>
    </footer>
    <script src=""~/js/site.js"" asp-append-version=""true""></script>
    @await RenderSectionAsync(""Scripts"", required: false)
</body>
</html>
";
    }

    public string GetAdminLayoutTemplate()
    {
        return @"<!DOCTYPE html>
<html lang=""fa"" dir=""rtl"">
<head>
    <meta charset=""utf-8"" />
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"" />
    <title>@ViewData[""Title""] - پنل مدیریت</title>
    <link rel=""stylesheet"" href=""~/css/admin.css"" asp-append-version=""true"" />
</head>
<body>
    <div class=""admin-wrapper"">
        <aside class=""admin-sidebar"">
            <div class=""sidebar-header"">
                <h3>پنل مدیریت</h3>
            </div>
            <nav class=""sidebar-nav"">
                <ul>
                    <li><a asp-area=""Admin"" asp-controller=""Home"" asp-action=""Index"">داشبورد</a></li>
                    <li><a asp-area=""Admin"" asp-controller=""Users"" asp-action=""Index"">کاربران</a></li>
                    <li><a asp-area=""Admin"" asp-controller=""Roles"" asp-action=""Index"">نقش‌ها</a></li>
                    <li><a asp-area=""Admin"" asp-controller=""Products"" asp-action=""Index"">محصولات</a></li>
                    <li><a asp-area=""Admin"" asp-controller=""Categories"" asp-action=""Index"">دسته‌بندی‌ها</a></li>
                    <li><a asp-area=""Admin"" asp-controller=""Orders"" asp-action=""Index"">سفارشات</a></li>
                    <li><a asp-area=""Admin"" asp-controller=""Blogs"" asp-action=""Index"">بلاگ</a></li>
                </ul>
            </nav>
        </aside>
        <main class=""admin-main"">
            <header class=""admin-header"">
                <div class=""container-fluid"">
                    <div class=""row"">
                        <div class=""col"">
                            <h4>@ViewData[""Title""]</h4>
                        </div>
                        <div class=""col-auto"">
                            <a asp-area="""" asp-controller=""Home"" asp-action=""Index"">بازگشت به سایت</a>
                        </div>
                    </div>
                </div>
            </header>
            <div class=""admin-content"">
                @RenderBody()
            </div>
        </main>
    </div>
    <script src=""~/js/admin.js"" asp-append-version=""true""></script>
    @await RenderSectionAsync(""Scripts"", required: false)
</body>
</html>
";
    }

    public string GetSellerLayoutTemplate()
    {
        return @"<!DOCTYPE html>
<html lang=""fa"" dir=""rtl"">
<head>
    <meta charset=""utf-8"" />
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"" />
    <title>@ViewData[""Title""] - پنل فروشنده</title>
    <link rel=""stylesheet"" href=""~/css/seller.css"" asp-append-version=""true"" />
</head>
<body>
    <div class=""seller-wrapper"">
        <aside class=""seller-sidebar"">
            <div class=""sidebar-header"">
                <h3>پنل فروشنده</h3>
            </div>
            <nav class=""sidebar-nav"">
                <ul>
                    <li><a asp-area=""Seller"" asp-controller=""Home"" asp-action=""Index"">داشبورد</a></li>
                    <li><a asp-area=""Seller"" asp-controller=""Products"" asp-action=""Index"">محصولات من</a></li>
                    <li><a asp-area=""Seller"" asp-controller=""Orders"" asp-action=""Index"">سفارشات</a></li>
                </ul>
            </nav>
        </aside>
        <main class=""seller-main"">
            <header class=""seller-header"">
                <div class=""container-fluid"">
                    <div class=""row"">
                        <div class=""col"">
                            <h4>@ViewData[""Title""]</h4>
                        </div>
                        <div class=""col-auto"">
                            <a asp-area="""" asp-controller=""Home"" asp-action=""Index"">بازگشت به سایت</a>
                        </div>
                    </div>
                </div>
            </header>
            <div class=""seller-content"">
                @RenderBody()
            </div>
        </main>
    </div>
    <script src=""~/js/seller.js"" asp-append-version=""true""></script>
    @await RenderSectionAsync(""Scripts"", required: false)
</body>
</html>
";
    }

    public string GetUserLayoutTemplate()
    {
        return @"<!DOCTYPE html>
<html lang=""fa"" dir=""rtl"">
<head>
    <meta charset=""utf-8"" />
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"" />
    <title>@ViewData[""Title""] - پنل کاربری</title>
    <link rel=""stylesheet"" href=""~/css/user.css"" asp-append-version=""true"" />
</head>
<body>
    <div class=""user-wrapper"">
        <aside class=""user-sidebar"">
            <div class=""sidebar-header"">
                <h3>پنل کاربری</h3>
            </div>
            <nav class=""sidebar-nav"">
                <ul>
                    <li><a asp-area=""User"" asp-controller=""Home"" asp-action=""Index"">داشبورد</a></li>
                    <li><a asp-area=""User"" asp-controller=""Profile"" asp-action=""Index"">پروفایل</a></li>
                    <li><a asp-area=""User"" asp-controller=""Orders"" asp-action=""Index"">سفارشات من</a></li>
                </ul>
            </nav>
        </aside>
        <main class=""user-main"">
            <header class=""user-header"">
                <div class=""container-fluid"">
                    <div class=""row"">
                        <div class=""col"">
                            <h4>@ViewData[""Title""]</h4>
                        </div>
                        <div class=""col-auto"">
                            <a asp-area="""" asp-controller=""Home"" asp-action=""Index"">بازگشت به سایت</a>
                        </div>
                    </div>
                </div>
            </header>
            <div class=""user-content"">
                @RenderBody()
            </div>
        </main>
    </div>
    <script src=""~/js/user.js"" asp-append-version=""true""></script>
    @await RenderSectionAsync(""Scripts"", required: false)
</body>
</html>
";
    }

    public string GetAdminDashboardTemplate()
    {
        return @"@{
    ViewData[""Title""] = ""داشبورد مدیریت"";
}

<div class=""row"">
    <div class=""col-md-3"">
        <div class=""card"">
            <div class=""card-body"">
                <h5 class=""card-title"">کاربران</h5>
                <p class=""card-text"">مدیریت کاربران سیستم</p>
                <a asp-controller=""Users"" asp-action=""Index"" class=""btn btn-primary"">مشاهده</a>
            </div>
        </div>
    </div>
    <div class=""col-md-3"">
        <div class=""card"">
            <div class=""card-body"">
                <h5 class=""card-title"">محصولات</h5>
                <p class=""card-text"">مدیریت محصولات</p>
                <a asp-controller=""Products"" asp-action=""Index"" class=""btn btn-primary"">مشاهده</a>
            </div>
        </div>
    </div>
    <div class=""col-md-3"">
        <div class=""card"">
            <div class=""card-body"">
                <h5 class=""card-title"">سفارشات</h5>
                <p class=""card-text"">مدیریت سفارشات</p>
                <a asp-controller=""Orders"" asp-action=""Index"" class=""btn btn-primary"">مشاهده</a>
            </div>
        </div>
    </div>
    <div class=""col-md-3"">
        <div class=""card"">
            <div class=""card-body"">
                <h5 class=""card-title"">بلاگ</h5>
                <p class=""card-text"">مدیریت محتوا</p>
                <a asp-controller=""Blogs"" asp-action=""Index"" class=""btn btn-primary"">مشاهده</a>
            </div>
        </div>
    </div>
</div>
";
    }

    public string GetSellerDashboardTemplate()
    {
        return @"@{
    ViewData[""Title""] = ""داشبورد فروشنده"";
}

<div class=""row"">
    <div class=""col-md-6"">
        <div class=""card"">
            <div class=""card-body"">
                <h5 class=""card-title"">محصولات من</h5>
                <p class=""card-text"">مدیریت محصولات</p>
                <a asp-controller=""Products"" asp-action=""Index"" class=""btn btn-primary"">مشاهده</a>
            </div>
        </div>
    </div>
    <div class=""col-md-6"">
        <div class=""card"">
            <div class=""card-body"">
                <h5 class=""card-title"">سفارشات</h5>
                <p class=""card-text"">مشاهده سفارشات</p>
                <a asp-controller=""Orders"" asp-action=""Index"" class=""btn btn-primary"">مشاهده</a>
            </div>
        </div>
    </div>
</div>
";
    }

    public string GetUserDashboardTemplate()
    {
        return @"@{
    ViewData[""Title""] = ""داشبورد کاربری"";
}

<div class=""row"">
    <div class=""col-md-6"">
        <div class=""card"">
            <div class=""card-body"">
                <h5 class=""card-title"">پروفایل من</h5>
                <p class=""card-text"">مشاهده و ویرایش پروفایل</p>
                <a asp-controller=""Profile"" asp-action=""Index"" class=""btn btn-primary"">مشاهده</a>
            </div>
        </div>
    </div>
    <div class=""col-md-6"">
        <div class=""card"">
            <div class=""card-body"">
                <h5 class=""card-title"">سفارشات من</h5>
                <p class=""card-text"">مشاهده سفارشات</p>
                <a asp-controller=""Orders"" asp-action=""Index"" class=""btn btn-primary"">مشاهده</a>
            </div>
        </div>
    </div>
</div>
";
    }
}
