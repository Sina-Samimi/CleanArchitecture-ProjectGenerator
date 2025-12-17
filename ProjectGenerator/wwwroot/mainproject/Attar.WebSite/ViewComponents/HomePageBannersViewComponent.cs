using System;
using System.Linq;
using System.Threading.Tasks;
using Attar.Application.Interfaces;
using Attar.Application.Queries.Admin.SiteSettings;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Attar.WebSite.ViewComponents;

public sealed class HomePageBannersViewComponent : ViewComponent
{
    private readonly IBannerRepository _bannerRepository;
    private readonly IMediator _mediator;

    public HomePageBannersViewComponent(IBannerRepository bannerRepository, IMediator mediator)
    {
        _bannerRepository = bannerRepository;
        _mediator = mediator;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var cancellationToken = HttpContext.RequestAborted;
        var banners = await _bannerRepository.GetActiveBannersForHomePageAsync(cancellationToken);

        // Get site settings to check if banners should be displayed as slider
        var siteSettingsResult = await _mediator.Send(new GetSiteSettingsQuery(), cancellationToken);
        var bannersAsSlider = siteSettingsResult.IsSuccess && siteSettingsResult.Value is not null
            ? siteSettingsResult.Value.BannersAsSlider
            : false;

        var bannersList = banners
            .Where(b => b.IsCurrentlyActive())
            .OrderBy(b => b.DisplayOrder)
            .ThenByDescending(b => b.CreateDate)
            .Select(b => new BannerViewModel(
                b.Id,
                b.Title,
                b.ImagePath,
                b.LinkUrl,
                b.AltText,
                bannersAsSlider))
            .ToList();

        return View(bannersList);
    }
}

public record BannerViewModel(
    Guid Id,
    string Title,
    string ImagePath,
    string? LinkUrl,
    string? AltText,
    bool IsSlider);

