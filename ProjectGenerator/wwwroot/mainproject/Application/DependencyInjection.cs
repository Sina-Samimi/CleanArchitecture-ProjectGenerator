using System.Reflection;
using MobiRooz.Application.Services;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace MobiRooz.Application;

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
