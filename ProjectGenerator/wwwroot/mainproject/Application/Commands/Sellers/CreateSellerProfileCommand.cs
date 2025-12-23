using System;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using MobiRooz.Application.Abstractions.Messaging;
using MobiRooz.Application.Interfaces;
using MobiRooz.Domain.Entities;
using MobiRooz.Domain.Entities.Sellers;
using MobiRooz.SharedKernel.Authorization;
using MobiRooz.SharedKernel.BaseTypes;
using Microsoft.AspNetCore.Identity;

namespace MobiRooz.Application.Commands.Sellers;

public sealed record CreateSellerProfileCommand(
    string DisplayName,
    string? LicenseNumber,
    DateOnly? LicenseIssueDate,
    DateOnly? LicenseExpiryDate,
    string? ShopAddress,
    string? WorkingHours,
    int? ExperienceYears,
    string? Bio,
    string? AvatarUrl,
    string? ContactEmail,
    string? ContactPhone,
    string? UserId,
    bool IsActive,
    decimal? SellerSharePercentage = null) : ICommand<Guid>
{
    public sealed class Handler : ICommandHandler<CreateSellerProfileCommand, Guid>
    {
        private readonly ISellerProfileRepository _sellerRepository;
        private readonly IAuditContext _auditContext;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public Handler(
            ISellerProfileRepository sellerRepository,
            IAuditContext auditContext,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            _sellerRepository = sellerRepository;
            _auditContext = auditContext;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public async Task<Result<Guid>> Handle(CreateSellerProfileCommand request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.DisplayName))
            {
                return Result<Guid>.Failure("نام فروشنده را وارد کنید.");
            }

            var userId = string.IsNullOrWhiteSpace(request.UserId)
                ? null
                : request.UserId.Trim();

            if (!string.IsNullOrWhiteSpace(userId))
            {
                var exists = await _sellerRepository.ExistsByUserIdAsync(userId, null, cancellationToken);
                if (exists)
                {
                    return Result<Guid>.Failure("برای این کاربر قبلاً پروفایل فروشنده ثبت شده است.");
                }
            }

            var seller = new SellerProfile(
                request.DisplayName,
                request.LicenseNumber,
                request.LicenseIssueDate,
                request.LicenseExpiryDate,
                request.ShopAddress,
                request.WorkingHours,
                request.ExperienceYears,
                request.Bio,
                request.AvatarUrl,
                request.ContactEmail,
                request.ContactPhone,
                userId,
                request.IsActive,
                request.SellerSharePercentage);

            var audit = _auditContext.Capture();

            seller.CreatorId = audit.UserId;
            seller.CreateDate = audit.Timestamp;
            seller.UpdaterId = audit.UserId;
            seller.UpdateDate = audit.Timestamp;
            seller.Ip = audit.IpAddress;

            await _sellerRepository.AddAsync(seller, cancellationToken);

            // Assign Seller role to the user if UserId is provided
            if (!string.IsNullOrWhiteSpace(userId))
            {
                var roleAssignmentResult = await AssignSellerRoleToUserAsync(userId);
                if (!roleAssignmentResult.IsSuccess)
                {
                    // Rollback: soft delete the seller profile if role assignment fails
                    var rollbackAudit = _auditContext.Capture();
                    seller.IsDeleted = true;
                    seller.RemoveDate = rollbackAudit.Timestamp;
                    seller.UpdaterId = rollbackAudit.UserId;
                    seller.UpdateDate = rollbackAudit.Timestamp;
                    seller.Ip = rollbackAudit.IpAddress;
                    await _sellerRepository.UpdateAsync(seller, cancellationToken);
                    return Result<Guid>.Failure(roleAssignmentResult.Error ?? "خطا در تخصیص نقش فروشنده به کاربر.");
                }
            }

            return Result<Guid>.Success(seller.Id);
        }

        private async Task<Result> AssignSellerRoleToUserAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user is null || user.IsDeleted)
            {
                return Result.Failure("کاربر مورد نظر یافت نشد.");
            }

            // Check if user already has the Seller role
            var isInRole = await _userManager.IsInRoleAsync(user, RoleNames.Seller);
            if (isInRole)
            {
                return Result.Success();
            }

            // Ensure the Seller role exists
            var identityRole = await _roleManager.FindByNameAsync(RoleNames.Seller);
            if (identityRole is null)
            {
                identityRole = new IdentityRole(RoleNames.Seller);
                var createRoleResult = await _roleManager.CreateAsync(identityRole);
                if (!createRoleResult.Succeeded)
                {
                    var roleCreationError = string.Join("; ", createRoleResult.Errors.Select(e => e.Description));
                    return Result.Failure(roleCreationError);
                }
            }

            // Ensure role has display name claim
            var displayNameError = await EnsureRoleDisplayNameAsync(identityRole);
            if (displayNameError is not null)
            {
                return Result.Failure(displayNameError);
            }

            // Add user to Seller role
            var addToRoleResult = await _userManager.AddToRoleAsync(user, RoleNames.Seller);
            if (!addToRoleResult.Succeeded)
            {
                var addToRoleError = string.Join("; ", addToRoleResult.Errors.Select(e => e.Description));
                return Result.Failure(addToRoleError);
            }

            // Update user's last modified date
            user.LastModifiedOn = DateTimeOffset.UtcNow;
            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                return Result.Failure(string.Join("; ", updateResult.Errors.Select(e => e.Description)));
            }

            // Update security stamp to invalidate existing sessions
            var stampResult = await _userManager.UpdateSecurityStampAsync(user);
            if (!stampResult.Succeeded)
            {
                return Result.Failure(string.Join("; ", stampResult.Errors.Select(e => e.Description)));
            }

            return Result.Success();
        }

        private async Task<string?> EnsureRoleDisplayNameAsync(IdentityRole role)
        {
            var claims = await _roleManager.GetClaimsAsync(role);
            var displayNameClaim = claims.FirstOrDefault(claim =>
                string.Equals(claim.Type, RoleClaimTypes.DisplayName, StringComparison.OrdinalIgnoreCase));

            if (displayNameClaim is null)
            {
                var addResult = await _roleManager.AddClaimAsync(role, new Claim(RoleClaimTypes.DisplayName, RoleNames.Seller));
                if (!addResult.Succeeded)
                {
                    return string.Join("; ", addResult.Errors.Select(error => error.Description));
                }
            }

            return null;
        }
    }
}
