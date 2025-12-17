using System.Collections.Generic;

namespace LogTableRenameTest.Application.DTOs;

public sealed record PageDescriptorDto(
    string Area,
    string Controller,
    string Action,
    string DisplayName,
    bool AllowAnonymous);

public sealed record PageAccessEntryDto(
    PageDescriptorDto Descriptor,
    IReadOnlyCollection<string> Permissions);

public sealed record PageAccessOverviewDto(IReadOnlyCollection<PageAccessEntryDto> Pages);

public sealed record PageAccessPolicyDto(
    string Area,
    string Controller,
    string Action,
    IReadOnlyCollection<string> Permissions);
