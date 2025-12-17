using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TestAttarClone.Application.Abstractions.Messaging;
using TestAttarClone.Application.DTOs;
using TestAttarClone.Application.Interfaces;
using TestAttarClone.SharedKernel.BaseTypes;

namespace TestAttarClone.Application.Queries.Admin.PageAccess;

public sealed record GetAllPageAccessPoliciesQuery : IQuery<IReadOnlyCollection<PageAccessPolicyDto>>;

public sealed class GetAllPageAccessPoliciesQueryHandler
    : IQueryHandler<GetAllPageAccessPoliciesQuery, IReadOnlyCollection<PageAccessPolicyDto>>
{
    private readonly IPageAccessPolicyRepository _policyRepository;

    public GetAllPageAccessPoliciesQueryHandler(IPageAccessPolicyRepository policyRepository)
    {
        _policyRepository = policyRepository;
    }

    public async Task<Result<IReadOnlyCollection<PageAccessPolicyDto>>> Handle(
        GetAllPageAccessPoliciesQuery request,
        CancellationToken cancellationToken)
    {
        var policies = await _policyRepository.GetAllAsync(cancellationToken);

        var grouped = policies
            .GroupBy(policy => BuildKey(policy.Area, policy.Controller, policy.Action))
            .Select(group =>
            {
                var parts = group.Key.Split('|');
                var area = parts.ElementAtOrDefault(0) ?? string.Empty;
                var controller = parts.ElementAtOrDefault(1) ?? string.Empty;
                var action = parts.ElementAtOrDefault(2) ?? string.Empty;
                var permissions = group
                    .Select(policy => policy.PermissionKey)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToArray();

                return new PageAccessPolicyDto(area, controller, action, permissions);
            })
            .ToArray();

        return Result<IReadOnlyCollection<PageAccessPolicyDto>>.Success(grouped);
    }

    private static string BuildKey(string area, string controller, string action)
        => $"{area?.Trim() ?? string.Empty}|{controller?.Trim() ?? string.Empty}|{action?.Trim() ?? string.Empty}";
}
