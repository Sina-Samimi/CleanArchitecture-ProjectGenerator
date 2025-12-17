using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Attar.Application.Abstractions.Messaging;
using Attar.Application.DTOs;
using Attar.Domain.Entities;
using Attar.SharedKernel.BaseTypes;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Attar.Application.Queries.Identity.GetUsersByIds;

public sealed record GetUsersByIdsQuery(IReadOnlyCollection<string> UserIds)
    : IQuery<IReadOnlyDictionary<string, UserLookupDto>>
{
    public sealed class Handler : IQueryHandler<GetUsersByIdsQuery, IReadOnlyDictionary<string, UserLookupDto>>
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public Handler(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        public async Task<Result<IReadOnlyDictionary<string, UserLookupDto>>> Handle(
            GetUsersByIdsQuery request,
            CancellationToken cancellationToken)
        {
            if (request.UserIds is null || request.UserIds.Count == 0)
            {
                return Result<IReadOnlyDictionary<string, UserLookupDto>>.Success(
                    new Dictionary<string, UserLookupDto>(StringComparer.Ordinal));
            }

            var normalizedIds = request.UserIds
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Select(id => id.Trim())
                .Distinct(StringComparer.Ordinal)
                .ToArray();

            if (normalizedIds.Length == 0)
            {
                return Result<IReadOnlyDictionary<string, UserLookupDto>>.Success(
                    new Dictionary<string, UserLookupDto>(StringComparer.Ordinal));
            }

            var users = await _userManager.Users
                .Where(user => normalizedIds.Contains(user.Id))
                .Select(user => new
                {
                    user.Id,
                    user.FullName,
                    user.Email,
                    user.UserName,
                    user.PhoneNumber,
                    user.IsActive
                })
                .ToListAsync(cancellationToken);

            var lookup = new Dictionary<string, UserLookupDto>(StringComparer.Ordinal);

            foreach (var user in users)
            {
                var displayName = string.IsNullOrWhiteSpace(user.FullName)
                    ? (!string.IsNullOrWhiteSpace(user.Email)
                        ? user.Email!
                        : user.UserName ?? user.Id)
                    : user.FullName;

                lookup[user.Id] = new UserLookupDto(user.Id, displayName, user.Email, user.IsActive, user.PhoneNumber);
            }

            return Result<IReadOnlyDictionary<string, UserLookupDto>>.Success(lookup);
        }
    }
}

