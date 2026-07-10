using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PrettyWoman.Application.Interfaces;
using PrettyWoman.Domain.Entities;

namespace PrettyWoman.Infrastructure.Persistence;

public class ApplicationDbContext : IdentityDbContext<User>, IApplicationDbContext
{
    private readonly ICurrentUserService? _currentUserService;

    public ApplicationDbContext(DbContextOptions options) : base(options)
    {
    }

    public ApplicationDbContext(DbContextOptions options, ICurrentUserService currentUserService) : base(options)
    {
        _currentUserService = currentUserService;
    }


    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ApplyAuditValues();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void ApplyAuditValues()
    {
        var now = DateTime.UtcNow;
        var userId = _currentUserService?.UserId;

        foreach (var entry in ChangeTracker.Entries<IAuditableEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = entry.Entity.CreatedAt == default ? now : entry.Entity.CreatedAt;
                entry.Entity.CreatedById ??= userId;
                continue;
            }

            if (entry.State == EntityState.Modified)
            {
                entry.Property(entity => entity.CreatedAt).IsModified = false;
                entry.Property(entity => entity.CreatedById).IsModified = false;
                entry.Entity.UpdatedAt = now;
                entry.Entity.UpdatedById = userId;
            }
        }
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.ApplyConfigurationsFromAssembly(
        typeof(ApplicationDbContext).Assembly);

    }


    // Orders
    public DbSet<Supplier> Suppliers { get; set; }
    public DbSet<ShippingCompany> ShippingCompanies { get; set; }
    public DbSet<OrderStatus> OrderStatuses { get; set; }
    public DbSet<OrderTrackingNumber> OrderTrackingNumbers { get; set; }
    public DbSet<Order> Orders { get; set; }
    public DbSet<ProductReceipt> ProductReceipts { get; set; }
    public DbSet<ProductReceiptDetail> ProductReceiptDetails { get; set; }

    // Products
    public DbSet<Category> Categories { get; set; }
    public DbSet<Subcategory> Subcategories { get; set; }
    public DbSet<Size> Sizes { get; set; }
    public DbSet<SizeGroup> SizeGroups { get; set; }
    public DbSet<ProductDetail> ProductDetails { get; set; }
    public DbSet<ProductImage> ProductImages { get; set; }
    public DbSet<Product> Products { get; set; }
    public DbSet<ProductHold> ProductHolds { get; set; }
    public DbSet<ProductHoldStatus> ProductHoldStatuses { get; set; }
    public DbSet<ProductInventoryIssue> ProductInventoryIssues { get; set; }
    public DbSet<ProductInventoryIssueType> ProductInventoryIssueTypes { get; set; }
    public DbSet<ProductInventoryIssueStatus> ProductInventoryIssueStatuses { get; set; }


    // Discounts
    public DbSet<DiscountType> DiscountTypes { get; set; }
    public DbSet<DiscountSource> DiscountSources { get; set; }
    public DbSet<DiscountCampaign> DiscountCampaigns { get; set; }
    public DbSet<DiscountCampaignProduct> DiscountCampaignProducts { get; set; }


    // Sales
    public DbSet<Client> Clients { get; set; }
    public DbSet<SaleChannel> SaleChannels { get; set; }
    public DbSet<SaleStatus> SaleStatuses { get; set; }
    public DbSet<SaleProductStatus> SaleProductStatuses { get; set; }
    public DbSet<Sale> Sales { get; set; }
    public DbSet<SaleProduct> SaleProducts { get; set; }

    // Payments
    public DbSet<PaymentMethod> PaymentMethods { get; set; }
    public DbSet<PaymentTerminal> PaymentTerminals { get; set; }
    public DbSet<SalePaymentStatus> SalePaymentStatuses { get; set; }
    public DbSet<SalePaymentMovement> SalePaymentMovements { get; set; }


    // Deliveries
    public DbSet<DeliveryAgency> DeliveryAgencies { get; set; }
    public DbSet<DeliveryStatus> DeliveryStatuses { get; set; }
    public DbSet<SaleDelivery> SaleDeliveries { get; set; }
    public DbSet<Department> Departments { get; set; }
    public DbSet<Municipality> Municipalities { get; set; }


    // Finances
    public DbSet<ExpenseCategory> ExpenseCategories { get; set; }
    public DbSet<MovementDirection> MovementDirections { get; set; }
    public DbSet<FinancialMovementType> FinancialMovementTypes { get; set; }
    public DbSet<FinancialMovement> FinancialMovements { get; set; }
    public DbSet<LoanOwner> LoanOwners { get; set; }
    public DbSet<Loan> Loans { get; set; }
    public DbSet<LoanPayment> LoanPayments { get; set; }
    public DbSet<DollarExchangeRate> DollarExchangeRates { get; set; }


    // Inventory
    public DbSet<InventoryMovementType> InventoryMovementTypes { get; set; }
    public DbSet<InventoryStockBucket> InventoryStockBuckets { get; set; }
    public DbSet<InventoryMovement> InventoryMovements { get; set; }



}




