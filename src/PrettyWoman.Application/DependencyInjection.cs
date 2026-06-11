
using Microsoft.Extensions.DependencyInjection;
using PrettyWoman.Application.Interfaces;
using PrettyWoman.Application.Services;

namespace PrettyWoman.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<ISupplierService, SupplierService>();

        return services;
    }
}