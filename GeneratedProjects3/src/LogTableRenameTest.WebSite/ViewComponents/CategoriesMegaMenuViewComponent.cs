using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LogTableRenameTest.Application.Queries.Catalog;
using LogTableRenameTest.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace LogTableRenameTest.WebSite.ViewComponents;

public sealed class CategoriesMegaMenuViewComponent : ViewComponent
{
    private readonly IMediator _mediator;

    public CategoriesMegaMenuViewComponent(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var query = new GetSiteCategoriesQuery(CategoryScope.Product);
        var result = await _mediator.Send(query, HttpContext.RequestAborted);
        
        var categories = result.IsSuccess && result.Value is not null
            ? result.Value
            : Array.Empty<Application.DTOs.Catalog.SiteCategoryDto>();

        return View(categories);
    }
}
