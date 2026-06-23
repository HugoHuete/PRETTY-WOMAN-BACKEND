using Microsoft.EntityFrameworkCore;
using PrettyWoman.Domain.Entities;

namespace PrettyWoman.Application.Interfaces;

public interface IApplicationDbContext
{
    // Orders
    DbSet<Supplier> Suppliers { get; }
    DbSet<ShippingCompany> ShippingCompanies { get; }
    DbSet<OrderStatus> OrderStatuses { get; }
    DbSet<OrderTrackingNumber> OrderTrackingNumbers { get; }
    DbSet<Order> Orders { get; }
    DbSet<ProductReceipt> ProductReceipts { get; }
    DbSet<ProductReceiptDetail> ProductReceiptDetails { get; }

    // Products
    DbSet<Category> Categories { get; }
    DbSet<Subcategory> Subcategories { get; }
    DbSet<Size> Sizes { get; }
    DbSet<SizeGroup> SizeGroups { get; }
    DbSet<ProductDetail> ProductDetails { get; }
    DbSet<ProductImage> ProductImages { get; }
    DbSet<Product> Products { get; }
    DbSet<ProductHold> ProductHolds { get; }
    DbSet<ProductHoldStatus> ProductHoldStatuses { get; }

    // Discounts
    DbSet<DiscountType> DiscountTypes { get; }
    DbSet<DiscountSource> DiscountSources { get; }
    DbSet<DiscountCampaign> DiscountCampaigns { get; }
    DbSet<DiscountCampaignProduct> DiscountCampaignProducts { get; }

    // Sales
    DbSet<Client> Clients { get; }
    DbSet<SaleChannel> SaleChannels { get; }
    DbSet<SaleStatus> SaleStatuses { get; }
    DbSet<SaleProductStatus> SaleProductStatuses { get; }
    DbSet<Sale> Sales { get; }
    DbSet<SaleProduct> SaleProducts { get; }

    // Payments
    DbSet<PaymentMethod> PaymentMethods { get; }
    DbSet<PaymentTerminal> PaymentTerminals { get; }
    DbSet<SalePayment> SalePayments { get; }

    // Deliveries
    DbSet<DeliveryAgency> DeliveryAgencies { get; }
    DbSet<DeliveryStatus> DeliveryStatuses { get; }
    DbSet<SaleDelivery> SaleDeliveries { get; }
    DbSet<Department> Departments { get; }
    DbSet<Municipality> Municipalities { get; }

    // Finances
    DbSet<ExpenseCategory> ExpenseCategories { get; }
    DbSet<MovementDirection> MovementDirections { get; }
    DbSet<FinancialMovementType> FinancialMovementTypes { get; }
    DbSet<FinancialMovement> FinancialMovements { get; }
    DbSet<LoanOwner> LoanOwners { get; }
    DbSet<Loan> Loans { get; }
    DbSet<LoanPayment> LoanPayments { get; }
    DbSet<DollarExchangeRate> DollarExchangeRates { get; }

    // Inventory
    DbSet<InventoryMovementType> InventoryMovementTypes { get; }
    DbSet<InventoryMovement> InventoryMovements { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

