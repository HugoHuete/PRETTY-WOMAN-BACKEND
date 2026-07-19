using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PrettyWoman.Domain.Entities;

namespace PrettyWoman.Infrastructure.Persistence.Configurations.Orders;

public class SupplierRefundConfiguration : IEntityTypeConfiguration<SupplierRefund>
{
    public void Configure(EntityTypeBuilder<SupplierRefund> builder)
    {
        builder.Property(x => x.AmountNio).HasPrecision(14, 2);
        builder.Property(x => x.Reference).HasMaxLength(100);
        builder.Property(x => x.Comments).HasMaxLength(300);

        builder.HasOne(x => x.Order)
            .WithOne(x => x.SupplierRefund)
            .HasForeignKey<SupplierRefund>(x => x.OrderId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.FinancialMovement)
            .WithOne()
            .HasForeignKey<SupplierRefund>(x => x.FinancialMovementId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.OrderId).IsUnique();
        builder.HasIndex(x => x.FinancialMovementId).IsUnique();
        builder.ToTable(table => table.HasCheckConstraint("ck_supplier_refunds_amount_positive", "amount_nio > 0"));
    }
}
