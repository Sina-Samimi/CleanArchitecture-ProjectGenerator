using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LogTableRenameTest.Application.Abstractions.Messaging;
using LogTableRenameTest.Application.DTOs;
using LogTableRenameTest.Domain.Entities;
using LogTableRenameTest.SharedKernel.Authorization;
using LogTableRenameTest.SharedKernel.BaseTypes;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace LogTableRenameTest.Application.Queries.Identity.GetUserLookups;

public sealed record GetAdminUserLookupsQuery(int MaxResults = 200) : IQuery<IReadOnlyCollection<UserLookupDto>>
{
    public sealed class Handler : IQueryHandler<GetAdminUserLookupsQuery, IReadOnlyCollection<UserLookupDto>>
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public Handler(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        public async Task<Result<IReadOnlyCollection<UserLookupDto>>> Handle(
            GetAdminUserLookupsQuery request,
            CancellationToken cancellationToken)
        {
            var maxResults = Math.Clamp(request.MaxResults, 1, 500);

            // Get all admin users
            var adminUsers = await _userManager.GetUsersInRoleAsync(RoleNames.Admin);
            var adminUserIds = adminUsers.Select(u => u.Id).ToHashSet();

            if (adminUserIds.Count == 0)
            {
                return Result<IReadOnlyCollection<UserLookupDto>>.Success(Array.Empty<UserLookupDto>());
            }

            var users = await _userManager.Users
                .Where(user => !user.IsDeleted && adminUserIds.Contains(user.Id))
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
