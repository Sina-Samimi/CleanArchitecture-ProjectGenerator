using Arsis.Application.DTOs.OrganizationAnalysis;
using Arsis.SharedKernel.BaseTypes;
using MediatR;

namespace Arsis.Application.Queries.OrganizationAnalysis;

public record GetWeakUsersQuery(Guid OrganizationId, string WeaknessType) : IRequest<Result<IReadOnlyList<WeakUserDto>>>;
