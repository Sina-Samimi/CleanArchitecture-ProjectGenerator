using Arsis.Application.DTOs.OrganizationAnalysis;
using Arsis.Application.Interfaces;
using Arsis.SharedKernel.BaseTypes;
using MediatR;

namespace Arsis.Application.Queries.OrganizationAnalysis;

public class GetOrganizationWeaknessesQueryHandler : IRequestHandler<GetOrganizationWeaknessesQuery, Result<IReadOnlyList<OrganizationWeaknessDto>>>
{
    private readonly IOrganizationAnalysisService _organizationAnalysisService;

    public GetOrganizationWeaknessesQueryHandler(IOrganizationAnalysisService organizationAnalysisService)
    {
        _organizationAnalysisService = organizationAnalysisService;
    }

    public async Task<Result<IReadOnlyList<OrganizationWeaknessDto>>> Handle(GetOrganizationWeaknessesQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _organizationAnalysisService.GetOrganizationWeaknessesAsync(
                request.OrganizationId,
                cancellationToken);

            return Result<IReadOnlyList<OrganizationWeaknessDto>>.Success(result);
        }
        catch (Exception ex)
        {
            return Result<IReadOnlyList<OrganizationWeaknessDto>>.Failure($"خطا در دریافت ضعف‌های سازمان: {ex.Message}");
        }
    }
}
