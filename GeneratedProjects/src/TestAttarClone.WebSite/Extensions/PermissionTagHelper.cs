using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using TestAttarClone.SharedKernel.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace TestAttarClone.WebSite.Extensions;

/// <summary>
/// Tag helper to conditionally render content based on user permissions
/// Usage: &lt;div asp-require-permission="blogs.create"&gt;Create Blog&lt;/div&gt;
/// </summary>
[HtmlTargetElement(Attributes = "asp-require-permission")]
public sealed class PermissionTagHelper : TagHelper
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    /// <summary>
    /// The permission(s) required to display the content (comma-separated for multiple)
    /// </summary>
    [HtmlAttributeName("asp-require-permission")]
    public string? RequirePermission { get; set; }

    /// <summary>
    /// If true, all specified permissions are required (AND logic)
    /// If false, any permission is sufficient (OR logic)
    /// </summary>
    [HtmlAttributeName("asp-require-all")]
    public bool RequireAll { get; set; }

    public PermissionTagHelper(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public override Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
    {
        if (string.IsNullOrWhiteSpace(RequirePermission))
        {
            // No permission specified, suppress the element
            output.SuppressOutput();
            return Task.CompletedTask;
        }

        var user = _httpContextAccessor.HttpContext?.User;
        if (user?.Identity?.IsAuthenticated != true)
        {
            output.SuppressOutput();
            return Task.CompletedTask;
        }

        var permissions = RequirePermission
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .ToArray();

        if (permissions.Length == 0)
        {
            output.SuppressOutput();
            return Task.CompletedTask;
        }

        bool hasAccess;
        if (RequireAll)
        {
            hasAccess = user.HasAllPermissions(permissions);
        }
        else
        {
            hasAccess = user.HasAnyPermission(permissions);
        }

        if (!hasAccess)
        {
            output.SuppressOutput();
        }

        return Task.CompletedTask;
    }
}

/// <summary>
/// Tag helper to conditionally hide content based on user permissions (inverse of PermissionTagHelper)
/// Usage: &lt;div asp-hide-if-permission="blogs.create"&gt;You cannot create blogs&lt;/div&gt;
/// </summary>
[HtmlTargetElement(Attributes = "asp-hide-if-permission")]
public sealed class HideIfPermissionTagHelper : TagHelper
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    /// <summary>
    /// The permission(s) that will hide the content if present (comma-separated for multiple)
    /// </summary>
    [HtmlAttributeName("asp-hide-if-permission")]
    public string? HideIfPermission { get; set; }

    /// <summary>
    /// If true, all specified permissions must be present to hide (AND logic)
    /// If false, any permission will hide the content (OR logic)
    /// </summary>
    [HtmlAttributeName("asp-hide-all")]
    public bool HideAll { get; set; }

    public HideIfPermissionTagHelper(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public override Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
    {
        if (string.IsNullOrWhiteSpace(HideIfPermission))
        {
            // No permission specified, show the element
            return Task.CompletedTask;
        }

        var user = _httpContextAccessor.HttpContext?.User;
        if (user?.Identity?.IsAuthenticated != true)
        {
            // User not authenticated, show the element
            return Task.CompletedTask;
        }

        var permissions = HideIfPermission
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .ToArray();

        if (permissions.Length == 0)
        {
            return Task.CompletedTask;
        }

        bool hasPermission;
        if (HideAll)
        {
            hasPermission = user.HasAllPermissions(permissions);
        }
        else
        {
            hasPermission = user.HasAnyPermission(permissions);
        }

        if (hasPermission)
        {
            output.SuppressOutput();
        }

        return Task.CompletedTask;
    }
}
