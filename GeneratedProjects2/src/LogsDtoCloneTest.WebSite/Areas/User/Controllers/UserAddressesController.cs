using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using LogsDtoCloneTest.Application.Commands.UserAddresses;
using LogsDtoCloneTest.Application.Queries.UserAddresses;
using LogsDtoCloneTest.WebSite.Areas.User.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LogsDtoCloneTest.WebSite.Areas.User.Controllers;

[Area("User")]
[Authorize]
public sealed class UserAddressesController : Controller
{
    private readonly IMediator _mediator;

    public UserAddressesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Challenge();
        }

        ViewData["Title"] = "مدیریت آدرس‌ها";
        ViewData["Sidebar:ActiveTab"] = "addresses";

        var query = new GetUserAddressesQuery(userId);
        var result = await _mediator.Send(query, cancellationToken);

        if (!result.IsSuccess)
        {
            TempData["Error"] = result.Error ?? "خطا در دریافت آدرس‌ها.";
            return View(new UserAddressesViewModel { Addresses = Array.Empty<UserAddressViewModel>() });
        }

        var viewModel = new UserAddressesViewModel
        {
            Addresses = result.Value.Select(a => new UserAddressViewModel
            {
                Id = a.Id,
                Title = a.Title,
                RecipientName = a.RecipientName,
                RecipientPhone = a.RecipientPhone,
                Province = a.Province,
                City = a.City,
                PostalCode = a.PostalCode,
                AddressLine = a.AddressLine,
                Plaque = a.Plaque,
                Unit = a.Unit,
                IsDefault = a.IsDefault
            }).ToList()
        };

        return View(viewModel);
    }

    [HttpGet]
    public IActionResult Create()
    {
        ViewData["Title"] = "افزودن آدرس جدید";
        ViewData["Sidebar:ActiveTab"] = "addresses";
        return View(new UserAddressFormViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(UserAddressFormViewModel model, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Challenge();
        }

        ViewData["Title"] = "افزودن آدرس جدید";
        ViewData["Sidebar:ActiveTab"] = "addresses";

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var command = new CreateUserAddressCommand(
            userId,
            model.Title!,
            model.RecipientName!,
            model.RecipientPhone!,
            model.Province!,
            model.City!,
            model.PostalCode!,
            model.AddressLine!,
            model.Plaque,
            model.Unit,
            model.IsDefault);

        var result = await _mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
        {
            TempData["Error"] = result.Error ?? "خطا در افزودن آدرس.";
            return View(model);
        }

        TempData["Success"] = "آدرس با موفقیت افزوده شد.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(Guid id, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Challenge();
        }

        ViewData["Title"] = "ویرایش آدرس";
        ViewData["Sidebar:ActiveTab"] = "addresses";

        var query = new GetUserAddressQuery(id, userId);
        var result = await _mediator.Send(query, cancellationToken);

        if (!result.IsSuccess || result.Value is null)
        {
            TempData["Error"] = result.Error ?? "آدرس مورد نظر یافت نشد.";
            return RedirectToAction(nameof(Index));
        }

        var address = result.Value;
        var viewModel = new UserAddressFormViewModel
        {
            Id = address.Id,
            Title = address.Title,
            RecipientName = address.RecipientName,
            RecipientPhone = address.RecipientPhone,
            Province = address.Province,
            City = address.City,
            PostalCode = address.PostalCode,
            AddressLine = address.AddressLine,
            Plaque = address.Plaque,
            Unit = address.Unit,
            IsDefault = address.IsDefault
        };

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(UserAddressFormViewModel model, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Challenge();
        }

        ViewData["Title"] = "ویرایش آدرس";
        ViewData["Sidebar:ActiveTab"] = "addresses";

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var command = new UpdateUserAddressCommand(
            model.Id!.Value,
            userId,
            model.Title!,
            model.RecipientName!,
            model.RecipientPhone!,
            model.Province!,
            model.City!,
            model.PostalCode!,
            model.AddressLine!,
            model.Plaque,
            model.Unit);

        var result = await _mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
        {
            TempData["Error"] = result.Error ?? "خطا در ویرایش آدرس.";
            return View(model);
        }

        // If set as default, update default status
        if (model.IsDefault)
        {
            var setDefaultCommand = new SetDefaultUserAddressCommand(model.Id.Value, userId);
            await _mediator.Send(setDefaultCommand, cancellationToken);
        }

        TempData["Success"] = "آدرس با موفقیت ویرایش شد.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Challenge();
        }

        var command = new DeleteUserAddressCommand(id, userId);
        var result = await _mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
        {
            TempData["Error"] = result.Error ?? "خطا در حذف آدرس.";
        }
        else
        {
            TempData["Success"] = "آدرس با موفقیت حذف شد.";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetDefault(Guid id, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Challenge();
        }

        var command = new SetDefaultUserAddressCommand(id, userId);
        var result = await _mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
        {
            TempData["Error"] = result.Error ?? "خطا در تنظیم آدرس پیش‌فرض.";
        }
        else
        {
            TempData["Success"] = "آدرس پیش‌فرض با موفقیت تنظیم شد.";
        }

        return RedirectToAction(nameof(Index));
    }

    private string? GetUserId()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier);
    }
}
