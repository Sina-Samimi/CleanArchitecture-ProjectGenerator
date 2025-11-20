using ProjectGenerator.Core.Models;

namespace ProjectGenerator.Core.Templates;

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
using {_namespace}.Domain.Entities;
using {_projectName}.WebSite.Areas.Admin.Models;

namespace {_projectName}.WebSite.Areas.Admin.Controllers;

[Area(""Admin"")]
[Authorize(Roles = ""Admin"")]
public class UsersController : Controller
{{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;

    public UsersController(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager)
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

    public async Task<IActionResult> Edit(string id)
    {{
        var user = await _userManager.FindByIdAsync(id);
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
            var user = await _userManager.FindByIdAsync(model.Id);
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
    public async Task<IActionResult> Delete(string id)
    {{
        var user = await _userManager.FindByIdAsync(id);
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
    private readonly RoleManager<IdentityRole> _roleManager;

    public RolesController(RoleManager<IdentityRole> roleManager)
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
            var role = new IdentityRole {{ Name = model.Name }};
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
using Microsoft.AspNetCore.Http;
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
using {_namespace}.Application.Common;
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
using {_namespace}.Application.Common;
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
using {_namespace}.Application.Common;
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
using {_namespace}.Domain.Entities;
using {_projectName}.WebSite.Areas.User.Models;

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
";
    }

    public string GetUserOrdersControllerTemplate()
    {
        return $@"using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using {_namespace}.Application.Common;
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
using Microsoft.AspNetCore.Hosting;
using {_namespace}.Domain.Entities;
using {_namespace}.Application.Services;
using {_projectName}.WebSite.Models;

namespace {_projectName}.WebSite.Controllers;

public class AccountController : Controller
{{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IOtpService _otpService;
    private readonly ISmsService _smsService;

    public AccountController(
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager,
        IOtpService otpService,
        ISmsService smsService)
    {{
        _signInManager = signInManager;
        _userManager = userManager;
        _otpService = otpService;
        _smsService = smsService;
    }}

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {{
        ViewData[""ReturnUrl""] = returnUrl;
        return View(new LoginViewModel());
    }}

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SendOtp(LoginViewModel model, string? returnUrl = null)
    {{
        if (!ModelState.IsValid)
        {{
            return View(""Login"", model);
        }}

        // Normalize phone number
        var phoneNumber = NormalizePhoneNumber(model.PhoneNumber);
        
        // Check if user exists
        var user = await _userManager.FindByNameAsync(phoneNumber);
        var isNewUser = user == null;

        // Generate and send OTP
        var otp = _otpService.GenerateOtp();
        await _otpService.StoreOtpAsync(phoneNumber, otp);
        await _smsService.SendOtpAsync(phoneNumber, otp);

        // Store phone number in session for verification
        HttpContext.Session.SetString(""VerifyingPhoneNumber"", phoneNumber);
        HttpContext.Session.SetString(""IsNewUser"", isNewUser.ToString());
        ViewData[""ReturnUrl""] = returnUrl;

        // Show OTP input
        model.ShowOtpInput = true;
        model.PhoneNumber = phoneNumber;
        
        // In Development mode, show OTP for testing
        var environment = HttpContext.RequestServices.GetRequiredService<Microsoft.AspNetCore.Hosting.IWebHostEnvironment>();
        if (environment.IsDevelopment())
        {{
            TempData[""Message""] = $""کد تایید به شماره شما ارسال شد. کد تایید: {{otp}}"";
        }}
        else
        {{
            TempData[""Message""] = ""کد تایید به شماره شما ارسال شد"";
        }}

        return View(""Login"", model);
    }}

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> VerifyOtp(LoginViewModel model, string? returnUrl = null)
    {{
        var phoneNumber = HttpContext.Session.GetString(""VerifyingPhoneNumber"");
        var isNewUserStr = HttpContext.Session.GetString(""IsNewUser"");

        if (string.IsNullOrEmpty(phoneNumber))
        {{
            ModelState.AddModelError(string.Empty, ""لطفا ابتدا شماره تلفن خود را وارد کنید"");
            return View(""Login"", model);
        }}

        var isNewUser = bool.Parse(isNewUserStr ?? ""false"");

        // Validate OTP
        var isValidOtp = await _otpService.ValidateOtpAsync(phoneNumber, model.Otp);
        
        if (!isValidOtp)
        {{
            ModelState.AddModelError(string.Empty, ""کد تایید نامعتبر است"");
            model.ShowOtpInput = true;
            model.PhoneNumber = phoneNumber;
            return View(""Login"", model);
        }}

        ApplicationUser user;

        if (isNewUser)
        {{
            // Create new user
            user = new ApplicationUser
            {{
                UserName = phoneNumber,
                PhoneNumber = phoneNumber,
                PhoneNumberConfirmed = true,
                EmailConfirmed = false
            }};

            var createResult = await _userManager.CreateAsync(user);
            if (!createResult.Succeeded)
            {{
                foreach (var error in createResult.Errors)
                {{
                    ModelState.AddModelError(string.Empty, error.Description);
                }}
                model.ShowOtpInput = true;
                model.PhoneNumber = phoneNumber;
                return View(""Login"", model);
            }}
        }}
        else
        {{
            user = await _userManager.FindByNameAsync(phoneNumber);
            if (user == null)
            {{
                ModelState.AddModelError(string.Empty, ""کاربر یافت نشد"");
                model.ShowOtpInput = true;
                model.PhoneNumber = phoneNumber;
                return View(""Login"", model);
            }}
        }}

        // Sign in user
        await _signInManager.SignInAsync(user, isPersistent: model.RememberMe);
        
        // Clear session
        HttpContext.Session.Remove(""VerifyingPhoneNumber"");
        HttpContext.Session.Remove(""IsNewUser"");

        // Redirect
        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        {{
            return Redirect(returnUrl);
        }}

        return RedirectToAction(nameof(HomeController.Index), ""Home"");
    }}

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {{
        await _signInManager.SignOutAsync();
        return RedirectToAction(nameof(HomeController.Index), ""Home"");
    }}

    private static string NormalizePhoneNumber(string phoneNumber)
    {{
        if (string.IsNullOrWhiteSpace(phoneNumber))
            return string.Empty;

        // Remove all non-digit characters
        var normalized = new string(phoneNumber.Where(char.IsDigit).ToArray());

        // Add 0 prefix if not present
        if (!normalized.StartsWith(""0"") && normalized.Length == 10)
        {{
            normalized = ""0"" + normalized;
        }}

        return normalized;
    }}
}}
";
    }

    public string GetProductControllerTemplate()
    {
        return $@"using Microsoft.AspNetCore.Mvc;
using {_namespace}.Application.Common;
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
using {_namespace}.Application.Common;
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
using {_namespace}.Application.Common;
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
using {_namespace}.Application.Common;
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
@using {_namespace}.Domain.Entities
@using Microsoft.AspNetCore.Identity
@inject UserManager<ApplicationUser> UserManager
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
    <link href=""https://cdn.jsdelivr.net/npm/bootstrap@5.3.0/dist/css/bootstrap.min.css"" rel=""stylesheet"" />
    <link href=""https://cdn.jsdelivr.net/npm/bootstrap@5.3.0/dist/css/bootstrap.rtl.min.css"" rel=""stylesheet"" />
    <link rel=""stylesheet"" href=""https://cdnjs.cloudflare.com/ajax/libs/font-awesome/6.4.0/css/all.min.css"" />
    <link rel=""stylesheet"" href=""~/css/site.css"" asp-append-version=""true"" />
</head>
<body>
    <header>
        <nav class=""navbar navbar-expand-lg navbar-dark bg-primary"">
            <div class=""container"">
                <a class=""navbar-brand"" asp-area="""" asp-controller=""Home"" asp-action=""Index"">
                    <i class=""fas fa-home""></i> {siteName}
                </a>
                <button class=""navbar-toggler"" type=""button"" data-bs-toggle=""collapse"" data-bs-target=""#navbarNav"">
                    <span class=""navbar-toggler-icon""></span>
                </button>
                <div class=""collapse navbar-collapse"" id=""navbarNav"">
                    <ul class=""navbar-nav me-auto"">
                        <li class=""nav-item"">
                            <a class=""nav-link"" asp-area="""" asp-controller=""Home"" asp-action=""Index"">
                                <i class=""fas fa-home""></i> خانه
                            </a>
                        </li>
                        <li class=""nav-item"">
                            <a class=""nav-link"" asp-area="""" asp-controller=""Product"" asp-action=""Index"">
                                <i class=""fas fa-shopping-bag""></i> محصولات
                            </a>
                        </li>
                        <li class=""nav-item"">
                            <a class=""nav-link"" asp-area="""" asp-controller=""Blog"" asp-action=""Index"">
                                <i class=""fas fa-blog""></i> بلاگ
                            </a>
                        </li>
                    </ul>
                    <ul class=""navbar-nav"">
                        @if (User.Identity?.IsAuthenticated ?? false)
                        {{
                            <li class=""nav-item"">
                                <a class=""nav-link"" asp-area="""" asp-controller=""Cart"" asp-action=""Index"">
                                    <i class=""fas fa-shopping-cart""></i> سبد خرید
                                </a>
                            </li>
                            @if (User.IsInRole(""Admin""))
                            {{
                                <li class=""nav-item"">
                                    <a class=""nav-link"" asp-area=""Admin"" asp-controller=""Home"" asp-action=""Index"">
                                        <i class=""fas fa-user-shield""></i> پنل مدیریت
                                    </a>
                                </li>
                            }}
                            @if (User.IsInRole(""Seller""))
                            {{
                                <li class=""nav-item"">
                                    <a class=""nav-link"" asp-area=""Seller"" asp-controller=""Home"" asp-action=""Index"">
                                        <i class=""fas fa-store""></i> پنل فروشنده
                                    </a>
                                </li>
                            }}
                            <li class=""nav-item"">
                                <a class=""nav-link"" asp-area=""User"" asp-controller=""Home"" asp-action=""Index"">
                                    <i class=""fas fa-user""></i> پنل کاربری
                                </a>
                            </li>
                            <li class=""nav-item"">
                                <form method=""post"" asp-area="""" asp-controller=""Account"" asp-action=""Logout"" class=""d-inline"">
                                    <button type=""submit"" class=""nav-link btn btn-link text-white"">
                                        <i class=""fas fa-sign-out-alt""></i> خروج
                                    </button>
                                </form>
                            </li>
                        }}
                        else
                        {{
                            <li class=""nav-item"">
                                <a class=""nav-link"" asp-area="""" asp-controller=""Account"" asp-action=""Login"">
                                    <i class=""fas fa-sign-in-alt""></i> ورود
                                </a>
                            </li>
                            <li class=""nav-item"">
                                <a class=""nav-link"" asp-area="""" asp-controller=""Account"" asp-action=""Register"">
                                    <i class=""fas fa-user-plus""></i> ثبت نام
                                </a>
                            </li>
                        }}
                    </ul>
                </div>
            </div>
        </nav>
    </header>
    
    @if (TempData[""SuccessMessage""] != null)
    {{
        <div class=""alert alert-success alert-dismissible fade show m-3"" role=""alert"">
            @TempData[""SuccessMessage""]
            <button type=""button"" class=""btn-close"" data-bs-dismiss=""alert""></button>
        </div>
    }}
    
    @if (TempData[""ErrorMessage""] != null)
    {{
        <div class=""alert alert-danger alert-dismissible fade show m-3"" role=""alert"">
            @TempData[""ErrorMessage""]
            <button type=""button"" class=""btn-close"" data-bs-dismiss=""alert""></button>
        </div>
    }}
    
    <main role=""main"">
        @RenderBody()
    </main>

    <footer class=""bg-dark text-white mt-5 py-4"">
        <div class=""container"">
            <div class=""row"">
                <div class=""col-md-4"">
                    <h5>درباره ما</h5>
                    <p>ما یک تیم متخصص هستیم که در زمینه ارائه خدمات و محصولات با کیفیت فعالیت می‌کنیم.</p>
                </div>
                <div class=""col-md-4"">
                    <h5>لینک‌های مفید</h5>
                    <ul class=""list-unstyled"">
                        <li><a href=""/Home/About"" class=""text-white-50"">درباره ما</a></li>
                        <li><a href=""/Home/Contact"" class=""text-white-50"">تماس با ما</a></li>
                        <li><a href=""/Product"" class=""text-white-50"">محصولات</a></li>
                        <li><a href=""/Blog"" class=""text-white-50"">بلاگ</a></li>
                    </ul>
                </div>
                <div class=""col-md-4"">
                    <h5>تماس با ما</h5>
                    <p><i class=""fas fa-phone""></i> 021-12345678</p>
                    <p><i class=""fas fa-envelope""></i> info@example.com</p>
                </div>
            </div>
            <hr class=""bg-white"">
            <div class=""text-center"">
                <p>&copy; 2024 {siteName}. تمام حقوق محفوظ است.</p>
            </div>
        </div>
    </footer>
    
    <script src=""https://code.jquery.com/jquery-3.7.0.min.js""></script>
    <script src=""https://cdn.jsdelivr.net/npm/bootstrap@5.3.0/dist/js/bootstrap.bundle.min.js""></script>
    <script src=""~/js/site.js"" asp-append-version=""true""></script>
    @await RenderSectionAsync(""Scripts"", required: false)
</body>
</html>
";
    }

    public string GetAdminLayoutTemplate()
    {
        return $@"<!DOCTYPE html>
<html lang=""fa"" dir=""rtl"">
<head>
    <meta charset=""utf-8"" />
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"" />
    <title>@ViewData[""Title""] - پنل مدیریت</title>
    <link href=""https://cdn.jsdelivr.net/npm/bootstrap@5.3.0/dist/css/bootstrap.min.css"" rel=""stylesheet"" />
    <link href=""https://cdn.jsdelivr.net/npm/bootstrap@5.3.0/dist/css/bootstrap.rtl.min.css"" rel=""stylesheet"" />
    <link rel=""stylesheet"" href=""https://cdnjs.cloudflare.com/ajax/libs/font-awesome/6.4.0/css/all.min.css"" />
    <link rel=""stylesheet"" href=""~/css/admin.css"" asp-append-version=""true"" />
</head>
@{{
    var adminCurrentUser = await UserManager.GetUserAsync(User);
    var adminUserName = adminCurrentUser?.UserName ?? User.Identity?.Name ?? ""مدیر"";
}}
<body>
    <div class=""admin-panel-wrapper"">
        <!-- Top Header Bar -->
        <header class=""admin-top-header"">
            <div class=""container-fluid"">
                <div class=""d-flex justify-content-between align-items-center"">
                    <div class=""d-flex align-items-center"">
                        <div class=""admin-avatar-small me-3"">
                            <span>@(adminUserName.Length > 0 ? adminUserName[0].ToString() : ""م"")</span>
                        </div>
                        <div class=""admin-name-dropdown"">
                            <span class=""admin-name-text"">@adminUserName</span>
                            <i class=""fas fa-chevron-down ms-2""></i>
                        </div>
                    </div>
                    <div class=""d-flex align-items-center gap-3"">
                        <button class=""btn btn-link text-dark"">
                            <i class=""fas fa-search""></i>
                        </button>
                        <button class=""btn btn-link text-dark position-relative"">
                            <i class=""fas fa-bell""></i>
                            <span class=""position-absolute top-0 start-100 translate-middle badge rounded-pill bg-danger"">5</span>
                        </button>
                    </div>
                </div>
            </div>
        </header>

        <div class=""admin-panel-content-wrapper"">
            <!-- Right Sidebar -->
            <aside class=""admin-right-sidebar"">
                <div class=""sidebar-brand"">
                    <div class=""brand-logo admin-logo"">
                        <span>م</span>
                    </div>
                    <h4>پنل مدیریت</h4>
                    <p class=""text-muted mb-0"">مدیریت سیستم</p>
                </div>

                <div class=""admin-summary-card"">
                    <div class=""admin-avatar-medium"">
                        <span>@(adminUserName.Length > 0 ? adminUserName[0].ToString() : ""م"")</span>
                    </div>
                    <h5 class=""mt-3"">@adminUserName</h5>
                    <p class=""text-muted small"">مدیر سیستم</p>
                    <div class=""admin-status"">
                        <span class=""status-dot""></span>
                        <span>آنلاین</span>
                    </div>
                    @if (adminCurrentUser != null)
                    {{
                        <div class=""admin-contact-info mt-3"">
                            @if (!string.IsNullOrEmpty(adminCurrentUser.Email))
                            {{
                                <div class=""contact-item"">
                                    <i class=""fas fa-envelope""></i>
                                    <span>@adminCurrentUser.Email</span>
                                </div>
                            }}
                            @if (!string.IsNullOrEmpty(adminCurrentUser.PhoneNumber))
                            {{
                                <div class=""contact-item"">
                                    <i class=""fas fa-phone""></i>
                                    <span>@adminCurrentUser.PhoneNumber</span>
                                </div>
                            }}
                        </div>
                    }}
                </div>

                <nav class=""sidebar-menu"">
                    <div class=""menu-header"">
                        <span>منوی اصلی</span>
                        <i class=""fas fa-chevron-down""></i>
                    </div>
                    <ul class=""menu-list"">
                        <li>
                            <a asp-area=""Admin"" asp-controller=""Home"" asp-action=""Index"">
                                <i class=""fas fa-tachometer-alt""></i>
                                <span>داشبورد</span>
                            </a>
                        </li>
                        <li>
                            <a asp-area=""Admin"" asp-controller=""Users"" asp-action=""Index"">
                                <i class=""fas fa-users""></i>
                                <span>کاربران</span>
                            </a>
                        </li>
                        <li>
                            <a asp-area=""Admin"" asp-controller=""Roles"" asp-action=""Index"">
                                <i class=""fas fa-user-tag""></i>
                                <span>نقش‌ها</span>
                            </a>
                        </li>
                        <li>
                            <a asp-area=""Admin"" asp-controller=""Products"" asp-action=""Index"">
                                <i class=""fas fa-box""></i>
                                <span>محصولات</span>
                            </a>
                        </li>
                        <li>
                            <a asp-area=""Admin"" asp-controller=""Categories"" asp-action=""Index"">
                                <i class=""fas fa-folder""></i>
                                <span>دسته‌بندی‌ها</span>
                            </a>
                        </li>
                        <li>
                            <a asp-area=""Admin"" asp-controller=""Orders"" asp-action=""Index"">
                                <i class=""fas fa-shopping-cart""></i>
                                <span>سفارشات</span>
                            </a>
                        </li>
                        <li>
                            <a asp-area=""Admin"" asp-controller=""Blogs"" asp-action=""Index"">
                                <i class=""fas fa-blog""></i>
                                <span>بلاگ</span>
                            </a>
                        </li>
                    </ul>
                </nav>

                <div class=""sidebar-help"">
                    <h6>نیاز به راهنمایی؟</h6>
                    <p class=""small text-muted"">تیم پشتیبانی ما آماده کمک به شماست</p>
                    <a href=""#"" class=""btn btn-primary btn-sm w-100"">
                        <i class=""fas fa-headset""></i> تماس با پشتیبانی
                    </a>
                </div>
            </aside>

            <!-- Main Content Area -->
            <main class=""admin-main-content"">
                <!-- Welcome Header -->
                <div class=""welcome-header"">
                    <div class=""d-flex justify-content-between align-items-center"">
                        <div>
                            <h2 class=""welcome-text"">
                                <span class=""greeting-emoji"">👋</span>
                                سلام، @adminUserName
                            </h2>
                            <p class=""text-muted mb-0"">به پنل مدیریت خوش آمدید</p>
                        </div>
                        <div class=""admin-avatar-large"">
                            <span>@(adminUserName.Length > 0 ? adminUserName[0].ToString() : ""م"")</span>
                        </div>
                    </div>
                </div>

                @if (TempData[""SuccessMessage""] != null)
                {{
                    <div class=""alert alert-success alert-dismissible fade show"" role=""alert"">
                        @TempData[""SuccessMessage""]
                        <button type=""button"" class=""btn-close"" data-bs-dismiss=""alert""></button>
                    </div>
                }}
                @if (TempData[""ErrorMessage""] != null)
                {{
                    <div class=""alert alert-danger alert-dismissible fade show"" role=""alert"">
                        @TempData[""ErrorMessage""]
                        <button type=""button"" class=""btn-close"" data-bs-dismiss=""alert""></button>
                    </div>
                }}

                @RenderBody()
            </main>
        </div>
    </div>

    <script src=""https://code.jquery.com/jquery-3.7.0.min.js""></script>
    <script src=""https://cdn.jsdelivr.net/npm/bootstrap@5.3.0/dist/js/bootstrap.bundle.min.js""></script>
    <script src=""~/js/admin.js"" asp-append-version=""true""></script>
    @await RenderSectionAsync(""Scripts"", required: false)
</body>
</html>
";
    }

    public string GetSellerLayoutTemplate()
    {
        return $@"<!DOCTYPE html>
<html lang=""fa"" dir=""rtl"">
<head>
    <meta charset=""utf-8"" />
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"" />
    <title>@ViewData[""Title""] - پنل فروشنده</title>
    <link href=""https://cdn.jsdelivr.net/npm/bootstrap@5.3.0/dist/css/bootstrap.min.css"" rel=""stylesheet"" />
    <link href=""https://cdn.jsdelivr.net/npm/bootstrap@5.3.0/dist/css/bootstrap.rtl.min.css"" rel=""stylesheet"" />
    <link rel=""stylesheet"" href=""https://cdnjs.cloudflare.com/ajax/libs/font-awesome/6.4.0/css/all.min.css"" />
    <link rel=""stylesheet"" href=""~/css/seller.css"" asp-append-version=""true"" />
</head>
@{{
    var sellerCurrentUser = await UserManager.GetUserAsync(User);
    var sellerUserName = sellerCurrentUser?.UserName ?? User.Identity?.Name ?? ""فروشنده"";
}}
<body>
    <div class=""seller-panel-wrapper"">
        <!-- Top Header Bar -->
        <header class=""seller-top-header"">
            <div class=""container-fluid"">
                <div class=""d-flex justify-content-between align-items-center"">
                    <div class=""d-flex align-items-center"">
                        <div class=""seller-avatar-small me-3"">
                            <span>@(sellerUserName.Length > 0 ? sellerUserName[0].ToString() : ""ف"")</span>
                        </div>
                        <div class=""seller-name-dropdown"">
                            <span class=""seller-name-text"">@sellerUserName</span>
                            <i class=""fas fa-chevron-down ms-2""></i>
                        </div>
                    </div>
                    <div class=""d-flex align-items-center gap-3"">
                        <button class=""btn btn-link text-dark"">
                            <i class=""fas fa-search""></i>
                        </button>
                        <button class=""btn btn-link text-dark position-relative"">
                            <i class=""fas fa-bell""></i>
                            <span class=""position-absolute top-0 start-100 translate-middle badge rounded-pill bg-danger"">2</span>
                        </button>
                    </div>
                </div>
            </div>
        </header>

        <div class=""seller-panel-content-wrapper"">
            <!-- Right Sidebar -->
            <aside class=""seller-right-sidebar"">
                <div class=""sidebar-brand"">
                    <div class=""brand-logo seller-logo"">
                        <span>ف</span>
                    </div>
                    <h4>پنل فروشنده</h4>
                    <p class=""text-muted mb-0"">مدیریت فروش</p>
                </div>

                <div class=""seller-summary-card"">
                    <div class=""seller-avatar-medium"">
                        <span>@(sellerUserName.Length > 0 ? sellerUserName[0].ToString() : ""ف"")</span>
                    </div>
                    <h5 class=""mt-3"">@sellerUserName</h5>
                    <p class=""text-muted small"">فروشنده</p>
                    <div class=""seller-status"">
                        <span class=""status-dot""></span>
                        <span>آنلاین</span>
                    </div>
                    @if (sellerCurrentUser != null)
                    {{
                        <div class=""seller-contact-info mt-3"">
                            @if (!string.IsNullOrEmpty(sellerCurrentUser.Email))
                            {{
                                <div class=""contact-item"">
                                    <i class=""fas fa-envelope""></i>
                                    <span>@sellerCurrentUser.Email</span>
                                </div>
                            }}
                            @if (!string.IsNullOrEmpty(sellerCurrentUser.PhoneNumber))
                            {{
                                <div class=""contact-item"">
                                    <i class=""fas fa-phone""></i>
                                    <span>@sellerCurrentUser.PhoneNumber</span>
                                </div>
                            }}
                        </div>
                    }}
                </div>

                <nav class=""sidebar-menu"">
                    <div class=""menu-header"">
                        <span>منوی اصلی</span>
                        <i class=""fas fa-chevron-down""></i>
                    </div>
                    <ul class=""menu-list"">
                        <li>
                            <a asp-area=""Seller"" asp-controller=""Home"" asp-action=""Index"">
                                <i class=""fas fa-tachometer-alt""></i>
                                <span>داشبورد</span>
                            </a>
                        </li>
                        <li>
                            <a asp-area=""Seller"" asp-controller=""Products"" asp-action=""Index"">
                                <i class=""fas fa-box""></i>
                                <span>محصولات من</span>
                            </a>
                        </li>
                        <li>
                            <a asp-area=""Seller"" asp-controller=""Orders"" asp-action=""Index"">
                                <i class=""fas fa-shopping-cart""></i>
                                <span>سفارشات</span>
                            </a>
                        </li>
                    </ul>
                </nav>

                <div class=""sidebar-help"">
                    <h6>نیاز به راهنمایی؟</h6>
                    <p class=""small text-muted"">تیم پشتیبانی ما آماده کمک به شماست</p>
                    <a href=""#"" class=""btn btn-success btn-sm w-100"">
                        <i class=""fas fa-headset""></i> تماس با پشتیبانی
                    </a>
                </div>
            </aside>

            <!-- Main Content Area -->
            <main class=""seller-main-content"">
                <!-- Welcome Header -->
                <div class=""welcome-header"">
                    <div class=""d-flex justify-content-between align-items-center"">
                        <div>
                            <h2 class=""welcome-text"">
                                <span class=""greeting-emoji"">👋</span>
                                سلام، @sellerUserName
                            </h2>
                            <p class=""text-muted mb-0"">به پنل فروشنده خوش آمدید</p>
                        </div>
                        <div class=""seller-avatar-large"">
                            <span>@(sellerUserName.Length > 0 ? sellerUserName[0].ToString() : ""ف"")</span>
                        </div>
                    </div>
                </div>

                @if (TempData[""SuccessMessage""] != null)
                {{
                    <div class=""alert alert-success alert-dismissible fade show"" role=""alert"">
                        @TempData[""SuccessMessage""]
                        <button type=""button"" class=""btn-close"" data-bs-dismiss=""alert""></button>
                    </div>
                }}
                @if (TempData[""ErrorMessage""] != null)
                {{
                    <div class=""alert alert-danger alert-dismissible fade show"" role=""alert"">
                        @TempData[""ErrorMessage""]
                        <button type=""button"" class=""btn-close"" data-bs-dismiss=""alert""></button>
                    </div>
                }}

                @RenderBody()
            </main>
        </div>
    </div>

    <script src=""https://code.jquery.com/jquery-3.7.0.min.js""></script>
    <script src=""https://cdn.jsdelivr.net/npm/bootstrap@5.3.0/dist/js/bootstrap.bundle.min.js""></script>
    <script src=""~/js/seller.js"" asp-append-version=""true""></script>
    @await RenderSectionAsync(""Scripts"", required: false)
</body>
</html>
";
    }

    public string GetUserLayoutTemplate()
    {
        return $@"<!DOCTYPE html>
<html lang=""fa"" dir=""rtl"">
<head>
    <meta charset=""utf-8"" />
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"" />
    <title>@ViewData[""Title""] - پنل کاربری</title>
    <link href=""https://cdn.jsdelivr.net/npm/bootstrap@5.3.0/dist/css/bootstrap.min.css"" rel=""stylesheet"" />
    <link href=""https://cdn.jsdelivr.net/npm/bootstrap@5.3.0/dist/css/bootstrap.rtl.min.css"" rel=""stylesheet"" />
    <link rel=""stylesheet"" href=""https://cdnjs.cloudflare.com/ajax/libs/font-awesome/6.4.0/css/all.min.css"" />
    <link rel=""stylesheet"" href=""~/css/user.css"" asp-append-version=""true"" />
</head>
@{{
    var userCurrentUser = await UserManager.GetUserAsync(User);
    var userUserName = userCurrentUser?.UserName ?? User.Identity?.Name ?? ""کاربر"";
}}
<body>
    <div class=""user-panel-wrapper"">
        <!-- Top Header Bar -->
        <header class=""user-top-header"">
            <div class=""container-fluid"">
                <div class=""d-flex justify-content-between align-items-center"">
                    <div class=""d-flex align-items-center"">
                        <div class=""user-avatar-small me-3"">
                            <span>@(userUserName.Length > 0 ? userUserName[0].ToString() : ""آ"")</span>
                        </div>
                        <div class=""user-name-dropdown"">
                            <span class=""user-name-text"">@userUserName</span>
                            <i class=""fas fa-chevron-down ms-2""></i>
                        </div>
                    </div>
                    <div class=""d-flex align-items-center gap-3"">
                        <button class=""btn btn-link text-dark"">
                            <i class=""fas fa-filter""></i>
                        </button>
                        <button class=""btn btn-link text-dark position-relative"">
                            <i class=""fas fa-bell""></i>
                            <span class=""position-absolute top-0 start-100 translate-middle badge rounded-pill bg-danger"">3</span>
                        </button>
                    </div>
                </div>
            </div>
        </header>

        <div class=""user-panel-content-wrapper"">
            <!-- Right Sidebar -->
            <aside class=""user-right-sidebar"">
                <div class=""sidebar-brand"">
                    <div class=""brand-logo"">
                        <span>آ</span>
                    </div>
                    <h4>آرسیس</h4>
                    <p class=""text-muted mb-0"">پنل کاربری</p>
                </div>

                <div class=""user-summary-card"">
                    <div class=""user-avatar-medium"">
                        <span>@(userUserName.Length > 0 ? userUserName[0].ToString() : ""آ"")</span>
                    </div>
                    <h5 class=""mt-3"">@userUserName</h5>
                    <p class=""text-muted small"">پروفایل خود را کامل نگه دارید</p>
                    <div class=""user-status"">
                        <span class=""status-dot""></span>
                        <span>آنلاین</span>
                    </div>
                    @if (userCurrentUser != null)
                    {{
                        <div class=""user-contact-info mt-3"">
                            @if (!string.IsNullOrEmpty(userCurrentUser.Email))
                            {{
                                <div class=""contact-item"">
                                    <i class=""fas fa-envelope""></i>
                                    <span>@userCurrentUser.Email</span>
                                </div>
                            }}
                            @if (!string.IsNullOrEmpty(userCurrentUser.PhoneNumber))
                            {{
                                <div class=""contact-item"">
                                    <i class=""fas fa-phone""></i>
                                    <span>@userCurrentUser.PhoneNumber</span>
                                </div>
                            }}
                        </div>
                    }}
                </div>

                <nav class=""sidebar-menu"">
                    <div class=""menu-header"">
                        <span>منوی اصلی</span>
                        <i class=""fas fa-chevron-down""></i>
                    </div>
                    <ul class=""menu-list"">
                        <li>
                            <a asp-area=""User"" asp-controller=""Home"" asp-action=""Index"">
                                <i class=""fas fa-home""></i>
                                <span>داشبورد</span>
                            </a>
                        </li>
                        <li>
                            <a asp-area=""User"" asp-controller=""Profile"" asp-action=""Index"">
                                <i class=""fas fa-user""></i>
                                <span>پروفایل</span>
                            </a>
                        </li>
                        <li>
                            <a asp-area=""User"" asp-controller=""Orders"" asp-action=""Index"">
                                <i class=""fas fa-shopping-bag""></i>
                                <span>سفارشات من</span>
                            </a>
                        </li>
                    </ul>
                </nav>

                <div class=""sidebar-help"">
                    <h6>نیاز به راهنمایی؟</h6>
                    <p class=""small text-muted"">تیم پشتیبانی ما آماده کمک به شماست</p>
                    <a href=""#"" class=""btn btn-success btn-sm w-100"">
                        <i class=""fas fa-headset""></i> تماس با پشتیبانی
                    </a>
                </div>
            </aside>

            <!-- Main Content Area -->
            <main class=""user-main-content"">
                <!-- Welcome Header -->
                <div class=""welcome-header"">
                    <div class=""d-flex justify-content-between align-items-center"">
                        <div>
                            <h2 class=""welcome-text"">
                                <span class=""greeting-emoji"">👋</span>
                                سلام، @userUserName
                            </h2>
                            <p class=""text-muted mb-0"">پروفایل خود را کامل نگه دارید</p>
                        </div>
                        <div class=""user-avatar-large"">
                            <span>@(userUserName.Length > 0 ? userUserName[0].ToString() : ""آ"")</span>
                        </div>
                    </div>
                </div>

                @if (TempData[""SuccessMessage""] != null)
                {{
                    <div class=""alert alert-success alert-dismissible fade show"" role=""alert"">
                        @TempData[""SuccessMessage""]
                        <button type=""button"" class=""btn-close"" data-bs-dismiss=""alert""></button>
                    </div>
                }}
                @if (TempData[""ErrorMessage""] != null)
                {{
                    <div class=""alert alert-danger alert-dismissible fade show"" role=""alert"">
                        @TempData[""ErrorMessage""]
                        <button type=""button"" class=""btn-close"" data-bs-dismiss=""alert""></button>
                    </div>
                }}

                @RenderBody()
            </main>
        </div>
    </div>

    <script src=""https://code.jquery.com/jquery-3.7.0.min.js""></script>
    <script src=""https://cdn.jsdelivr.net/npm/bootstrap@5.3.0/dist/js/bootstrap.bundle.min.js""></script>
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
    var displayName = User?.Identity?.Name ?? ""مدیر"";
}

<div class=""dashboard-hero"">
    <div>
        <div class=""hero-label""><i class=""fas fa-shield-alt ms-1""></i>پنل مدیریت</div>
        <h3>سلام، @displayName</h3>
        <p>ظاهر و چیدمان دقیقاً مشابه نسخه ArsisTest با تمرکز بر کارت‌های خلاصه و دایره وضعیت.</p>
        <div class=""hero-meta"">
            <span class=""meta-chip""><i class=""fas fa-thumbtack""></i>بدون تست و سازمان</span>
            <span class=""meta-chip""><i class=""fas fa-magic""></i>الهام از متریکون</span>
        </div>
    </div>
    <div class=""stat-stack"">
        <div class=""stat-circle"">0</div>
        <div class=""stat-note"">
            <span class=""fw-bold"">کاربران</span>
            <small class=""text-muted"">همان دایره شمارنده بالای کارت‌ها</small>
        </div>
    </div>
</div>

<div class=""dashboard-grid"">
    <div class=""summary-card primary"">
        <div class=""icon-badge""><i class=""fas fa-users""></i></div>
        <div class=""label"">کاربران</div>
        <div class=""value"">128</div>
        <p class=""desc"">همان کارت گرد با آیکن آبی</p>
    </div>
    <div class=""summary-card info"">
        <div class=""icon-badge""><i class=""fas fa-shield-alt""></i></div>
        <div class=""label"">سطح دسترسی</div>
        <div class=""value"">کامل</div>
        <p class=""desc"">هم‌راستا با کارت سبز ArsisTest</p>
    </div>
    <div class=""summary-card success"">
        <div class=""icon-badge""><i class=""fas fa-box""></i></div>
        <div class=""label"">محصولات</div>
        <div class=""value"">342</div>
        <p class=""desc"">مدیریت کالا و دسته‌ها</p>
    </div>
    <div class=""summary-card warning"">
        <div class=""icon-badge""><i class=""fas fa-receipt""></i></div>
        <div class=""label"">سفارشات</div>
        <div class=""value"">57</div>
        <p class=""desc"">پردازش و صدور فاکتور</p>
    </div>
</div>

<div class=""row g-3"">
    <div class=""col-lg-7"">
        <div class=""action-card"">
            <div class=""section-title-bar"">
                <h5 class=""mb-0"">اقدامات سریع</h5>
                <span class=""text-muted small"">چیدمان منویی شبیه لیست کنار کارت</span>
            </div>
            <div class=""list-group list-group-flush"">
                <a class=""list-group-item"" asp-controller=""Users"" asp-action=""Create"">
                    <span><i class=""fas fa-user-plus ms-2""></i>افزودن کاربر جدید</span>
                    <span class=""badge bg-primary rounded-pill"">جدید</span>
                </a>
                <a class=""list-group-item"" asp-controller=""Roles"" asp-action=""Index"">
                    <span><i class=""fas fa-user-shield ms-2""></i>مدیریت نقش‌ها</span>
                    <i class=""fas fa-angle-left text-muted""></i>
                </a>
                <a class=""list-group-item"" asp-controller=""Products"" asp-action=""Create"">
                    <span><i class=""fas fa-plus-circle ms-2""></i>افزودن محصول</span>
                    <i class=""fas fa-angle-left text-muted""></i>
                </a>
                <a class=""list-group-item"" asp-controller=""Orders"" asp-action=""Index"">
                    <span><i class=""fas fa-clipboard-list ms-2""></i>مشاهده سفارشات</span>
                    <i class=""fas fa-angle-left text-muted""></i>
                </a>
                <a class=""list-group-item"" asp-controller=""Blogs"" asp-action=""Index"">
                    <span><i class=""fas fa-pen ms-2""></i>مدیریت مقالات</span>
                    <i class=""fas fa-angle-left text-muted""></i>
                </a>
            </div>
        </div>
    </div>
    <div class=""col-lg-5"">
        <div class=""action-card h-100"">
            <div class=""section-title-bar"">
                <h5 class=""mb-0"">مرور سیستم</h5>
                <span class=""text-muted small"">نمای کلی وضعیت</span>
            </div>
            <ul class=""list-unstyled mb-0 small text-muted"">
                <li class=""mb-2""><i class=""fas fa-check-circle text-success ms-2""></i>دایره شمارنده و کارت‌ها در بالای صفحه</li>
                <li class=""mb-2""><i class=""fas fa-check-circle text-success ms-2""></i>کارت‌های سفید با حاشیه روشن</li>
                <li class=""mb-2""><i class=""fas fa-check-circle text-success ms-2""></i>لیست اقدامات سریع درون کارت</li>
                <li><i class=""fas fa-check-circle text-success ms-2""></i>بدون وابستگی به تست یا سازمان</li>
            </ul>
        </div>
    </div>
</div>
";
    }


    public string GetSellerDashboardTemplate()
    {
        return @"@{
    ViewData[""Title""] = ""داشبورد فروشنده"";
    var displayName = User?.Identity?.Name ?? ""فروشنده"";
}

<div class=""dashboard-hero"">
    <div>
        <div class=""hero-label""><i class=""fas fa-store ms-1""></i>پنل فروشنده</div>
        <h3>سلام، @displayName</h3>
        <p>سربرگ، کارت‌های گرد و لیست اقدامات مشابه نسخه مرجع ArsisTest.</p>
        <div class=""hero-meta"">
            <span class=""meta-chip""><i class=""fas fa-box""></i>محصولات فعال</span>
            <span class=""meta-chip""><i class=""fas fa-truck""></i>ارسال سریع</span>
        </div>
    </div>
    <div class=""stat-stack"">
        <div class=""stat-circle"">0</div>
        <div class=""stat-note"">
            <span class=""fw-bold"">سفارشات</span>
            <small class=""text-muted"">نمای دایره شمارنده</small>
        </div>
    </div>
</div>

<div class=""dashboard-grid"">
    <div class=""summary-card success"">
        <div class=""icon-badge""><i class=""fas fa-box-open""></i></div>
        <div class=""label"">محصولات فعال</div>
        <div class=""value"">86</div>
        <p class=""desc"">نمای کارت سبز مانند نمونه</p>
    </div>
    <div class=""summary-card primary"">
        <div class=""icon-badge""><i class=""fas fa-truck""></i></div>
        <div class=""label"">سفارشات جاری</div>
        <div class=""value"">24</div>
        <p class=""desc"">در انتظار ارسال</p>
    </div>
    <div class=""summary-card warning"">
        <div class=""icon-badge""><i class=""fas fa-wallet""></i></div>
        <div class=""label"">تسویه</div>
        <div class=""value"">12</div>
        <p class=""desc"">تسویه حساب‌های در صف</p>
    </div>
</div>

<div class=""row g-3"">
    <div class=""col-lg-7"">
        <div class=""action-card"">
            <div class=""section-title-bar"">
                <h5 class=""mb-0"">اقدامات سریع</h5>
                <span class=""text-muted small"">مدیریت فروش</span>
            </div>
            <div class=""list-group list-group-flush"">
                <a class=""list-group-item"" asp-controller=""Products"" asp-action=""Index"">
                    <span><i class=""fas fa-box ms-2""></i>محصولات من</span>
                    <i class=""fas fa-angle-left text-muted""></i>
                </a>
                <a class=""list-group-item"" asp-controller=""Orders"" asp-action=""Index"">
                    <span><i class=""fas fa-shopping-cart ms-2""></i>سفارشات مشتریان</span>
                    <i class=""fas fa-angle-left text-muted""></i>
                </a>
                <a class=""list-group-item"" asp-controller=""Orders"" asp-action=""Index"">
                    <span><i class=""fas fa-truck-loading ms-2""></i>سفارشات در انتظار ارسال</span>
                    <i class=""fas fa-angle-left text-muted""></i>
                </a>
            </div>
        </div>
    </div>
    <div class=""col-lg-5"">
        <div class=""action-card h-100"">
            <div class=""section-title-bar"">
                <h5 class=""mb-0"">نکات طراحی</h5>
                <span class=""text-muted small"">الهام از ArsisTest</span>
            </div>
            <ul class=""list-unstyled mb-0 small text-muted"">
                <li class=""mb-2""><i class=""fas fa-check-circle text-success ms-2""></i>سربرگ با دایره شمارنده</li>
                <li class=""mb-2""><i class=""fas fa-check-circle text-success ms-2""></i>کارت‌های سفید حاشیه‌دار</li>
                <li><i class=""fas fa-check-circle text-success ms-2""></i>بدون ماژول تست یا سازمان</li>
            </ul>
        </div>
    </div>
</div>
";
    }


    public string GetUserDashboardTemplate()
    {
        return @"@{
    ViewData[""Title""] = ""داشبورد کاربری"";
    var displayName = User?.Identity?.Name ?? ""کاربر"";
}

<div class=""dashboard-hero"">
    <div>
        <div class=""hero-label""><i class=""fas fa-user ms-1""></i>پنل کاربری</div>
        <h3>سلام، @displayName</h3>
        <p>سربرگ، کارت‌های سفید و دایره شمارنده همسان با رابط ArsisTest.</p>
        <div class=""hero-meta"">
            <span class=""meta-chip""><i class=""fas fa-phone""></i>پروفایل کامل</span>
            <span class=""meta-chip""><i class=""fas fa-shopping-bag""></i>سفارشات فعال</span>
        </div>
    </div>
    <div class=""stat-stack"">
        <div class=""stat-circle"">0</div>
        <div class=""stat-note"">
            <span class=""fw-bold"">جمع سفارشات</span>
            <small class=""text-muted"">دایره بزرگ بالای کارت‌ها</small>
        </div>
    </div>
</div>

<div class=""dashboard-grid"">
    <div class=""summary-card primary"">
        <div class=""icon-badge""><i class=""fas fa-user-circle""></i></div>
        <div class=""label"">پروفایل</div>
        <div class=""value"">کامل</div>
        <p class=""desc"">اطلاعات به‌روز</p>
    </div>
    <div class=""summary-card success"">
        <div class=""icon-badge""><i class=""fas fa-bag-shopping""></i></div>
        <div class=""label"">سفارشات</div>
        <div class=""value"">8 فعال</div>
        <p class=""desc"">پیگیری وضعیت سفارش</p>
    </div>
    <div class=""summary-card info"">
        <div class=""icon-badge""><i class=""fas fa-ticket-alt""></i></div>
        <div class=""label"">پشتیبانی</div>
        <div class=""value"">3 باز</div>
        <p class=""desc"">گفتگوهای جاری</p>
    </div>
</div>

<div class=""row g-3"">
    <div class=""col-lg-7"">
        <div class=""action-card"">
            <div class=""section-title-bar"">
                <h5 class=""mb-0"">اقدامات سریع</h5>
                <span class=""text-muted small"">مدیریت حساب</span>
            </div>
            <div class=""list-group list-group-flush"">
                <a class=""list-group-item"" asp-controller=""Profile"" asp-action=""Edit"">
                    <span><i class=""fas fa-user-edit ms-2""></i>ویرایش اطلاعات</span>
                    <i class=""fas fa-angle-left text-muted""></i>
                </a>
                <a class=""list-group-item"" asp-controller=""Orders"" asp-action=""Index"">
                    <span><i class=""fas fa-box-open ms-2""></i>پیگیری سفارشات</span>
                    <i class=""fas fa-angle-left text-muted""></i>
                </a>
                <a class=""list-group-item"" asp-controller=""Cart"" asp-action=""Index"">
                    <span><i class=""fas fa-shopping-cart ms-2""></i>مشاهده سبد خرید</span>
                    <i class=""fas fa-angle-left text-muted""></i>
                </a>
            </div>
        </div>
    </div>
    <div class=""col-lg-5"">
        <div class=""action-card h-100"">
            <div class=""section-title-bar"">
                <h5 class=""mb-0"">یادآوری‌های طراحی</h5>
                <span class=""text-muted small"">هماهنگ با ArsisTest</span>
            </div>
            <ul class=""list-unstyled mb-0 small text-muted"">
                <li class=""mb-2""><i class=""fas fa-check-circle text-success ms-2""></i>سربرگ و کارت‌های سفید</li>
                <li class=""mb-2""><i class=""fas fa-check-circle text-success ms-2""></i>دایره شمارنده بالای کارت‌ها</li>
                <li><i class=""fas fa-check-circle text-success ms-2""></i>فاقد بخش تست و سازمان</li>
            </ul>
        </div>
    </div>
</div>
";
    }


    public string GetUserProfileIndexViewTemplate()
    {
        return $@"@model {_namespace}.Domain.Entities.ApplicationUser
@{{
    ViewData[""Title""] = ""پروفایل کاربری"";
    var user = Model;
    var membershipDate = user != null ? user.CreatedOn : DateTimeOffset.UtcNow;
    var lastUpdate = user != null ? user.LastModifiedOn : DateTimeOffset.UtcNow;
    var daysSinceMembership = (DateTimeOffset.UtcNow - membershipDate).Days;
    var completionPercentage = 100; // Calculate based on filled fields
}}

<div class=""profile-page"">
    <div class=""profile-shell"">
        <div>
            <div class=""profile-hero"">
                <div class=""hero-row"">
                    <div>
                        <div class=""hero-chip""><i class=""fas fa-id-card ms-1""></i>پروفایل کاربری</div>
                        <h2>@(user?.UserName ?? ""کاربر"")</h2>
                        <div class=""hero-meta"">
                            @if (!string.IsNullOrEmpty(user?.PhoneNumber))
                            {{
                                <span class=""meta-chip""><i class=""fas fa-phone""></i>@user.PhoneNumber</span>
                            }}
                            @if (!string.IsNullOrEmpty(user?.Email))
                            {{
                                <span class=""meta-chip""><i class=""fas fa-envelope""></i>@user.Email</span>
                            }}
                        </div>
                    </div>
                    <div class=""avatar-circle-large"">
                        <span>@(user?.UserName?.Length > 0 ? user.UserName[0].ToString() : ""آ"")</span>
                    </div>
                </div>
                <div class=""profile-actions"">
                    <a asp-action=""Edit"" class=""btn btn-edit-profile""><i class=""fas fa-edit ms-1""></i>ویرایش پروفایل</a>
                    <a href=""#"" class=""btn btn-account-details""><i class=""fas fa-file-alt ms-1""></i>جزئیات حساب</a>
                </div>
                <div class=""info-pills"">
                    <div class=""info-pill"">
                        <div class=""label"">آخرین ورود</div>
                        <div class=""value"">@DateTime.Now.ToString(""yyyy/MM/dd"")</div>
                        <div class=""desc"">ورود با کد یک‌بار مصرف</div>
                    </div>
                    <div class=""info-pill"">
                        <div class=""label"">روزهای همراهی</div>
                        <div class=""value"">@daysSinceMembership</div>
                        <div class=""desc"">از @membershipDate.ToString(""yyyy/MM/dd"")</div>
                    </div>
                    <div class=""info-pill"">
                        <div class=""label"">درصد تکمیل</div>
                        <div class=""value"">@completionPercentage%</div>
                        <div class=""desc"">همان کارت پیشرفت ArsisTest</div>
                    </div>
                </div>
            </div>

            <div class=""info-cards-row"">
                <div class=""info-card"">
                    <div class=""info-card-header"">
                        <i class=""fas fa-sign-in-alt""></i>
                        <h6>آخرین ورود</h6>
                    </div>
                    <div class=""info-card-body"">
                        <div class=""info-value"">@DateTime.Now.ToString(""yyyy/MM/dd"")</div>
                        <div class=""info-description"">ورود با کد یک بار مصرف</div>
                    </div>
                </div>

                <div class=""info-card"">
                    <div class=""info-card-header"">
                        <i class=""fas fa-calendar-alt""></i>
                        <h6>روزهای همراهی</h6>
                    </div>
                    <div class=""info-card-body"">
                        <div class=""info-value"">@daysSinceMembership</div>
                        <div class=""info-description"">از @membershipDate.ToString(""yyyy/MM/dd"")</div>
                    </div>
                </div>

                <div class=""info-card"">
                    <div class=""info-card-header"">
                        <i class=""fas fa-percentage""></i>
                        <h6>درصد تکمیل</h6>
                    </div>
                    <div class=""info-card-body"">
                        <div class=""info-value"">@completionPercentage%</div>
                        <div class=""info-description"">آماده دریافت گزارشها</div>
                    </div>
                </div>
            </div>
        </div>
        <div>
            <div class=""action-card h-100"">
                <div class=""section-title-bar"">
                    <h5 class=""mb-0"">مرکز اطلاعات</h5>
                    <span class=""text-muted small"">نمای کناری مشابه ArsisTest</span>
                </div>
                <ul class=""list-unstyled mb-0 small text-muted"">
                    <li class=""mb-2""><i class=""fas fa-check-circle text-success ms-2""></i>کارت پروفایل با پس‌زمینه آبی روشن</li>
                    <li class=""mb-2""><i class=""fas fa-check-circle text-success ms-2""></i>دکمه‌های سبز و آبی مشابه تصویر</li>
                    <li><i class=""fas fa-check-circle text-success ms-2""></i>همه جزئیات بدون ماژول تست/سازمان</li>
                </ul>
            </div>
        </div>
    </div>
</div>
";
    }

    public string GetUserProfileEditViewTemplate()
    {
        return $@"@model {_projectName}.WebSite.Areas.User.Models.ProfileEditViewModel
@{{
    ViewData[""Title""] = ""ویرایش پروفایل"";
}}

<div class=""profile-edit-page"">
    <div class=""page-header mb-4"">
        <h1 class=""page-title"">ویرایش پروفایل</h1>
    </div>

    <div class=""card"">
        <div class=""card-body"">
            <form asp-action=""Edit"" method=""post"">
                <div asp-validation-summary=""All"" class=""text-danger mb-3""></div>
                
                <div class=""row"">
                    <div class=""col-md-6 mb-3"">
                        <label asp-for=""Email"" class=""form-label"">ایمیل</label>
                        <input asp-for=""Email"" class=""form-control"" />
                        <span asp-validation-for=""Email"" class=""text-danger""></span>
                    </div>
                    
                    <div class=""col-md-6 mb-3"">
                        <label asp-for=""PhoneNumber"" class=""form-label"">شماره تلفن</label>
                        <input asp-for=""PhoneNumber"" class=""form-control"" />
                        <span asp-validation-for=""PhoneNumber"" class=""text-danger""></span>
                    </div>
                </div>

                <div class=""d-flex gap-2"">
                    <button type=""submit"" class=""btn btn-primary"">
                        <i class=""fas fa-save""></i> ذخیره تغییرات
                    </button>
                    <a asp-action=""Index"" class=""btn btn-secondary"">
                        <i class=""fas fa-times""></i> انصراف
                    </a>
                </div>
            </form>
        </div>
    </div>
</div>
";
    }

    // ==================== Main Site Views ====================
    
    public string GetHomeIndexViewTemplate()
    {
        return $@"@{{
    ViewData[""Title""] = ""خانه"";
}}

<section class=""landing-hero mb-5"">
    <div class=""container"">
        <div class=""row align-items-center g-4"">
            <div class=""col-lg-7"">
                <div class=""hero-content"">
                    <div class=""d-inline-flex align-items-center gap-2 glass-card text-white mb-3"">
                        <i class=""fas fa-bolt""></i>
                        <span>تولید سریع پروژه‌های Clean Architecture</span>
                    </div>
                    <h1 class=""hero-title"">زیرساخت آماده برای فروشگاه، بلاگ و پنل‌های مدیریتی</h1>
                    <p class=""hero-subtitle"">همان دیزاینی که در ArsisTest دیده‌اید؛ همراه با وب‌سایت اصلی، پنل مدیریت، فروشنده و کاربر در یک پکیج.</p>
                    <div class=""hero-actions"">
                        <a href=""/Product"" class=""btn btn-light btn-lg""><i class=""fas fa-shopping-bag ms-2""></i>مشاهده محصولات</a>
                        <a href=""/Account/Register"" class=""btn btn-outline-light btn-lg""><i class=""fas fa-rocket ms-2""></i>شروع سریع</a>
                    </div>
                    <div class=""stats-badges"">
                        <div class=""stats-badge"">
                            <i class=""fas fa-layer-group""></i>
                            <div>
                                <div class=""value"">4 لایه آماده</div>
                                <small>Domain, Application, Infrastructure, WebSite</small>
                            </div>
                        </div>
                        <div class=""stats-badge"">
                            <i class=""fas fa-users-cog""></i>
                            <div>
                                <div class=""value"">پنل‌های کامل</div>
                                <small>مدیر، فروشنده و کاربر</small>
                            </div>
                        </div>
                        <div class=""stats-badge"">
                            <i class=""fas fa-shield-check""></i>
                            <div>
                                <div class=""value"">هویت و نقش‌ها</div>
                                <small>ساخته‌شده با Identity</small>
                            </div>
                        </div>
                    </div>
                </div>
            </div>
            <div class=""col-lg-5"">
                <div class=""glass-card"">
                    <h5 class=""mb-3"">پنل‌ها و صفحات آماده</h5>
                    <div class=""d-grid gap-3"">
                        <div class=""panel-card"">
                            <div class=""d-flex justify-content-between align-items-start"">
                                <div>
                                    <h6 class=""mb-1"">پنل مدیریت</h6>
                                    <p class=""panel-meta mb-2"">مدیریت کاربران، نقش‌ها، محصولات و سفارشات</p>
                                    <a class=""panel-link"" asp-area=""Admin"" asp-controller=""Home"" asp-action=""Index""><i class=""fas fa-arrow-left""></i> ورود به داشبورد</a>
                                </div>
                                <span class=""badge bg-primary rounded-pill"">Admin</span>
                            </div>
                        </div>
                        <div class=""panel-card"">
                            <div class=""d-flex justify-content-between align-items-start"">
                                <div>
                                    <h6 class=""mb-1"">پنل فروشنده</h6>
                                    <p class=""panel-meta mb-2"">مدیریت کالاها و سفارشات مخصوص فروشنده</p>
                                    <a class=""panel-link"" asp-area=""Seller"" asp-controller=""Home"" asp-action=""Index""><i class=""fas fa-arrow-left""></i> مدیریت فروش</a>
                                </div>
                                <span class=""badge bg-success rounded-pill"">Seller</span>
                            </div>
                        </div>
                        <div class=""panel-card mb-0"">
                            <div class=""d-flex justify-content-between align-items-start"">
                                <div>
                                    <h6 class=""mb-1"">پنل کاربری</h6>
                                    <p class=""panel-meta mb-2"">پروفایل، سفارشات و تیکت‌های کاربر</p>
                                    <a class=""panel-link"" asp-area=""User"" asp-controller=""Home"" asp-action=""Index""><i class=""fas fa-arrow-left""></i> مشاهده پنل</a>
                                </div>
                                <span class=""badge bg-info rounded-pill"">User</span>
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        </div>
    </div>
</section>

<div class=""container mb-5"">
    <div class=""section-header"">
        <div>
            <h2 class=""section-title"">امکانات کلیدی</h2>
            <p class=""section-subtitle"">همان تجربه ArsisTest با تمرکز بر فروشگاه و بلاگ</p>
        </div>
    </div>
    <div class=""feature-grid"">
        <div class=""feature-card"">
            <div class=""feature-icon""><i class=""fas fa-store""></i></div>
            <h5 class=""mb-2"">کاتالوگ محصولات</h5>
            <p class=""text-muted mb-3"">صفحات لیست و جزئیات محصول با دکمه‌های افزودن به سبد.</p>
            <a href=""/Product"" class=""btn btn-outline-primary btn-sm"">نمایش محصولات</a>
        </div>
        <div class=""feature-card"">
            <div class=""feature-icon""><i class=""fas fa-blog""></i></div>
            <h5 class=""mb-2"">بلاگ و محتوا</h5>
            <p class=""text-muted mb-3"">لیست مقالات، جزئیات، برچسب‌ها و مدیریت انتشار.</p>
            <a href=""/Blog"" class=""btn btn-outline-success btn-sm"">ورود به بلاگ</a>
        </div>
        <div class=""feature-card"">
            <div class=""feature-icon""><i class=""fas fa-shopping-cart""></i></div>
            <h5 class=""mb-2"">سبد خرید و تسویه</h5>
            <p class=""text-muted mb-3"">فرآیند سبد خرید، پرداخت و پیگیری سفارش به‌صورت آماده.</p>
            <a href=""/Cart"" class=""btn btn-outline-dark btn-sm"">بررسی سفارش</a>
        </div>
        <div class=""feature-card"">
            <div class=""feature-icon""><i class=""fas fa-user-shield""></i></div>
            <h5 class=""mb-2"">هویت و نقش‌ها</h5>
            <p class=""text-muted mb-3"">ورود/ثبت‌نام، نقش‌ها و سیاست‌های دسترسی از پیش پیکربندی شده.</p>
            <a href=""/Account/Login"" class=""btn btn-outline-secondary btn-sm"">تجربه ورود</a>
        </div>
    </div>
</div>

<div class=""container mb-5"">
    <div class=""section-header"">
        <div>
            <h2 class=""section-title"">پنل‌های الهام‌گرفته از ArsisTest</h2>
            <p class=""section-subtitle"">طراحی کارت‌ها، هدر چسبان و ناوبری درون‌پنل مطابق نمونه اصلی</p>
        </div>
    </div>
    <div class=""row g-4"">
        <div class=""col-lg-4"">
            <div class=""panel-card h-100"">
                <div class=""d-flex align-items-center gap-3 mb-3"">
                    <span class=""feature-icon""><i class=""fas fa-gauge""></i></span>
                    <div>
                        <h5 class=""mb-1"">داشبورد مدیریتی</h5>
                        <p class=""panel-meta mb-0"">نمایش گزارش‌های کلیدی و لینک‌های سریع</p>
                    </div>
                </div>
                <ul class=""list-unstyled mb-3 small text-muted"">
                    <li class=""mb-2""><i class=""fas fa-check-circle text-success ms-2""></i> کارت‌های خلاصه کاربران، محصولات و سفارش‌ها</li>
                    <li class=""mb-2""><i class=""fas fa-check-circle text-success ms-2""></i> دکمه‌های اقدام سریع برای افزودن داده</li>
                    <li><i class=""fas fa-check-circle text-success ms-2""></i> هدر خوش‌آمدگویی و هشدارها</li>
                </ul>
                <a asp-area=""Admin"" asp-controller=""Home"" asp-action=""Index"" class=""btn btn-primary w-100"">ورود به مدیریت</a>
            </div>
        </div>
        <div class=""col-lg-4"">
            <div class=""panel-card h-100"">
                <div class=""d-flex align-items-center gap-3 mb-3"">
                    <span class=""feature-icon""><i class=""fas fa-store""></i></span>
                    <div>
                        <h5 class=""mb-1"">پنل فروشنده</h5>
                        <p class=""panel-meta mb-0"">مدیریت سفارشات و محصولات شخصی</p>
                    </div>
                </div>
                <ul class=""list-unstyled mb-3 small text-muted"">
                    <li class=""mb-2""><i class=""fas fa-check-circle text-success ms-2""></i> کارت‌های آماری سفارش و درآمد</li>
                    <li class=""mb-2""><i class=""fas fa-check-circle text-success ms-2""></i> لینک‌های سریع به مدیریت محصولات</li>
                    <li><i class=""fas fa-check-circle text-success ms-2""></i> ناوبری ثابت در سایدبار</li>
                </ul>
                <a asp-area=""Seller"" asp-controller=""Home"" asp-action=""Index"" class=""btn btn-success w-100"">ورود فروشنده</a>
            </div>
        </div>
        <div class=""col-lg-4"">
            <div class=""panel-card h-100"">
                <div class=""d-flex align-items-center gap-3 mb-3"">
                    <span class=""feature-icon""><i class=""fas fa-user""></i></span>
                    <div>
                        <h5 class=""mb-1"">پنل کاربر</h5>
                        <p class=""panel-meta mb-0"">پروفایل، سفارشات و بروزرسانی اطلاعات</p>
                    </div>
                </div>
                <ul class=""list-unstyled mb-3 small text-muted"">
                    <li class=""mb-2""><i class=""fas fa-check-circle text-success ms-2""></i> کارت خلاصه حساب و وضعیت سفارشات</li>
                    <li class=""mb-2""><i class=""fas fa-check-circle text-success ms-2""></i> فرم ویرایش اطلاعات مطابق سبک Arsis</li>
                    <li><i class=""fas fa-check-circle text-success ms-2""></i> هدر چسبان با آواتار کاربر</li>
                </ul>
                <a asp-area=""User"" asp-controller=""Home"" asp-action=""Index"" class=""btn btn-info text-white w-100"">پروفایل کاربری</a>
            </div>
        </div>
    </div>
</div>

<div class=""container mb-5"">
    <div class=""cta-section"">
        <div class=""row align-items-center g-3"">
            <div class=""col-lg-8"">
                <h3 class=""mb-2"">همه چیز آماده است؛ کافیست پروژه را تولید کنید.</h3>
                <p class=""text-muted mb-3"">تنظیمات تم، مسیرها و فایل‌های سی‌اس‌اس/اسکریپت با الهام از ArsisTest در خروجی شما قرار می‌گیرند.</p>
                <div class=""cta-badges"">
                    <span class=""cta-badge""><i class=""fas fa-check text-success ms-2""></i>بدون تست و سازمان</span>
                    <span class=""cta-badge""><i class=""fas fa-check text-success ms-2""></i>Bootstrap 5 + RTL</span>
                    <span class=""cta-badge""><i class=""fas fa-check text-success ms-2""></i>هویت و نقش‌بندی کامل</span>
                </div>
            </div>
            <div class=""col-lg-4 text-lg-end"">
                <a href=""/Account/Register"" class=""btn btn-primary btn-lg""><i class=""fas fa-magic ms-2""></i>شروع تولید پروژه</a>
            </div>
        </div>
    </div>
</div>
"; 
    }

    public string GetHomeAboutViewTemplate()
    {
        return $@"@{{
    ViewData[""Title""] = ""درباره ما"";
}}

<div class=""container"">
    <div class=""row"">
        <div class=""col-md-12"">
            <h2>درباره ما</h2>
            <p class=""lead"">ما یک تیم متخصص هستیم که در زمینه ارائه خدمات و محصولات با کیفیت فعالیت می‌کنیم.</p>
            <p>هدف ما رضایت شماست.</p>
        </div>
    </div>
</div>
";
    }

    public string GetHomeContactViewTemplate()
    {
        return $@"@{{
    ViewData[""Title""] = ""تماس با ما"";
}}

<div class=""container"">
    <div class=""row"">
        <div class=""col-md-8"">
            <h2>تماس با ما</h2>
            <form method=""post"">
                <div class=""form-group mb-3"">
                    <label for=""name"">نام</label>
                    <input type=""text"" class=""form-control"" id=""name"" name=""name"" required />
                </div>
                <div class=""form-group mb-3"">
                    <label for=""email"">ایمیل</label>
                    <input type=""email"" class=""form-control"" id=""email"" name=""email"" required />
                </div>
                <div class=""form-group mb-3"">
                    <label for=""message"">پیام</label>
                    <textarea class=""form-control"" id=""message"" name=""message"" rows=""5"" required></textarea>
                </div>
                <button type=""submit"" class=""btn btn-primary"">ارسال</button>
            </form>
        </div>
        <div class=""col-md-4"">
            <h3>اطلاعات تماس</h3>
            <p><strong>آدرس:</strong> تهران، ایران</p>
            <p><strong>تلفن:</strong> 021-12345678</p>
            <p><strong>ایمیل:</strong> info@example.com</p>
        </div>
    </div>
</div>
";
    }

    public string GetProductIndexViewTemplate()
    {
        return $@"@model List<{_namespace}.Application.DTOs.Product.ProductDto>
@{{
    ViewData[""Title""] = ""محصولات"";
}}

<div class=""container"">
    <div class=""row mb-4"">
        <div class=""col-md-12"">
            <h2>محصولات</h2>
        </div>
    </div>
    
    @if (Model != null && Model.Any())
    {{
        <div class=""row"">
            @foreach (var product in Model)
            {{
                <div class=""col-md-4 mb-4"">
                    <div class=""card h-100"">
                        @if (!string.IsNullOrEmpty(product.FeaturedImageUrl))
                        {{
                            <img src=""@product.FeaturedImageUrl"" class=""card-img-top"" alt=""@product.Name"" style=""height: 200px; object-fit: cover;"">
                        }}
                        <div class=""card-body"">
                            <h5 class=""card-title"">@product.Name</h5>
                            <p class=""card-text"">@product.Summary</p>
                            <p class=""text-primary""><strong>@product.Price.ToString(""N0"") تومان</strong></p>
                            <a href=""/Product/Details/@product.Id"" class=""btn btn-primary"">مشاهده جزئیات</a>
                        </div>
                    </div>
                </div>
            }}
        </div>
    }}
    else
    {{
        <div class=""alert alert-info"">
            <p>هیچ محصولی یافت نشد.</p>
        </div>
    }}
</div>
";
    }

    public string GetProductDetailsViewTemplate()
    {
        return $@"@model {_namespace}.Application.DTOs.Product.ProductDto
@{{
    ViewData[""Title""] = Model?.Name ?? ""محصول"";
}}

<div class=""container"">
    @if (Model != null)
    {{
        <div class=""row"">
            <div class=""col-md-6"">
                @if (!string.IsNullOrEmpty(Model.FeaturedImageUrl))
                {{
                    <img src=""@Model.FeaturedImageUrl"" class=""img-fluid"" alt=""@Model.Name"">
                }}
            </div>
            <div class=""col-md-6"">
                <h2>@Model.Name</h2>
                <p class=""text-muted"">@Model.Summary</p>
                <p class=""h4 text-primary"">@Model.Price.ToString(""N0"") تومان</p>
                <p>@Model.Description</p>
                <form method=""post"" action=""/Cart/AddToCart"">
                    <input type=""hidden"" name=""productId"" value=""@Model.Id"" />
                    <div class=""form-group mb-3"">
                        <label for=""quantity"">تعداد:</label>
                        <input type=""number"" class=""form-control"" id=""quantity"" name=""quantity"" value=""1"" min=""1"" style=""width: 100px;"" />
                    </div>
                    <button type=""submit"" class=""btn btn-primary btn-lg"">افزودن به سبد خرید</button>
                </form>
            </div>
        </div>
    }}
    else
    {{
        <div class=""alert alert-danger"">
            <p>محصول یافت نشد.</p>
        </div>
    }}
</div>
";
    }

    public string GetBlogIndexViewTemplate()
    {
        return $@"@model List<{_namespace}.Application.DTOs.Blog.BlogPostDto>
@{{
    ViewData[""Title""] = ""بلاگ"";
}}

<div class=""container"">
    <div class=""row mb-4"">
        <div class=""col-md-12"">
            <h2>بلاگ</h2>
        </div>
    </div>
    
    @if (Model != null && Model.Any())
    {{
        <div class=""row"">
            @foreach (var post in Model)
            {{
                <div class=""col-md-4 mb-4"">
                    <div class=""card h-100"">
                        <div class=""card-body"">
                            <h5 class=""card-title"">@post.Title</h5>
                            <p class=""card-text"">@post.Summary</p>
                            @if (post.PublishedAt.HasValue)
                            {{
                                <p class=""text-muted""><small>@post.PublishedAt.Value.ToString(""yyyy/MM/dd"")</small></p>
                            }}
                            <a href=""/Blog/Details/@post.Slug"" class=""btn btn-primary"">ادامه مطلب</a>
                        </div>
                    </div>
                </div>
            }}
        </div>
    }}
    else
    {{
        <div class=""alert alert-info"">
            <p>هیچ پستی یافت نشد.</p>
        </div>
    }}
</div>
";
    }

    public string GetBlogDetailsViewTemplate()
    {
        return $@"@model {_namespace}.Application.DTOs.Blog.BlogPostDto
@{{
    ViewData[""Title""] = Model?.Title ?? ""پست"";
}}

<div class=""container"">
    @if (Model != null)
    {{
        <div class=""row"">
            <div class=""col-md-12"">
                <h2>@Model.Title</h2>
                @if (Model.PublishedAt.HasValue)
                {{
                    <p class=""text-muted"">@Model.PublishedAt.Value.ToString(""yyyy/MM/dd"")</p>
                }}
                <div class=""mt-4"">
                    @Html.Raw(Model.Content)
                </div>
            </div>
        </div>
    }}
    else
    {{
        <div class=""alert alert-danger"">
            <p>پست یافت نشد.</p>
        </div>
    }}
</div>
";
    }

    public string GetCartIndexViewTemplate()
    {
        return $@"@model {_namespace}.Application.DTOs.Cart.CartDto
@{{
    ViewData[""Title""] = ""سبد خرید"";
}}

<div class=""container"">
    <h2>سبد خرید</h2>
    
    @if (Model != null && Model.Items != null && Model.Items.Any())
    {{
        <table class=""table"">
            <thead>
                <tr>
                    <th>محصول</th>
                    <th>قیمت واحد</th>
                    <th>تعداد</th>
                    <th>جمع</th>
                    <th>عملیات</th>
                </tr>
            </thead>
            <tbody>
                @foreach (var item in Model.Items)
                {{
                    <tr>
                        <td>@item.ProductName</td>
                        <td>@item.UnitPrice.ToString(""N0"") تومان</td>
                        <td>@item.Quantity</td>
                        <td>@((item.UnitPrice * item.Quantity).ToString(""N0"")) تومان</td>
                        <td>
                            <form method=""post"" action=""/Cart/RemoveFromCart"" style=""display: inline;"">
                                <input type=""hidden"" name=""productId"" value=""@item.ProductId"" />
                                <button type=""submit"" class=""btn btn-danger btn-sm"">حذف</button>
                            </form>
                        </td>
                    </tr>
                }}
            </tbody>
            <tfoot>
                <tr>
                    <th colspan=""3"">جمع کل:</th>
                    <th>@Model.Items.Sum(i => i.UnitPrice * i.Quantity).ToString(""N0"") تومان</th>
                    <th></th>
                </tr>
            </tfoot>
        </table>
        <div class=""text-end mt-3"">
            <a href=""/Checkout"" class=""btn btn-primary btn-lg"">تسویه حساب</a>
        </div>
    }}
    else
    {{
        <div class=""alert alert-info"">
            <p>سبد خرید شما خالی است.</p>
            <a href=""/Product"" class=""btn btn-primary"">مشاهده محصولات</a>
        </div>
    }}
</div>
";
    }

    public string GetCheckoutIndexViewTemplate()
    {
        return $@"@model {_namespace}.Application.DTOs.Order.CreateOrderDto
@{{
    ViewData[""Title""] = ""تسویه حساب"";
}}

<div class=""container"">
    <h2>تسویه حساب</h2>
    
    <form method=""post"" action=""/Checkout/PlaceOrder"">
        <div class=""row"">
            <div class=""col-md-6"">
                <h3>اطلاعات ارسال</h3>
                <div class=""form-group mb-3"">
                    <label for=""ShippingAddress"">آدرس ارسال</label>
                    <textarea class=""form-control"" id=""ShippingAddress"" name=""ShippingAddress"" rows=""3"" required></textarea>
                </div>
                <div class=""form-group mb-3"">
                    <label for=""ShippingPhone"">تلفن تماس</label>
                    <input type=""tel"" class=""form-control"" id=""ShippingPhone"" name=""ShippingPhone"" required />
                </div>
            </div>
            <div class=""col-md-6"">
                <h3>خلاصه سفارش</h3>
                <p>جمع کل: <strong id=""totalAmount"">0</strong> تومان</p>
            </div>
        </div>
        <div class=""mt-3"">
            <button type=""submit"" class=""btn btn-primary btn-lg"">ثبت سفارش</button>
            <a href=""/Cart"" class=""btn btn-secondary"">بازگشت به سبد خرید</a>
        </div>
    </form>
</div>
";
    }

    public string GetAccountLoginViewTemplate()
    {
        return $@"@model {_projectName}.WebSite.Models.LoginViewModel
@{{
    ViewData[""Title""] = ""ورود"";
    var returnUrl = ViewData[""ReturnUrl""] as string;
}}

<div class=""login-container"" style=""min-height: 100vh; display: flex; align-items: center; background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);"">
    <div class=""container"">
        <div class=""row justify-content-center"">
            <div class=""col-lg-10"">
                <div class=""row shadow-lg"" style=""border-radius: 20px; overflow: hidden; background: white;"">
                    <!-- Left Side - Illustration -->
                    <div class=""col-lg-8 d-none d-lg-flex align-items-center justify-content-center"" style=""background: linear-gradient(135deg, #e3f2fd 0%, #ffffff 100%); padding: 60px;"">
                        <div class=""text-center"">
                            <div style=""width: 200px; height: 200px; margin: 0 auto 30px; background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); border-radius: 50%; display: flex; align-items: center; justify-content: center;"">
                                <i class=""fas fa-mobile-alt"" style=""font-size: 80px; color: white;""></i>
                            </div>
                            <h3 style=""color: #667eea; font-weight: 600;"">به سیستم خوش آمدید</h3>
                            <p style=""color: #666; margin-top: 15px;"">با وارد کردن شماره موبایل خود، کد تایید دریافت کنید</p>
                        </div>
                    </div>

                    <!-- Right Side - Login Form -->
                    <div class=""col-lg-4"" style=""background: #2c3e50; padding: 50px 40px; color: white;"">
                        <div class=""text-center mb-4"">
                            <div style=""width: 60px; height: 60px; background: white; border-radius: 12px; display: inline-flex; align-items: center; justify-content: center; margin-bottom: 20px;"">
                                <span style=""font-size: 32px; color: #2c3e50; font-weight: bold;"">آ</span>
                            </div>
                            <h2 style=""font-weight: 600; margin-bottom: 10px;"">ورود</h2>
                            <p style=""color: #bdc3c7; font-size: 14px;"">لطفا شماره موبایل خود را وارد کنید</p>
                        </div>

                        @if (TempData[""Message""] != null)
                        {{
                            <div class=""alert alert-success alert-dismissible fade show"" role=""alert"">
                                @TempData[""Message""]
                                <button type=""button"" class=""btn-close"" data-bs-dismiss=""alert""></button>
                            </div>
                        }}

                        @if (!Model.ShowOtpInput)
                        {{
                            <!-- Phone Number Input -->
                            <form method=""post"" asp-action=""SendOtp"">
                                <input type=""hidden"" name=""returnUrl"" value=""@returnUrl"" />
                                <div asp-validation-summary=""All"" class=""text-danger mb-3""></div>
                                
                                <div class=""mb-3"">
                                    <label asp-for=""PhoneNumber"" class=""form-label"">شماره موبایل</label>
                                    <div class=""input-group"">
                                        <span class=""input-group-text"" style=""background: #34495e; border: none; color: white;"">
                                            <i class=""fas fa-phone""></i>
                                        </span>
                                        <input asp-for=""PhoneNumber"" class=""form-control"" placeholder=""۰۹۱۲۳۴۵۶۷۸۹"" style=""background: #34495e; border: none; color: white; direction: ltr; text-align: left;"" />
                                    </div>
                                    <span asp-validation-for=""PhoneNumber"" class=""text-danger""></span>
                                </div>

                                <div class=""form-check mb-4"">
                                    <input asp-for=""AgreeToTerms"" class=""form-check-input"" type=""checkbox"" id=""agreeTerms"" required />
                                    <label class=""form-check-label"" for=""agreeTerms"" style=""font-size: 13px; color: #bdc3c7;"">
                                        با <a href=""#"" style=""color: #3498db; text-decoration: none;"">قوانین و مقررات</a> موافقم
                                    </label>
                                    <span asp-validation-for=""AgreeToTerms"" class=""text-danger d-block""></span>
                                </div>

                                <button type=""submit"" class=""btn w-100"" style=""background: #3498db; color: white; padding: 12px; border-radius: 8px; font-weight: 500; border: none;"">
                                    ورود
                                </button>
                            </form>
                        }}
                        else
                        {{
                            <!-- OTP Input -->
                            <form method=""post"" asp-action=""VerifyOtp"">
                                <input type=""hidden"" name=""returnUrl"" value=""@returnUrl"" />
                                <input type=""hidden"" asp-for=""PhoneNumber"" />
                                <div asp-validation-summary=""All"" class=""text-danger mb-3""></div>
                                
                                <div class=""mb-3"">
                                    <label class=""form-label"">کد تایید</label>
                                    <input asp-for=""Otp"" class=""form-control text-center"" placeholder=""کد ۶ رقمی"" maxlength=""6"" style=""background: #34495e; border: none; color: white; font-size: 24px; letter-spacing: 8px; direction: ltr;"" />
                                    <span asp-validation-for=""Otp"" class=""text-danger""></span>
                                    <small class=""text-muted d-block mt-2"" style=""color: #95a5a6 !important;"">کد تایید به شماره @Model.PhoneNumber ارسال شد</small>
                                    @{{
                                        var env = Context.RequestServices.GetRequiredService<Microsoft.AspNetCore.Hosting.IWebHostEnvironment>();
                                        if (env.IsDevelopment())
                                        {{
                                            <div class=""alert alert-info mt-2"" style=""background: #17a2b8; color: white; border: none; font-size: 12px; padding: 8px; border-radius: 4px;"">
                                                <i class=""fas fa-info-circle""></i> <strong>Development Mode:</strong> کد تایید در پیام بالا نمایش داده شده است.
                                            </div>
                                        }}
                                    }}
                                </div>

                                <div class=""form-check mb-4"">
                                    <input asp-for=""RememberMe"" class=""form-check-input"" type=""checkbox"" id=""rememberMe"" />
                                    <label class=""form-check-label"" for=""rememberMe"" style=""font-size: 13px; color: #bdc3c7;"">
                                        مرا به خاطر بسپار
                                    </label>
                                </div>

                                <button type=""submit"" class=""btn w-100 mb-3"" style=""background: #3498db; color: white; padding: 12px; border-radius: 8px; font-weight: 500; border: none;"">
                                    تایید و ورود
                                </button>

                                <form method=""post"" asp-action=""SendOtp"" class=""d-inline"">
                                    <input type=""hidden"" name=""returnUrl"" value=""@returnUrl"" />
                                    <input type=""hidden"" asp-for=""PhoneNumber"" />
                                    <button type=""submit"" class=""btn btn-link w-100"" style=""color: #3498db; text-decoration: none; font-size: 14px;"">
                                        ارسال مجدد کد
                                    </button>
                                </form>
                            </form>
                        }}

                        <div class=""text-center mt-4"" style=""border-top: 1px solid #34495e; padding-top: 20px;"">
                            <p style=""color: #95a5a6; font-size: 13px; margin: 0;"">
                                با ورود و ثبت نام شرایط ما را قبول می‌کنید.
                                @if (!Model.ShowOtpInput)
                                {{
                                    <span>اگر اکانت کاربری ندارید؟ <a href=""#"" onclick=""document.querySelector('form').submit(); return false;"" style=""color: #3498db; text-decoration: none;"">ثبت نام کنید</a></span>
                                }}
                            </p>
                        </div>
                    </div>
                </div>
            </div>
        </div>
    </div>
</div>

@section Scripts {{
    <script type=""text/javascript"">
        // Auto-focus on OTP input
        @if (Model.ShowOtpInput)
        {{
            <text>
            document.addEventListener('DOMContentLoaded', function() {{{{
                var otpInput = document.querySelector('input[name=""Otp""]');
                if (otpInput) otpInput.focus();
            }}}});
            </text>
        }}

        // Format phone number input
        document.addEventListener('DOMContentLoaded', function() {{{{
            var phoneInput = document.querySelector('input[name=""PhoneNumber""]');
            if (phoneInput) {{{{
                phoneInput.addEventListener('input', function(e) {{{{
                    var value = e.target.value.replace(/[^0-9]/g, '');
                    if (value.length > 0 && value.indexOf('0') !== 0) {{{{
                        value = '0' + value;
                    }}}}
                    e.target.value = value;
                }}}});
            }}}}
        }}}}); 
    </script>
}}
";
    }

    public string GetAccountRegisterViewTemplate()
    {
        return $@"@model {_projectName}.WebSite.Models.RegisterViewModel
@{{
    ViewData[""Title""] = ""ثبت نام"";
}}

<div class=""container"">
    <div class=""row justify-content-center"">
        <div class=""col-md-6"">
            <h2>ثبت نام</h2>
            <form method=""post"" asp-action=""Register"">
                <div class=""form-group mb-3"">
                    <label asp-for=""Username""></label>
                    <input asp-for=""Username"" class=""form-control"" />
                    <span asp-validation-for=""Username"" class=""text-danger""></span>
                </div>
                <div class=""form-group mb-3"">
                    <label asp-for=""Email""></label>
                    <input asp-for=""Email"" class=""form-control"" type=""email"" />
                    <span asp-validation-for=""Email"" class=""text-danger""></span>
                </div>
                <div class=""form-group mb-3"">
                    <label asp-for=""PhoneNumber""></label>
                    <input asp-for=""PhoneNumber"" class=""form-control"" />
                    <span asp-validation-for=""PhoneNumber"" class=""text-danger""></span>
                </div>
                <div class=""form-group mb-3"">
                    <label asp-for=""Password""></label>
                    <input asp-for=""Password"" class=""form-control"" type=""password"" />
                    <span asp-validation-for=""Password"" class=""text-danger""></span>
                </div>
                <div class=""form-group mb-3"">
                    <label asp-for=""ConfirmPassword""></label>
                    <input asp-for=""ConfirmPassword"" class=""form-control"" type=""password"" />
                    <span asp-validation-for=""ConfirmPassword"" class=""text-danger""></span>
                </div>
                <button type=""submit"" class=""btn btn-primary"">ثبت نام</button>
                <a asp-action=""Login"" class=""btn btn-link"">ورود</a>
            </form>
        </div>
    </div>
</div>
";
    }

    // ==================== Admin Area Views ====================
    
    public string GetAdminUsersIndexViewTemplate()
    {
        return $@"@model List<{_namespace}.Domain.Entities.ApplicationUser>
@{{
    ViewData[""Title""] = ""مدیریت کاربران"";
}}

<div class=""container-fluid"">
    <div class=""row mb-3"">
        <div class=""col"">
            <h2>مدیریت کاربران</h2>
        </div>
        <div class=""col-auto"">
            <a asp-action=""Create"" class=""btn btn-primary"">افزودن کاربر جدید</a>
        </div>
    </div>
    
    <table class=""table table-striped"">
        <thead>
            <tr>
                <th>نام کاربری</th>
                <th>ایمیل</th>
                <th>تلفن</th>
                <th>وضعیت</th>
                <th>عملیات</th>
            </tr>
        </thead>
        <tbody>
            @foreach (var user in Model)
            {{
                <tr>
                    <td>@user.UserName</td>
                    <td>@user.Email</td>
                    <td>@user.PhoneNumber</td>
                    <td>
                        @if (user.IsActive)
                        {{
                            <span class=""badge bg-success"">فعال</span>
                        }}
                        else
                        {{
                            <span class=""badge bg-danger"">غیرفعال</span>
                        }}
                    </td>
                    <td>
                        <a asp-action=""Edit"" asp-route-id=""@user.Id"" class=""btn btn-sm btn-warning"">ویرایش</a>
                        <a asp-action=""Delete"" asp-route-id=""@user.Id"" class=""btn btn-sm btn-danger"">حذف</a>
                    </td>
                </tr>
            }}
        </tbody>
    </table>
</div>
";
    }

    public string GetAdminUsersCreateViewTemplate()
    {
        return $@"@model {_projectName}.WebSite.Areas.Admin.Models.CreateUserViewModel
@{{
    ViewData[""Title""] = ""افزودن کاربر جدید"";
}}

<div class=""container-fluid"">
    <h2>افزودن کاربر جدید</h2>
    
    <form method=""post"" asp-action=""Create"">
        <div class=""form-group mb-3"">
            <label asp-for=""Username""></label>
            <input asp-for=""Username"" class=""form-control"" />
            <span asp-validation-for=""Username"" class=""text-danger""></span>
        </div>
        <div class=""form-group mb-3"">
            <label asp-for=""Email""></label>
            <input asp-for=""Email"" class=""form-control"" type=""email"" />
            <span asp-validation-for=""Email"" class=""text-danger""></span>
        </div>
        <div class=""form-group mb-3"">
            <label asp-for=""PhoneNumber""></label>
            <input asp-for=""PhoneNumber"" class=""form-control"" />
            <span asp-validation-for=""PhoneNumber"" class=""text-danger""></span>
        </div>
        <div class=""form-group mb-3"">
            <label asp-for=""Password""></label>
            <input asp-for=""Password"" class=""form-control"" type=""password"" />
            <span asp-validation-for=""Password"" class=""text-danger""></span>
        </div>
        <button type=""submit"" class=""btn btn-primary"">ذخیره</button>
        <a asp-action=""Index"" class=""btn btn-secondary"">بازگشت</a>
    </form>
</div>
";
    }

    public string GetAdminUsersEditViewTemplate()
    {
        return $@"@model {_projectName}.WebSite.Areas.Admin.Models.EditUserViewModel
@{{
    ViewData[""Title""] = ""ویرایش کاربر"";
}}

<div class=""container-fluid"">
    <h2>ویرایش کاربر</h2>
    
    <form method=""post"" asp-action=""Edit"">
        <input type=""hidden"" asp-for=""Id"" />
        <div class=""form-group mb-3"">
            <label asp-for=""Username""></label>
            <input asp-for=""Username"" class=""form-control"" />
            <span asp-validation-for=""Username"" class=""text-danger""></span>
        </div>
        <div class=""form-group mb-3"">
            <label asp-for=""Email""></label>
            <input asp-for=""Email"" class=""form-control"" type=""email"" />
            <span asp-validation-for=""Email"" class=""text-danger""></span>
        </div>
        <div class=""form-group mb-3"">
            <label asp-for=""PhoneNumber""></label>
            <input asp-for=""PhoneNumber"" class=""form-control"" />
            <span asp-validation-for=""PhoneNumber"" class=""text-danger""></span>
        </div>
        <button type=""submit"" class=""btn btn-primary"">ذخیره</button>
        <a asp-action=""Index"" class=""btn btn-secondary"">بازگشت</a>
    </form>
</div>
";
    }

    public string GetAdminProductsIndexViewTemplate()
    {
        return $@"@model List<{_namespace}.Application.DTOs.Product.ProductDto>
@{{
    ViewData[""Title""] = ""مدیریت محصولات"";
}}

<div class=""container-fluid"">
    <div class=""row mb-3"">
        <div class=""col"">
            <h2>مدیریت محصولات</h2>
        </div>
        <div class=""col-auto"">
            <a asp-action=""Create"" class=""btn btn-primary"">افزودن محصول جدید</a>
        </div>
    </div>
    
    <table class=""table table-striped"">
        <thead>
            <tr>
                <th>نام</th>
                <th>قیمت</th>
                <th>وضعیت</th>
                <th>عملیات</th>
            </tr>
        </thead>
        <tbody>
            @if (Model != null && Model.Any())
            {{
                @foreach (var product in Model)
                {{
                    <tr>
                        <td>@product.Name</td>
                        <td>@product.Price.ToString(""N0"") تومان</td>
                        <td>
                            @if (product.IsPublished)
                            {{
                                <span class=""badge bg-success"">منتشر شده</span>
                            }}
                            else
                            {{
                                <span class=""badge bg-warning"">پیش‌نویس</span>
                            }}
                        </td>
                        <td>
                            <a asp-action=""Edit"" asp-route-id=""@product.Id"" class=""btn btn-sm btn-warning"">ویرایش</a>
                            <a asp-action=""Delete"" asp-route-id=""@product.Id"" class=""btn btn-sm btn-danger"">حذف</a>
                        </td>
                    </tr>
                }}
            }}
        </tbody>
    </table>
</div>
";
    }

    public string GetAdminProductsCreateViewTemplate()
    {
        return $@"@model {_namespace}.Application.DTOs.Product.CreateProductDto
@{{
    ViewData[""Title""] = ""افزودن محصول جدید"";
}}

<div class=""container-fluid"">
    <h2>افزودن محصول جدید</h2>
    
    <form method=""post"" asp-action=""Create"" enctype=""multipart/form-data"">
        <div class=""form-group mb-3"">
            <label asp-for=""Name""></label>
            <input asp-for=""Name"" class=""form-control"" />
            <span asp-validation-for=""Name"" class=""text-danger""></span>
        </div>
        <div class=""form-group mb-3"">
            <label asp-for=""Summary""></label>
            <input asp-for=""Summary"" class=""form-control"" />
            <span asp-validation-for=""Summary"" class=""text-danger""></span>
        </div>
        <div class=""form-group mb-3"">
            <label asp-for=""Description""></label>
            <textarea asp-for=""Description"" class=""form-control"" rows=""5""></textarea>
            <span asp-validation-for=""Description"" class=""text-danger""></span>
        </div>
        <div class=""form-group mb-3"">
            <label asp-for=""Price""></label>
            <input asp-for=""Price"" class=""form-control"" type=""number"" step=""0.01"" />
            <span asp-validation-for=""Price"" class=""text-danger""></span>
        </div>
        <div class=""form-group mb-3"">
            <div class=""form-check"">
                <input asp-for=""IsPublished"" class=""form-check-input"" />
                <label asp-for=""IsPublished"" class=""form-check-label"">منتشر شده</label>
            </div>
        </div>
        <button type=""submit"" class=""btn btn-primary"">ذخیره</button>
        <a asp-action=""Index"" class=""btn btn-secondary"">بازگشت</a>
    </form>
</div>
";
    }

    public string GetAdminProductsEditViewTemplate()
    {
        return $@"@model {_namespace}.Application.DTOs.Product.UpdateProductDto
@{{
    ViewData[""Title""] = ""ویرایش محصول"";
}}

<div class=""container-fluid"">
    <h2>ویرایش محصول</h2>
    
    <form method=""post"" asp-action=""Edit"" asp-route-id=""@ViewBag.ProductId"">
        <div class=""form-group mb-3"">
            <label asp-for=""Name""></label>
            <input asp-for=""Name"" class=""form-control"" />
            <span asp-validation-for=""Name"" class=""text-danger""></span>
        </div>
        <div class=""form-group mb-3"">
            <label asp-for=""Summary""></label>
            <input asp-for=""Summary"" class=""form-control"" />
            <span asp-validation-for=""Summary"" class=""text-danger""></span>
        </div>
        <div class=""form-group mb-3"">
            <label asp-for=""Description""></label>
            <textarea asp-for=""Description"" class=""form-control"" rows=""5""></textarea>
            <span asp-validation-for=""Description"" class=""text-danger""></span>
        </div>
        <div class=""form-group mb-3"">
            <label asp-for=""Price""></label>
            <input asp-for=""Price"" class=""form-control"" type=""number"" step=""0.01"" />
            <span asp-validation-for=""Price"" class=""text-danger""></span>
        </div>
        <div class=""form-group mb-3"">
            <div class=""form-check"">
                <input asp-for=""IsPublished"" class=""form-check-input"" />
                <label asp-for=""IsPublished"" class=""form-check-label"">منتشر شده</label>
            </div>
        </div>
        <button type=""submit"" class=""btn btn-primary"">ذخیره</button>
        <a asp-action=""Index"" class=""btn btn-secondary"">بازگشت</a>
    </form>
</div>
";
    }

    public string GetAdminCategoriesIndexViewTemplate()
    {
        return $@"@model List<{_namespace}.Application.DTOs.Category.CategoryDto>
@{{
    ViewData[""Title""] = ""مدیریت دسته‌بندی‌ها"";
}}

<div class=""container-fluid"">
    <div class=""row mb-3"">
        <div class=""col"">
            <h2>مدیریت دسته‌بندی‌ها</h2>
        </div>
        <div class=""col-auto"">
            <a asp-action=""Create"" class=""btn btn-primary"">افزودن دسته‌بندی جدید</a>
        </div>
    </div>
    
    <table class=""table table-striped"">
        <thead>
            <tr>
                <th>نام</th>
                <th>Slug</th>
                <th>عملیات</th>
            </tr>
        </thead>
        <tbody>
            @if (Model != null && Model.Any())
            {{
                @foreach (var category in Model)
                {{
                    <tr>
                        <td>@category.Name</td>
                        <td>@category.Slug</td>
                        <td>
                            <a asp-action=""Edit"" asp-route-id=""@category.Id"" class=""btn btn-sm btn-warning"">ویرایش</a>
                            <a asp-action=""Delete"" asp-route-id=""@category.Id"" class=""btn btn-sm btn-danger"">حذف</a>
                        </td>
                    </tr>
                }}
            }}
        </tbody>
    </table>
</div>
";
    }

    public string GetAdminCategoriesCreateViewTemplate()
    {
        return $@"@model {_namespace}.Application.DTOs.Category.CreateCategoryDto
@{{
    ViewData[""Title""] = ""افزودن دسته‌بندی جدید"";
}}

<div class=""container-fluid"">
    <h2>افزودن دسته‌بندی جدید</h2>
    
    <form method=""post"" asp-action=""Create"">
        <div class=""form-group mb-3"">
            <label asp-for=""Name""></label>
            <input asp-for=""Name"" class=""form-control"" />
            <span asp-validation-for=""Name"" class=""text-danger""></span>
        </div>
        <div class=""form-group mb-3"">
            <label asp-for=""Slug""></label>
            <input asp-for=""Slug"" class=""form-control"" />
            <span asp-validation-for=""Slug"" class=""text-danger""></span>
        </div>
        <div class=""form-group mb-3"">
            <label asp-for=""Description""></label>
            <textarea asp-for=""Description"" class=""form-control"" rows=""3""></textarea>
            <span asp-validation-for=""Description"" class=""text-danger""></span>
        </div>
        <button type=""submit"" class=""btn btn-primary"">ذخیره</button>
        <a asp-action=""Index"" class=""btn btn-secondary"">بازگشت</a>
    </form>
</div>
";
    }

    public string GetAdminOrdersIndexViewTemplate()
    {
        return $@"@model List<{_namespace}.Application.DTOs.Order.OrderDto>
@{{
    ViewData[""Title""] = ""مدیریت سفارشات"";
}}

<div class=""container-fluid"">
    <h2>مدیریت سفارشات</h2>
    
    <table class=""table table-striped"">
        <thead>
            <tr>
                <th>شماره سفارش</th>
                <th>کاربر</th>
                <th>مبلغ کل</th>
                <th>وضعیت</th>
                <th>تاریخ</th>
                <th>عملیات</th>
            </tr>
        </thead>
        <tbody>
            @if (Model != null && Model.Any())
            {{
                @foreach (var order in Model)
                {{
                    <tr>
                        <td>#@order.Id</td>
                        <td>@order.UserId</td>
                        <td>@order.TotalAmount.ToString(""N0"") تومان</td>
                        <td><span class=""badge bg-info"">@order.Status</span></td>
                        <td>@order.CreatedAt.ToString(""yyyy/MM/dd"")</td>
                        <td>
                            <a asp-action=""Details"" asp-route-id=""@order.Id"" class=""btn btn-sm btn-primary"">جزئیات</a>
                        </td>
                    </tr>
                }}
            }}
        </tbody>
    </table>
</div>
";
    }

    public string GetAdminOrdersDetailsViewTemplate()
    {
        return $@"@model {_namespace}.Application.DTOs.Order.OrderDto
@{{
    ViewData[""Title""] = ""جزئیات سفارش"";
}}

<div class=""container-fluid"">
    <h2>جزئیات سفارش #@Model.Id</h2>
    
    <div class=""row"">
        <div class=""col-md-6"">
            <h4>اطلاعات سفارش</h4>
            <p><strong>کاربر:</strong> @Model.UserId</p>
            <p><strong>وضعیت:</strong> @Model.Status</p>
            <p><strong>تاریخ:</strong> @Model.CreatedAt.ToString(""yyyy/MM/dd HH:mm"")</p>
            <p><strong>مبلغ کل:</strong> @Model.TotalAmount.ToString(""N0"") تومان</p>
        </div>
        <div class=""col-md-6"">
            <h4>آیتم‌های سفارش</h4>
            <table class=""table"">
                <thead>
                    <tr>
                        <th>محصول</th>
                        <th>تعداد</th>
                        <th>قیمت واحد</th>
                        <th>جمع</th>
                    </tr>
                </thead>
                <tbody>
                    @foreach (var item in Model.Items)
                    {{
                        <tr>
                            <td>@item.ProductName</td>
                            <td>@item.Quantity</td>
                            <td>@item.UnitPrice.ToString(""N0"") تومان</td>
                            <td>@((item.UnitPrice * item.Quantity).ToString(""N0"")) تومان</td>
                        </tr>
                    }}
                </tbody>
            </table>
        </div>
    </div>
    
    <a asp-action=""Index"" class=""btn btn-secondary"">بازگشت</a>
</div>
";
    }

    public string GetAdminBlogsIndexViewTemplate()
    {
        return $@"@model List<{_namespace}.Application.DTOs.Blog.BlogPostDto>
@{{
    ViewData[""Title""] = ""مدیریت بلاگ"";
}}

<div class=""container-fluid"">
    <div class=""row mb-3"">
        <div class=""col"">
            <h2>مدیریت بلاگ</h2>
        </div>
        <div class=""col-auto"">
            <a asp-action=""Create"" class=""btn btn-primary"">افزودن پست جدید</a>
        </div>
    </div>
    
    <table class=""table table-striped"">
        <thead>
            <tr>
                <th>عنوان</th>
                <th>Slug</th>
                <th>وضعیت</th>
                <th>تاریخ انتشار</th>
                <th>عملیات</th>
            </tr>
        </thead>
        <tbody>
            @if (Model != null && Model.Any())
            {{
                @foreach (var post in Model)
                {{
                    <tr>
                        <td>@post.Title</td>
                        <td>@post.Slug</td>
                        <td>
                            @if (post.IsPublished)
                            {{
                                <span class=""badge bg-success"">منتشر شده</span>
                            }}
                            else
                            {{
                                <span class=""badge bg-warning"">پیش‌نویس</span>
                            }}
                        </td>
                        <td>@(post.PublishedAt?.ToString(""yyyy/MM/dd"") ?? ""-"")</td>
                        <td>
                            <a asp-action=""Edit"" asp-route-id=""@post.Id"" class=""btn btn-sm btn-warning"">ویرایش</a>
                            <a asp-action=""Delete"" asp-route-id=""@post.Id"" class=""btn btn-sm btn-danger"">حذف</a>
                        </td>
                    </tr>
                }}
            }}
        </tbody>
    </table>
</div>
";
    }

    public string GetAdminBlogsCreateViewTemplate()
    {
        return $@"@model {_namespace}.Application.DTOs.Blog.CreateBlogDto
@{{
    ViewData[""Title""] = ""افزودن پست جدید"";
}}

<div class=""container-fluid"">
    <h2>افزودن پست جدید</h2>
    
    <form method=""post"" asp-action=""Create"">
        <div class=""form-group mb-3"">
            <label asp-for=""Title""></label>
            <input asp-for=""Title"" class=""form-control"" />
            <span asp-validation-for=""Title"" class=""text-danger""></span>
        </div>
        <div class=""form-group mb-3"">
            <label asp-for=""Slug""></label>
            <input asp-for=""Slug"" class=""form-control"" />
            <span asp-validation-for=""Slug"" class=""text-danger""></span>
        </div>
        <div class=""form-group mb-3"">
            <label asp-for=""Summary""></label>
            <textarea asp-for=""Summary"" class=""form-control"" rows=""3""></textarea>
            <span asp-validation-for=""Summary"" class=""text-danger""></span>
        </div>
        <div class=""form-group mb-3"">
            <label asp-for=""Content""></label>
            <textarea asp-for=""Content"" class=""form-control"" rows=""10""></textarea>
            <span asp-validation-for=""Content"" class=""text-danger""></span>
        </div>
        <div class=""form-group mb-3"">
            <div class=""form-check"">
                <input asp-for=""IsPublished"" class=""form-check-input"" />
                <label asp-for=""IsPublished"" class=""form-check-label"">منتشر شده</label>
            </div>
        </div>
        <button type=""submit"" class=""btn btn-primary"">ذخیره</button>
        <a asp-action=""Index"" class=""btn btn-secondary"">بازگشت</a>
    </form>
</div>
";
    }

    // ==================== ViewModel Templates ====================
    
    public string GetLoginViewModelTemplate()
    {
        return $@"namespace {_projectName}.WebSite.Models;

public class LoginViewModel
{{
    public string PhoneNumber {{ get; set; }} = string.Empty;
    public string Otp {{ get; set; }} = string.Empty;
    public bool RememberMe {{ get; set; }}
    public bool ShowOtpInput {{ get; set; }} = false;
    public bool AgreeToTerms {{ get; set; }}
}}
";
    }

    public string GetRegisterViewModelTemplate()
    {
        return $@"namespace {_projectName}.WebSite.Models;

// RegisterViewModel is no longer needed as registration is handled in Login flow
// Keeping this for backward compatibility if needed
public class RegisterViewModel
{{
    public string PhoneNumber {{ get; set; }} = string.Empty;
}}
";
    }

    public string GetCreateUserViewModelTemplate()
    {
        return $@"namespace {_projectName}.WebSite.Areas.Admin.Models;

public class CreateUserViewModel
{{
    public string Username {{ get; set; }} = string.Empty;
    public string Email {{ get; set; }} = string.Empty;
    public string PhoneNumber {{ get; set; }} = string.Empty;
    public string Password {{ get; set; }} = string.Empty;
}}
";
    }

    public string GetEditUserViewModelTemplate()
    {
        return $@"namespace {_projectName}.WebSite.Areas.Admin.Models;

public class EditUserViewModel
{{
    public string Id {{ get; set; }} = string.Empty;
    public string Username {{ get; set; }} = string.Empty;
    public string Email {{ get; set; }} = string.Empty;
    public string PhoneNumber {{ get; set; }} = string.Empty;
}}
";
    }

    public string GetProfileEditViewModelTemplate()
    {
        return $@"namespace {_projectName}.WebSite.Areas.User.Models;

public class ProfileEditViewModel
{{
    public string Email {{ get; set; }} = string.Empty;
    public string PhoneNumber {{ get; set; }} = string.Empty;
}}
";
    }
}
