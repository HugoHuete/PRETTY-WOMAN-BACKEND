
using Microsoft.Extensions.DependencyInjection;
using PrettyWoman.Application.Interfaces;
using PrettyWoman.Application.Services;

namespace PrettyWoman.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<ISupplierService, SupplierService>();
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<ISubcategoryService, SubcategoryService>();
        services.AddScoped<ISizeService, SizeService>();
        services.AddScoped<IDeliveryAgencyService, DeliveryAgencyService>();
        services.AddScoped<IPaymentTerminalService, PaymentTerminalService>();
        services.AddScoped<IExpenseCategoryService, ExpenseCategoryService>();
        services.AddScoped<ILoanOwnerService, LoanOwnerService>();

        return services;
    }
}
