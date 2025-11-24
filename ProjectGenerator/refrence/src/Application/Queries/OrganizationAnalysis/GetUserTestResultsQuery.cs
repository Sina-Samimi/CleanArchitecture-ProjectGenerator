using Arsis.Application.DTOs.OrganizationAnalysis;
using Arsis.SharedKernel.BaseTypes;
using MediatR;

namespace Arsis.Application.Queries.OrganizationAnalysis;

public record GetUserTestResultsQuery(Guid OrganizationId, string JobGroup) : IRequest<Result<IReadOnlyList<UserTestResultDto>>>;
