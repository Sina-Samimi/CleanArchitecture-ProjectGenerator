using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using LogTableRenameTest.Application.Commands.Contacts;
using LogTableRenameTest.WebSite.Models;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace LogTableRenameTest.WebSite.Controllers;

public sealed class ContactController : Controller
{
    private readonly IMediator _mediator;

    public ContactController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Submit(ContactFormModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            TempData["ContactError"] = "لطفاً تمام فیلدهای الزامی را پر کنید.";
            return RedirectToAction("Contact", "Home");
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var command = new CreateContactMessageCommand(
            model.FullName,
            model.Email,
            model.Phone,
            model.Subject,
            model.Message,
            userId);

        var result = await _mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
        {
            TempData["ContactError"] = result.Error ?? "در ارسال پیام خطایی رخ داد. لطفاً دوباره تلاش کنید.";
            return RedirectToAction("Contact", "Home");
        }

        TempData["ContactSuccess"] = "پیام شما با موفقیت ارسال شد. در اسرع وقت با شما تماس خواهیم گرفت.";
        return RedirectToAction("Contact", "Home");
    }
}

