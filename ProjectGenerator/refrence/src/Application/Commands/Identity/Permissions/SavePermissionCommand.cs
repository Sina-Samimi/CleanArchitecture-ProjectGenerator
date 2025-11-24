using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Arsis.Application.Abstractions.Messaging;
using Arsis.Application.DTOs;
using Arsis.Application.Interfaces;
using Arsis.Domain.Entities;
using Arsis.SharedKernel.Authorization;
using Arsis.SharedKernel.BaseTypes;

namespace Arsis.Application.Commands.Identity.Permissions;

public sealed record SavePermissionCommand(SavePermissionPayload Payload) : ICommand<PermissionDetailsDto>;

public sealed record SavePermissionPayload(
    Guid? Id,
    string DisplayName,
    string? Description,
    bool IsCore,
    string? GroupKey,
    string? GroupDisplayName);

public sealed class SavePermissionCommandHandler : ICommandHandler<SavePermissionCommand, PermissionDetailsDto>
{
    private readonly IAccessPermissionRepository _permissionRepository;
    private readonly IPermissionDefinitionService _permissionDefinitionService;
    private readonly IAuditContext _auditContext;

    public SavePermissionCommandHandler(
        IAccessPermissionRepository permissionRepository,
        IPermissionDefinitionService permissionDefinitionService,
        IAuditContext auditContext)
    {
        _permissionRepository = permissionRepository;
        _permissionDefinitionService = permissionDefinitionService;
        _auditContext = auditContext;
    }

    public async Task<Result<PermissionDetailsDto>> Handle(SavePermissionCommand request, CancellationToken cancellationToken)
    {
        var payload = request.Payload;
        var audit = _auditContext.Capture();
        var trimmedName = payload.DisplayName?.Trim();
        var trimmedDescription = string.IsNullOrWhiteSpace(payload.Description)
            ? null
            : payload.Description.Trim();

        if (string.IsNullOrWhiteSpace(trimmedName))
        {
            return Result<PermissionDetailsDto>.Failure("نام مجوز را وارد کنید.");
        }

        if (trimmedName.Length > 256)
        {
            return Result<PermissionDetailsDto>.Failure("نام مجوز نباید بیش از ۲۵۶ کاراکتر باشد.");
        }

        if (trimmedDescription is not null && trimmedDescription.Length > 1024)
        {
            return Result<PermissionDetailsDto>.Failure("توضیح مجوز نباید بیش از ۱۰۲۴ کاراکتر باشد.");
        }

        var normalizedGroupKey = PermissionGroupUtility.NormalizeGroupKey(payload.GroupKey);
        var trimmedGroupDisplayName = string.IsNullOrWhiteSpace(payload.GroupDisplayName)
            ? null
            : payload.GroupDisplayName.Trim();

        var resolvedGroupDisplayName = PermissionGroupUtility.ResolveGroupDisplayName(
            normalizedGroupKey,
            trimmedGroupDisplayName ?? payload.GroupKey);

        if (string.IsNullOrWhiteSpace(resolvedGroupDisplayName))
        {
            resolvedGroupDisplayName = normalizedGroupKey;
        }

        AccessPermission permission;
        if (payload.Id.HasValue)
        {
            permission = await _permissionRepository.GetByIdAsync(payload.Id.Value, cancellationToken);

            if (permission is null)
            {
                return Result<PermissionDetailsDto>.Failure("مجوز مورد نظر یافت نشد.");
            }

            var duplicateName = await _permissionRepository.ExistsWithDisplayNameAsync(
                trimmedName!,
                permission.Id,
                cancellationToken);

            if (duplicateName)
            {
                return Result<PermissionDetailsDto>.Failure("مجوز دیگری با این عنوان ثبت شده است.");
            }

            permission.UpdateDetails(
                trimmedName!,
                trimmedDescription,
                payload.IsCore,
                normalizedGroupKey,
                resolvedGroupDisplayName);

            permission.UpdaterId = audit.UserId;
            permission.UpdateDate = audit.Timestamp;
            permission.Ip = audit.IpAddress;
        }
        else
        {
            var duplicateName = await _permissionRepository.ExistsWithDisplayNameAsync(
                trimmedName!,
                excludeId: null,
                cancellationToken);

            if (duplicateName)
            {
                return Result<PermissionDetailsDto>.Failure("مجوزی با این عنوان از قبل وجود دارد.");
            }

            var allKeys = await _permissionDefinitionService.GetAllKeysAsync(cancellationToken);
            var generatedKey = GenerateUniqueKey(trimmedName!, allKeys, normalizedGroupKey);

            permission = new AccessPermission(
                generatedKey,
                trimmedName!,
                trimmedDescription,
                payload.IsCore,
                normalizedGroupKey,
                resolvedGroupDisplayName)
            {
                CreatorId = audit.UserId,
                Ip = audit.IpAddress,
                CreateDate = audit.Timestamp,
                UpdateDate = audit.Timestamp
            };
            await _permissionRepository.AddAsync(permission, cancellationToken);
        }

        await _permissionRepository.SaveChangesAsync(cancellationToken);

        var dto = new PermissionDetailsDto(
            permission.Id,
            permission.Key,
            permission.DisplayName,
            permission.Description,
            permission.IsCore,
            permission.CreatedAt,
            permission.UpdatedAt,
            permission.GroupKey,
            permission.GroupDisplayName);

        return Result<PermissionDetailsDto>.Success(dto);
    }

    private static string GenerateUniqueKey(string displayName, HashSet<string> existingKeys, string groupKey)
    {
        var baseSlug = CreateSlug(displayName);
        if (string.IsNullOrWhiteSpace(baseSlug))
        {
            baseSlug = "permission";
        }

        var canonicalGroup = string.IsNullOrWhiteSpace(groupKey) ? "custom" : groupKey;
        var baseKey = $"{canonicalGroup}.{baseSlug}";
        var candidate = baseKey;
        var counter = 1;

        while (existingKeys.Contains(candidate))
        {
            candidate = $"{baseKey}.{counter}";
            counter++;
        }

        existingKeys.Add(candidate);
        return candidate;
    }

    private static string CreateSlug(string value)
    {
        var normalized = value.Normalize(NormalizationForm.FormKD);
        var builder = new StringBuilder();

        foreach (var character in normalized)
        {
            if (char.IsWhiteSpace(character))
            {
                if (builder.Length == 0 || builder[^1] == '-')
                {
                    continue;
                }

                builder.Append('-');
                continue;
            }

            if (char.IsLetterOrDigit(character))
            {
                builder.Append(char.ToLowerInvariant(character));
                continue;
            }

            var category = CharUnicodeInfo.GetUnicodeCategory(character);
            if (category == UnicodeCategory.DecimalDigitNumber || category == UnicodeCategory.LetterNumber)
            {
                builder.Append(character);
            }
        }

        var slug = builder.ToString().Trim('-');
        if (slug.Length > 64)
        {
            slug = slug[..64];
        }

        slug = slug.Trim('-');
        return slug;
    }
}
