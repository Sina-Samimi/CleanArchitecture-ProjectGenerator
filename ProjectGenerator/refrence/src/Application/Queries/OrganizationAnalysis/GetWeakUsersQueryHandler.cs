using Arsis.Application.DTOs.OrganizationAnalysis;
using Arsis.Application.Interfaces;
using Arsis.SharedKernel.BaseTypes;
using MediatR;

namespace Arsis.Application.Queries.OrganizationAnalysis;

public class GetWeakUsersQueryHandler : IRequestHandler<GetWeakUsersQuery, Result<IReadOnlyList<WeakUserDto>>>
{
    private readonly IOrganizationAnalysisService _organizationAnalysisService;

    public GetWeakUsersQueryHandler(IOrganizationAnalysisService organizationAnalysisService)
    {
        _organizationAnalysisService = organizationAnalysisService;
    }

    public async Task<Result<IReadOnlyList<WeakUserDto>>> Handle(GetWeakUsersQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _organizationAnalysisService.GetWeakUsersAsync(
                request.OrganizationId,
                request.WeaknessType,
                cancellationToken);

            return Result<IReadOnlyList<WeakUserDto>>.Success(result);
        }
        catch (Exception ex)
        {
            return Result<IReadOnlyList<WeakUserDto>>.Failure($"خطا در دریافت کاربران ضعیف: {ex.Message}");
        }
    }
}
