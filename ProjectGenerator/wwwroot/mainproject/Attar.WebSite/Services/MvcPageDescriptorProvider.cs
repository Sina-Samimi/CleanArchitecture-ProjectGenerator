using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Attar.Application.DTOs;
using Attar.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace Attar.WebSite.Services;

public sealed class MvcPageDescriptorProvider : IPageDescriptorProvider
{
    private readonly IActionDescriptorCollectionProvider _actionDescriptorCollectionProvider;

    public MvcPageDescriptorProvider(IActionDescriptorCollectionProvider actionDescriptorCollectionProvider)
    {
        _actionDescriptorCollectionProvider = actionDescriptorCollectionProvider;
    }

    public Task<IReadOnlyCollection<PageDescriptorDto>> GetAdminPageDescriptorsAsync(CancellationToken cancellationToken)
    {
        var descriptors = _actionDescriptorCollectionProvider.ActionDescriptors.Items
            .OfType<ControllerActionDescriptor>();

        var result = new Dictionary<string, PageDescriptorDto>(StringComparer.OrdinalIgnoreCase);

        foreach (var descriptor in descriptors)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (descriptor.MethodInfo.IsDefined(typeof(NonActionAttribute), inherit: true))
            {
                continue;
            }

            var area = ResolveArea(descriptor);
            if (!string.Equals(area, "Admin", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var areaValue = area ?? string.Empty;

            var controller = descriptor.ControllerName;
            if (string.IsNullOrWhiteSpace(controller) && descriptor.RouteValues.TryGetValue("controller", out var controllerValue))
            {
                controller = controllerValue;
            }

            controller ??= string.Empty;

            var action = descriptor.ActionName;
            if (string.IsNullOrWhiteSpace(action) && descriptor.RouteValues.TryGetValue("action", out var actionValue))
            {
                action = actionValue;
            }

            action ??= string.Empty;

            if (string.IsNullOrWhiteSpace(controller) || string.IsNullOrWhiteSpace(action))
            {
                continue;
            }

            var displayName = ResolveDisplayName(descriptor) ?? $"{controller}/{action}";
            var allowAnonymous = descriptor.EndpointMetadata.Any(metadata => metadata is AllowAnonymousAttribute);

            var key = $"{areaValue}|{controller}|{action}";
            result[key] = new PageDescriptorDto(areaValue, controller, action, displayName, allowAnonymous);
        }

        return Task.FromResult<IReadOnlyCollection<PageDescriptorDto>>(result.Values.ToArray());
    }

    private static string? ResolveArea(ControllerActionDescriptor descriptor)
    {
        if (descriptor.RouteValues.TryGetValue("area", out var routeArea) && !string.IsNullOrWhiteSpace(routeArea))
        {
            return routeArea.Trim();
        }

        var controllerAreaValue = descriptor.ControllerTypeInfo.GetCustomAttribute<AreaAttribute>()?.RouteValue;
        if (!string.IsNullOrWhiteSpace(controllerAreaValue))
        {
            return controllerAreaValue.Trim();
        }

        var actionAreaValue = descriptor.MethodInfo.GetCustomAttribute<AreaAttribute>()?.RouteValue;
        if (!string.IsNullOrWhiteSpace(actionAreaValue))
        {
            return actionAreaValue.Trim();
        }

        return null;
    }

    private static string? ResolveDisplayName(ControllerActionDescriptor descriptor)
    {
        var displayAttribute = descriptor.MethodInfo.GetCustomAttribute<DisplayAttribute>();
        if (!string.IsNullOrWhiteSpace(displayAttribute?.Name))
        {
            return displayAttribute!.Name!.Trim();
        }

        var displayNameAttribute = descriptor.MethodInfo.GetCustomAttribute<DisplayNameAttribute>();
        if (!string.IsNullOrWhiteSpace(displayNameAttribute?.DisplayName))
        {
            return displayNameAttribute!.DisplayName!.Trim();
        }

        return descriptor.DisplayName;
    }
}
