using System.Reflection;
using Attar.Application.Services;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Attar.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediatR(Assembly.GetExecutingAssembly());

        // Domain/Application-level services
        services.AddScoped<IBackInStockNotificationService, BackInStockNotificationService>();

        return services;
    }
}
