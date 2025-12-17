using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LogsDtoCloneTest.Application.Abstractions.Messaging;
using LogsDtoCloneTest.Application.DTOs.Cart;
using LogsDtoCloneTest.Application.Interfaces;
using LogsDtoCloneTest.SharedKernel.BaseTypes;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using DomainEntities = LogsDtoCloneTest.Domain.Entities;

namespace LogsDtoCloneTest.Application.Queries.Cart;

public sealed record GetActiveCartsQuery(
    string? UserId = null,
    DateTimeOffset? FromDate = null,
    DateTimeOffset? ToDate = null,
    int? PageNumber = null,
    int? PageSize = null) : IQuery<ActiveCartListResultDto>
{
    public sealed class Handler : IQueryHandler<GetActiveCartsQuery, ActiveCartListResultDto>
    {
        private readonly IShoppingCartRepository _cartRepository;
        private readonly UserManager<DomainEntities.ApplicationUser> _userManager;

        public Handler(
            IShoppingCartRepository cartRepository,
            UserManager<DomainEntities.ApplicationUser> userManager)
        {
            _cartRepository = cartRepository;
            _userManager = userManager;
        }

        public async Task<Result<ActiveCartListResultDto>> Handle(GetActiveCartsQuery request, CancellationToken cancellationToken)
        {
            var pageNumber = request.PageNumber ?? 1;
            var pageSize = request.PageSize ?? 20;
            
            if (pageNumber < 1)
            {
                pageNumber = 1;
            }
            
            if (pageSize < 1 || pageSize > 100)
            {
                pageSize = 20;
            }

            var skip = (pageNumber - 1) * pageSize;

            var (carts, totalCount) = await _cartRepository.GetActiveCartsAsync(
                request.UserId,
                request.FromDate,
                request.ToDate,
                skip,
                pageSize,
                cancellationToken);

            // Get user information for carts
            var userIds = carts
                .Where(c => !string.IsNullOrWhiteSpace(c.UserId))
                .Select(c => c.UserId!)
                .Distinct()
                .ToList();

            var users = new Dictionary<string, (string? FullName, string? PhoneNumber)>();
            if (userIds.Count > 0)
            {
                var userEntities = await _userManager.Users
                    .Where(u => userIds.Contains(u.Id))
                    .Select(u => new { u.Id, u.FullName, u.PhoneNumber })
                    .ToListAsync(cancellationToken);

                foreach (var user in userEntities)
                {
                    users[user.Id] = (user.FullName, user.PhoneNumber);
                }
            }

            var cartDtos = carts.Select(cart =>
            {
                var userInfo = !string.IsNullOrWhiteSpace(cart.UserId) && users.TryGetValue(cart.UserId, out var info)
                    ? info
                    : ((string?)null, (string?)null);

                return new ActiveCartListItemDto(
                    cart.Id,
                    cart.UserId,
                    userInfo.Item1,
                    userInfo.Item2,
                    cart.Items.Count,
                    cart.Subtotal,
                    cart.DiscountTotal,
                    cart.GrandTotal,
                    cart.UpdateDate,
                    cart.CreateDate);
            }).ToList();

            var totalPages = pageSize > 0 ? (int)Math.Ceiling(totalCount / (double)pageSize) : 0;

            var result = new ActiveCartListResultDto(
                cartDtos,
                totalCount,
                pageNumber,
                pageSize,
                totalPages,
                DateTimeOffset.UtcNow);

            return Result<ActiveCartListResultDto>.Success(result);
        }
    }
}

