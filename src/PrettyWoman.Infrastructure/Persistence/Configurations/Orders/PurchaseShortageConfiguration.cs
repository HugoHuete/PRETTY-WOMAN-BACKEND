using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PrettyWoman.Domain.Entities;

namespace PrettyWoman.Infrastructure.Persistence.Configurations.Orders;

public class PurchaseShortageConfiguration : IEntityTypeConfiguration<PurchaseShortage>
{
    public void Configure(EntityTypeBuilder<PurchaseShortage> builder)
    {
        builder.Property(x => x.LossAmountNio).HasPrecision(14, 2);
        builder.Property(x => x.Comments).HasMaxLength(300);

        builder.HasOne(x => x.Order)
            .WithMany(x => x.PurchaseShortages)
            .HasForeignKey(x => x.OrderId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Product)
            .WithMany(x => x.PurchaseShortages)
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.OrderId);
        builder.HasIndex(x => x.ProductId).IsUnique();
        builder.HasIndex(x => x.ShortageDate);
        builder.ToTable(table =>
        {
            table.HasCheckConstraint("ck_purchase_shortages_quantity_positive", "quantity > 0");
            table.HasCheckConstraint("ck_purchase_shortages_loss_amount_non_negative", "loss_amount_nio >= 0");
        });
    }
}
