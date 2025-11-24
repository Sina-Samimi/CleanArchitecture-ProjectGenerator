using Arsis.Application.DTOs.OrganizationAnalysis;
using Arsis.SharedKernel.BaseTypes;
using MediatR;

namespace Arsis.Application.Queries.OrganizationAnalysis;

public record GetOrganizationWeaknessesQuery(Guid OrganizationId) : IRequest<Result<IReadOnlyList<OrganizationWeaknessDto>>>;
