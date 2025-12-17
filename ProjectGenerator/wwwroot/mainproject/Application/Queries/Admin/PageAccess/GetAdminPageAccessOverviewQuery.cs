using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Attar.Application.Abstractions.Messaging;
using Attar.Application.DTOs;
using Attar.Application.Interfaces;
using Attar.SharedKernel.BaseTypes;

namespace Attar.Application.Queries.Admin.PageAccess;

public sealed record GetAdminPageAccessOverviewQuery : IQuery<PageAccessOverviewDto>;

public sealed class GetAdminPageAccessOverviewQueryHandler
    : IQueryHandler<GetAdminPageAccessOverviewQuery, PageAccessOverviewDto>
{
    private readonly IPageDescriptorProvider _pageDescriptorProvider;
    private readonly IPageAccessPolicyRepository _policyRepository;

    public GetAdminPageAccessOverviewQueryHandler(
        IPageDescriptorProvider pageDescriptorProvider,
        IPageAccessPolicyRepository policyRepository)
    {
        _pageDescriptorProvider = pageDescriptorProvider;
        _policyRepository = policyRepository;
    }

    public async Task<Result<PageAccessOverviewDto>> Handle(
        GetAdminPageAccessOverviewQuery request,
        CancellationToken cancellationToken)
    {
        var descriptors = await _pageDescriptorProvider.GetAdminPageDescriptorsAsync(cancellationToken);
        var policies = await _policyRepository.GetAllAsync(cancellationToken);

        var policyLookup = policies
            .GroupBy(policy => BuildKey(policy.Area, policy.Controller, policy.Action))
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyCollection<string>)group
                    .Select(policy => policy.PermissionKey)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToArray(),
                StringComparer.OrdinalIgnoreCase);

        var pages = descriptors
            .Where(descriptor => !descriptor.AllowAnonymous)
            .Select(descriptor =>
            {
                var key = BuildKey(descriptor.Area, descriptor.Controller, descriptor.Action);
                policyLookup.TryGetValue(key, out var permissions);
                permissions ??= Array.Empty<string>();

                return new PageAccessEntryDto(descriptor, permissions);
            })
            .OrderBy(entry => entry.Descriptor.Controller, StringComparer.CurrentCulture)
            .ThenBy(entry => entry.Descriptor.Action, StringComparer.CurrentCulture)
            .ToArray();

        return Result<PageAccessOverviewDto>.Success(new PageAccessOverviewDto(pages));
    }

    private static string BuildKey(string area, string controller, string action)
        => $"{area?.Trim() ?? string.Empty}|{controller?.Trim() ?? string.Empty}|{action?.Trim() ?? string.Empty}";
}
