
using Microsoft.Extensions.DependencyInjection;
using PrettyWoman.Application.Interfaces;
using PrettyWoman.Application.Services;

namespace PrettyWoman.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<ISupplierService, SupplierService>();
        services.AddScoped<IProductService, ProductService>();
        services.AddScoped<IProductImageService, ProductImageService>();
        services.AddScoped<IInventoryService, InventoryService>();
        services.AddScoped<IInventoryCatalogService, InventoryCatalogService>();
        services.AddScoped<IInventoryAdjustmentService, InventoryAdjustmentService>();
        services.AddScoped<IProductInventoryIssueService, ProductInventoryIssueService>();
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<ISubcategoryService, SubcategoryService>();
        services.AddScoped<ISizeService, SizeService>();
        services.AddScoped<IClientService, ClientService>();
        services.AddScoped<IOrderService, OrderService>();
        services.AddScoped<ISaleDeliveryService, SaleDeliveryService>();
        services.AddScoped<IDeliveryAgencyReconciliationService, DeliveryAgencyReconciliationService>();
        services.AddScoped<ISalePaymentMovementService, SalePaymentMovementService>();
        services.AddScoped<ISaleService, SaleService>();
        services.AddScoped<ISaleExchangeService, SaleExchangeService>();
        services.AddScoped<ISaleReturnService, SaleReturnService>();
        services.AddScoped<IOrderReceiptService, OrderReceiptService>();
        services.AddScoped<IDeliveryAgencyService, DeliveryAgencyService>();
        services.AddScoped<IPaymentTerminalService, PaymentTerminalService>();
        services.AddScoped<IDiscountCampaignService, DiscountCampaignService>();
        services.AddScoped<IExpenseCategoryService, ExpenseCategoryService>();
        services.AddScoped<ILoanOwnerService, LoanOwnerService>();
        services.AddScoped<ILoanService, LoanService>();
        services.AddScoped<IFinancialService, FinancialService>();

        return services;
    }
}
