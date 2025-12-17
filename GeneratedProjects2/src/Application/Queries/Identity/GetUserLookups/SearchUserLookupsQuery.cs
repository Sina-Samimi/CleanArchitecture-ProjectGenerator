using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LogsDtoCloneTest.Application.Abstractions.Messaging;
using LogsDtoCloneTest.Application.DTOs;
using LogsDtoCloneTest.Domain.Entities;
using LogsDtoCloneTest.SharedKernel.BaseTypes;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace LogsDtoCloneTest.Application.Queries.Identity.GetUserLookups;

public sealed record SearchUserLookupsQuery(string? SearchTerm, int MaxResults = 50) : IQuery<IReadOnlyCollection<UserLookupDto>>
{
    public sealed class Handler : IQueryHandler<SearchUserLookupsQuery, IReadOnlyCollection<UserLookupDto>>
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public Handler(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        public async Task<Result<IReadOnlyCollection<UserLookupDto>>> Handle(
            SearchUserLookupsQuery request,
            CancellationToken cancellationToken)
        {
            var maxResults = Math.Clamp(request.MaxResults, 1, 100);
            var searchTerm = request.SearchTerm?.Trim();

            var query = _userManager.Users
                .Where(user => !user.IsDeleted);

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var pattern = $"%{searchTerm}%";
                query = query.Where(user =>
                    EF.Functions.Like(user.FullName, pattern) ||
                    EF.Functions.Like(user.Email ?? "", pattern) ||
                    EF.Functions.Like(user.UserName ?? "", pattern) ||
                    (user.PhoneNumber != null && EF.Functions.Like(user.PhoneNumber, pattern)));
            }

            var users = await query
                .OrderByDescending(user => user.IsActive)
                .ThenBy(user => user.FullName)
                .ThenBy(user => user.Email)
                .Take(maxResults)
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

            var lookups = users
                .Select(user =>
                {
                    var displayName = string.IsNullOrWhiteSpace(user.FullName)
                        ? (!string.IsNullOrWhiteSpace(user.Email)
                            ? user.Email!
                            : user.UserName ?? user.Id)
                        : user.FullName;

                    return new UserLookupDto(user.Id, displayName, user.Email, user.IsActive, user.PhoneNumber);
                })
                .ToArray();

            return Result<IReadOnlyCollection<UserLookupDto>>.Success(lookups);
        }
    }
}
