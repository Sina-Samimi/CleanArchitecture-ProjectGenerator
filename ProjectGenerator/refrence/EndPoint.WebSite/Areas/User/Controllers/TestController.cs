using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Arsis.Application.Commands.Tests;
using Arsis.Application.DTOs.Billing;
using Arsis.Application.Queries.Billing;
using Arsis.Application.Queries.Tests;
using Arsis.Domain.Enums;
using EndPoint.WebSite.Models.Test;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace EndPoint.WebSite.Areas.User.Controllers;

[Area("User")]
[Authorize]
public sealed class TestController : Controller
{
    private readonly IMediator _mediator;
    private readonly ILogger<TestController> _logger;

    public TestController(IMediator mediator, ILogger<TestController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> MyTests(bool debug = false, CancellationToken cancellationToken = default)
    {
        var userId = GetUserId();
        
        if (string.IsNullOrWhiteSpace(userId))
        {
            return RedirectToAction("PhoneLogin", "Account", new { area = "", returnUrl = Url.Action("MyTests") });
        }

        // Get user's test attempts
        var attemptsResult = await _mediator.Send(new GetUserTestAttemptsQuery(userId), cancellationToken);
        var attempts = attemptsResult.IsSuccess ? attemptsResult.Value : new List<Arsis.Application.DTOs.Tests.UserTestAttemptDto>();

        // Get paid invoices with test items
        var invoiceFilter = new InvoiceListFilterDto(
            SearchTerm: null,
            UserId: userId,
            Status: InvoiceStatus.Paid,
            IssueDateFrom: null,
            IssueDateTo: null
        );

        var invoicesResult = await _mediator.Send(new GetInvoiceListQuery(invoiceFilter), cancellationToken);
        var purchasedTests = new List<PurchasedTestViewModel>();

        if (invoicesResult.IsSuccess && invoicesResult.Value?.Items != null)
        {
            foreach (var invoice in invoicesResult.Value.Items)
            {
                // Get invoice details to access items
                var invoiceDetailsResult = await _mediator.Send(
                    new GetUserInvoiceDetailsQuery(invoice.Id, userId), 
                    cancellationToken);

                if (!invoiceDetailsResult.IsSuccess || invoiceDetailsResult.Value == null)
                {
                    _logger.LogWarning("Failed to get invoice details for {InvoiceId}: {Error}", 
                        invoice.Id, invoiceDetailsResult.Error);
                    continue;
                }

                var testItems = invoiceDetailsResult.Value.Items
                    .Where(i => i.ItemType == InvoiceItemType.Test && i.ReferenceId.HasValue)
                    .ToList();

                if (testItems.Count == 0)
                {
                    continue;
                }

                foreach (var testItem in testItems)
                {
                    var testId = testItem.ReferenceId!.Value;

                    // Get test details
                    var testResult = await _mediator.Send(
                        new GetTestByIdQuery(testId, userId),
                        cancellationToken);

                    // Find attempt specifically linked to this invoice
                    var attempt = attempts.FirstOrDefault(a => 
                        a.InvoiceId.HasValue && 
                        a.InvoiceId.Value == invoice.Id);

                    if (testResult.IsSuccess && testResult.Value != null)
                    {
                        var test = testResult.Value;

                        purchasedTests.Add(new PurchasedTestViewModel
                        {
                            TestId = testId,
                            TestTitle = test.Title,
                            TestType = test.Type,
                            PurchaseDate = invoice.IssueDate,
                            InvoiceId = invoice.Id,
                            InvoiceNumber = invoice.InvoiceNumber,
                            Attempt = attempt
                        });
                    }
                    else
                    {
                        // Even if test not found, add it with basic info from invoice
                        purchasedTests.Add(new PurchasedTestViewModel
                        {
                            TestId = testId,
                            TestTitle = testItem.Name,
                            TestType = attempt?.TestType ?? Arsis.Domain.Enums.TestType.General,
                            PurchaseDate = invoice.IssueDate,
                            InvoiceId = invoice.Id,
                            InvoiceNumber = invoice.InvoiceNumber,
                            Attempt = attempt
                        });
                    }
                }
            }
        }

        // Get all attempt IDs that are already shown in purchasedTests section
        var displayedAttemptIds = new HashSet<Guid>(
            purchasedTests
                .Where(pt => pt.Attempt != null)
                .Select(pt => pt.Attempt!.Id));

        // Show only attempts that are not already displayed in purchasedTests section
        var standaloneAttempts = attempts
            .Where(a => !displayedAttemptIds.Contains(a.Id))
            .OrderByDescending(a => a.StartedAt)
            .ToList();

        var viewModel = new MyTestsViewModel
        {
            PurchasedTests = purchasedTests.OrderByDescending(pt => pt.PurchaseDate).ToList(),
            Attempts = standaloneAttempts
        };

        // Debug mode: show detailed information
        if (debug)
        {
            return View("MyTestsDebug", viewModel);
        }

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Start(Guid id, Guid? invoiceId, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return RedirectToAction("PhoneLogin", "Account", new { area = "" });
        }

        var command = new StartTestAttemptCommand(id, userId, invoiceId);
        var result = await _mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
        {
            TempData["Error"] = result.Error;
            return RedirectToAction(nameof(MyTests));
        }

        return RedirectToAction("Take", "Test", new { area = "", attemptId = result.Value });
    }

    private string? GetUserId()
        => User?.Identity?.IsAuthenticated == true
            ? User.FindFirstValue(ClaimTypes.NameIdentifier)
            : null;
}
