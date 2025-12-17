using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Attar.Application.Abstractions.Messaging;
using Attar.Application.Interfaces;
using Attar.SharedKernel.BaseTypes;

namespace Attar.Application.Commands.Admin.PageAccess;

public sealed record SavePageAccessPolicyCommand(
    string Area,
    string Controller,
    string Action,
    IReadOnlyCollection<string> Permissions) : ICommand;

public sealed class SavePageAccessPolicyCommandHandler : ICommandHandler<SavePageAccessPolicyCommand>
{
    private readonly IPageAccessPolicyRepository _policyRepository;
    private readonly IPermissionDefinitionService _permissionDefinitionService;

    public SavePageAccessPolicyCommandHandler(
        IPageAccessPolicyRepository policyRepository,
        IPermissionDefinitionService permissionDefinitionService)
    {
        _policyRepository = policyRepository;
        _permissionDefinitionService = permissionDefinitionService;
    }

    public async Task<Result> Handle(SavePageAccessPolicyCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Controller) || string.IsNullOrWhiteSpace(request.Action))
        {
            return Result.Failure("صفحه انتخاب‌شده نامعتبر است.");
        }

        var normalizedPermissions = request.Permissions?
            .Where(permission => !string.IsNullOrWhiteSpace(permission))
            .Select(permission => permission.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray() ?? Array.Empty<string>();

        var validKeys = await _permissionDefinitionService.GetAllKeysAsync(cancellationToken);

        foreach (var permission in normalizedPermissions)
        {
            if (!validKeys.Contains(permission))
            {
                return Result.Failure($"مجوز '{permission}' معتبر نیست.");
            }
        }

        await _policyRepository.ReplacePoliciesAsync(
            request.Area ?? string.Empty,
            request.Controller,
            request.Action,
            normalizedPermissions,
            cancellationToken);

        await _policyRepository.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
