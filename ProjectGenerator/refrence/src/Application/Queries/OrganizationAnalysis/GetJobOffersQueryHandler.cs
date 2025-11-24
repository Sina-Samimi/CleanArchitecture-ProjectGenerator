using Arsis.Application.DTOs.OrganizationAnalysis;
using Arsis.Application.Interfaces;
using Arsis.SharedKernel.BaseTypes;
using MediatR;

namespace Arsis.Application.Queries.OrganizationAnalysis;

public class GetJobOffersQueryHandler : IRequestHandler<GetJobOffersQuery, Result<IReadOnlyList<JobApplicationDto>>>
{
    private readonly IOrganizationAnalysisService _organizationAnalysisService;

    public GetJobOffersQueryHandler(IOrganizationAnalysisService organizationAnalysisService)
    {
        _organizationAnalysisService = organizationAnalysisService;
    }

    public async Task<Result<IReadOnlyList<JobApplicationDto>>> Handle(GetJobOffersQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _organizationAnalysisService.GetJobOffersAsync(
                request.UserId,
                cancellationToken);

            return Result<IReadOnlyList<JobApplicationDto>>.Success(result);
        }
        catch (Exception ex)
        {
            return Result<IReadOnlyList<JobApplicationDto>>.Failure($"خطا در دریافت آگهی‌های شغلی: {ex.Message}");
        }
    }
}
