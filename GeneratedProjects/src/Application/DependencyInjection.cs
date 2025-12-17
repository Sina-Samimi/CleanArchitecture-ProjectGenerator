using System.Reflection;
using TestAttarClone.Application.Services;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace TestAttarClone.Application;

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
