using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PrettyWoman.Domain.Entities;

namespace PrettyWoman.Infrastructure.Persistence.Configurations.Sales;

public class SaleExchangeSettlementConfiguration : IEntityTypeConfiguration<SaleExchangeSettlement>
{
    public void Configure(EntityTypeBuilder<SaleExchangeSettlement> builder)
    {
        builder.Property(item => item.Amount).HasPrecision(14, 2);
        builder.Property(item => item.Comments).HasMaxLength(500);

        builder.HasOne(item => item.SaleExchange).WithOne(item => item.Settlement)
            .HasForeignKey<SaleExchangeSettlement>(item => item.SaleExchangeId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(item => item.SaleExchangeId).IsUnique();
        builder.HasIndex(item => item.MovementDate);

        builder.ToTable(table =>
        {
            table.HasCheckConstraint("ck_sale_exchange_settlements_amount_positive", "amount > 0");
            table.HasCheckConstraint("ck_sale_exchange_settlements_direction_valid", "movement_direction_id IN (1, 2)");
        });
    }
}
