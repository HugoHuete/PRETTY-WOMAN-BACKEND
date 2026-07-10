using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PrettyWoman.Domain.Entities;
using PrettyWoman.Domain.Enums;

namespace PrettyWoman.Infrastructure.Persistence.Configurations.Sales;

public class SaleConfiguration : IEntityTypeConfiguration<Sale>
{
    public void Configure(EntityTypeBuilder<Sale> builder)
    {
        builder.Property(x => x.UserId).IsRequired();
        builder.Property(x => x.Subtotal).HasPrecision(12, 2);
        builder.Property(x => x.TotalDiscount).HasPrecision(12, 2);
        builder.Property(x => x.Total).HasPrecision(12, 2);
        builder.Property(x => x.Comments).HasMaxLength(500);
        builder.Property(x => x.SaleStatusId).HasDefaultValue((int) SaleStatusOption.Pending);
        builder.Property(x => x.SalePaymentStatusId).HasDefaultValue((int) SalePaymentStatusOption.Unpaid);

        builder.HasOne(x => x.SaleChannel).WithMany().HasForeignKey(x => x.SaleChannelId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.SaleStatus).WithMany().HasForeignKey(x => x.SaleStatusId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.SalePaymentStatus).WithMany().HasForeignKey(x => x.SalePaymentStatusId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Client).WithMany(x => x.Sales).HasForeignKey(x => x.ClientId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Municipality).WithMany().HasForeignKey(x => x.MunicipalityId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.SaleDate);
        builder.HasIndex(x => x.CreatedAt);
        builder.HasIndex(x => new { x.SaleStatusId, x.SaleDate });
        builder.HasIndex(x => new { x.SalePaymentStatusId, x.SaleDate });
        builder.HasIndex(x => new { x.SaleChannelId, x.SaleDate });
        builder.HasIndex(x => new { x.UserId, x.SaleDate });
        builder.HasIndex(x => x.ClientId);
        builder.HasIndex(x => x.MunicipalityId);

        builder.ToTable(t =>
        {
            t.HasCheckConstraint(
                "ck_sales_subtotal_non_negative",
                "subtotal >= 0");
            t.HasCheckConstraint(
                "ck_sales_total_discount_non_negative",
                "total_discount >= 0");
            t.HasCheckConstraint(
                "ck_sales_total_non_negative",
                "total >= 0");
            t.HasCheckConstraint(
                "ck_sales_total_discount_not_greater_than_subtotal",
                "total_discount <= subtotal");
            t.HasCheckConstraint(
                "ck_sales_total_matches_components",
                "total = subtotal - total_discount");
        });
    }
}
