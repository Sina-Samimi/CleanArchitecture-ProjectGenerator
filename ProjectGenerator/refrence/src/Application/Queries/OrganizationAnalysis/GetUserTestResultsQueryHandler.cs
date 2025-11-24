using Arsis.Application.DTOs.OrganizationAnalysis;
using Arsis.Application.Interfaces;
using Arsis.SharedKernel.BaseTypes;
using MediatR;

namespace Arsis.Application.Queries.OrganizationAnalysis;

public class GetUserTestResultsQueryHandler : IRequestHandler<GetUserTestResultsQuery, Result<IReadOnlyList<UserTestResultDto>>>
{
    private readonly IOrganizationAnalysisService _organizationAnalysisService;

    public GetUserTestResultsQueryHandler(IOrganizationAnalysisService organizationAnalysisService)
    {
        _organizationAnalysisService = organizationAnalysisService;
    }

    public async Task<Result<IReadOnlyList<UserTestResultDto>>> Handle(GetUserTestResultsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _organizationAnalysisService.GetUserTestResultsAsync(
                request.OrganizationId,
                request.JobGroup,
                cancellationToken);

            return Result<IReadOnlyList<UserTestResultDto>>.Success(result);
        }
        catch (Exception ex)
        {
            return Result<IReadOnlyList<UserTestResultDto>>.Failure($"خطا در دریافت نتایج تست کاربران: {ex.Message}");
        }
    }
}
