using EndPoint.WebSite.Areas.Admin.Models.Organizations;
using Arsis.Domain.Entities;
using Arsis.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using DomainOrganization = Arsis.Domain.Entities.Organization;
using DomainOrganizationStatus = Arsis.Domain.Entities.OrganizationStatus;

namespace EndPoint.WebSite.Areas.Admin.Controllers;

[Area("Admin")]
public class OrganizationsController : Controller
{
    private const int DefaultPageSize = 10;
    private readonly AppDbContext _context;

    public OrganizationsController(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index([FromQuery] OrganizationIndexRequest request)
    {
        ViewData["Title"] = "مدیریت سازمان‌ها";
        request ??= new OrganizationIndexRequest();

        var page = Math.Max(1, request.Page.GetValueOrDefault(1));
        var pageSize = Math.Min(request.PageSize.GetValueOrDefault(DefaultPageSize), 50);

        // Build query
        var query = _context.Organizations
            .Where(o => !o.IsDeleted)
            .AsQueryable();

        // Apply filters
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            query = query.Where(o =>
                o.Name.Contains(request.Search) ||
                o.Code.Contains(request.Search) ||
                o.AdminName.Contains(request.Search) ||
                o.AdminEmail.Contains(request.Search));
        }

        if (request.Status.HasValue)
        {
            query = query.Where(o => o.Status == request.Status.Value);
        }

        // Get total count
        var totalCount = await query.CountAsync();

        // Apply pagination and ordering
        var organizations = await query
            .OrderByDescending(o => o.CreateDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(o => new OrganizationListItem
            {
                Id = o.Id,
                Name = o.Name,
                Code = o.Code,
                AdminName = o.AdminName,
                AdminEmail = o.AdminEmail,
                UserCount = 0, // TODO: Calculate actual user count
                ActiveTests = 0, // TODO: Calculate actual active tests
                Status = o.Status,
                CreatedAt = o.CreateDate.LocalDateTime
            })
            .ToListAsync();

        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        var viewModel = new OrganizationIndexViewModel
        {
            Organizations = organizations,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
            TotalPages = totalPages,
            Search = request.Search,
            Status = request.Status,
            StatusOptions = GetStatusOptions()
        };

        return View(viewModel);
    }

    public IActionResult Create()
    {
        ViewData["Title"] = "ایجاد سازمان جدید";

        var viewModel = new CreateOrganizationViewModel
        {
            StatusOptions = GetStatusOptions()
        };

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateOrganizationViewModel model)
    {
        if (!ModelState.IsValid)
        {
            model.StatusOptions = GetStatusOptions();
            return View(model);
        }

        // Check if code already exists
        var existingOrg = await _context.Organizations
            .FirstOrDefaultAsync(o => o.Code == model.Code && !o.IsDeleted);

        if (existingOrg != null)
        {
            ModelState.AddModelError("Code", "کد سازمان تکراری است.");
            model.StatusOptions = GetStatusOptions();
            return View(model);
        }

        var organization = new DomainOrganization(model.Name, model.Code, model.AdminName, model.AdminEmail);

        organization.UpdateDetails(
            model.Name,
            model.Description,
            model.AdminName,
            model.AdminEmail,
            model.PhoneNumber,
            model.Address
        );

        organization.UpdateStatus((DomainOrganizationStatus)model.Status);

        organization.UpdateSubscription(
            model.MaxUsers ?? 100,
            model.SubscriptionExpiry.HasValue ? DateTimeOffset.Parse(model.SubscriptionExpiry.Value.ToString()) : null
        );

        _context.Organizations.Add(organization);
        await _context.SaveChangesAsync();

        TempData["Success"] = "سازمان با موفقیت ایجاد شد.";

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(Guid id)
    {
        ViewData["Title"] = "ویرایش سازمان";

        var organization = await _context.Organizations
            .FirstOrDefaultAsync(o => o.Id == id && !o.IsDeleted);

        if (organization == null)
        {
            return NotFound();
        }

        var viewModel = new EditOrganizationViewModel
        {
            Id = organization.Id,
            Name = organization.Name,
            Code = organization.Code,
            Description = organization.Description,
            AdminName = organization.AdminName,
            AdminEmail = organization.AdminEmail,
            PhoneNumber = organization.PhoneNumber,
            Address = organization.Address,
            Status = (DomainOrganizationStatus)organization.Status,
            MaxUsers = organization.MaxUsers,
            SubscriptionExpiry = organization.SubscriptionExpiry?.LocalDateTime,
            StatusOptions = GetStatusOptions()
        };

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(EditOrganizationViewModel model)
    {
        if (!ModelState.IsValid)
        {
            model.StatusOptions = GetStatusOptions();
            return View(model);
        }

        var organization = await _context.Organizations
            .FirstOrDefaultAsync(o => o.Id == model.Id && !o.IsDeleted);

        if (organization == null)
        {
            return NotFound();
        }

        // Check if code already exists (excluding current organization)
        var existingOrg = await _context.Organizations
            .FirstOrDefaultAsync(o => o.Code == model.Code && o.Id != model.Id && !o.IsDeleted);

        if (existingOrg != null)
        {
            ModelState.AddModelError("Code", "کد سازمان تکراری است.");
            model.StatusOptions = GetStatusOptions();
            return View(model);
        }

        organization.UpdateDetails(
            model.Name,
            model.Description ?? string.Empty,
            model.AdminName,
            model.AdminEmail,
            model.PhoneNumber,
            model.Address
        );

        organization.UpdateStatus(model.Status);

        organization.UpdateSubscription(
            model.MaxUsers ?? 100,
            model.SubscriptionExpiry.HasValue ? DateTimeOffset.Parse(model.SubscriptionExpiry.Value.ToString()) : null
        );

        await _context.SaveChangesAsync();

        TempData["Success"] = "سازمان با موفقیت ویرایش شد.";

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id)
    {
        var organization = await _context.Organizations
            .FirstOrDefaultAsync(o => o.Id == id && !o.IsDeleted);

        if (organization == null)
        {
            return NotFound();
        }

        // Soft delete
        organization.RemoveDate = DateTimeOffset.UtcNow;
        organization.IsDeleted = true;

        await _context.SaveChangesAsync();

        TempData["Success"] = "سازمان با موفقیت حذف شد.";

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Details(Guid id)
    {
        ViewData["Title"] = "جزئیات سازمان";

        var organization = await _context.Organizations
            .FirstOrDefaultAsync(o => o.Id == id && !o.IsDeleted);

        if (organization == null)
        {
            return NotFound();
        }

        var viewModel = new OrganizationDetailsViewModel
        {
            Organization = new OrganizationDetailItem
            {
                Id = organization.Id,
                Name = organization.Name,
                Code = organization.Code,
                Description = organization.Description,
                AdminName = organization.AdminName,
                AdminEmail = organization.AdminEmail,
                PhoneNumber = organization.PhoneNumber,
                Address = organization.Address,
                UserCount = 0, // TODO: Calculate actual user count
                ActiveTests = 0, // TODO: Calculate actual active tests
                CompletedTests = 0, // TODO: Calculate actual completed tests
                Status = (DomainOrganizationStatus)organization.Status,
                MaxUsers = organization.MaxUsers,
                SubscriptionExpiry = organization.SubscriptionExpiry?.LocalDateTime,
                CreatedAt = organization.CreateDate.LocalDateTime,
                LastActivity = organization.UpdateDate != default ? organization.UpdateDate.LocalDateTime : organization.CreateDate.LocalDateTime
            },
            RecentTests = new List<TestSummary>() // TODO: Get actual recent tests
        };

        return View(viewModel);
    }

    private IEnumerable<SelectListItem> GetStatusOptions()
    {
        return new[]
        {
            new SelectListItem { Value = "Active", Text = "فعال" },
            new SelectListItem { Value = "Inactive", Text = "غیرفعال" },
            new SelectListItem { Value = "Suspended", Text = "معلق" }
        };
    }
}
