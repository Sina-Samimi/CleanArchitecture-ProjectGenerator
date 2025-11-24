using Arsis.Application.DTOs.OrganizationAnalysis;
using Arsis.SharedKernel.BaseTypes;
using MediatR;

namespace Arsis.Application.Queries.OrganizationAnalysis;

public record GetJobOffersQuery(string UserId) : IRequest<Result<IReadOnlyList<JobApplicationDto>>>;
