using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PrettyWoman.Domain.Entities;

namespace PrettyWoman.Infrastructure.Persistence.Configurations.Sales;

public class SaleConfiguration : IEntityTypeConfiguration<Sale>
{
    public void Configure(EntityTypeBuilder<Sale> builder)
    {
        builder.Property(x => x.UserId).IsRequired();
        builder.Property(x => x.SubtotalBeforeDiscount).HasPrecision(12, 2);
        builder.Property(x => x.TotalDiscount).HasPrecision(12, 2);
        builder.Property(x => x.SubTotal).HasPrecision(12, 2);
        builder.Property(x => x.Comission).HasPrecision(12, 2);
        builder.Property(x => x.Total).HasPrecision(12, 2);
        builder.Property(x => x.Comments).HasMaxLength(500);

        builder.HasOne(x => x.SaleChannel).WithMany().HasForeignKey(x => x.SaleChannelId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.SaleStatus).WithMany().HasForeignKey(x => x.SaleStatusId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Client).WithMany(x => x.Sales).HasForeignKey(x => x.ClientId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Municipality).WithMany().HasForeignKey(x => x.MunicipalityId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.CreatedAt);
        builder.HasIndex(x => new { x.SaleStatusId, x.CreatedAt });
        builder.HasIndex(x => new { x.SaleChannelId, x.CreatedAt });
        builder.HasIndex(x => new { x.UserId, x.CreatedAt });
        builder.HasIndex(x => x.ClientId);
        builder.HasIndex(x => x.MunicipalityId);

        builder.ToTable(t =>
        {
            t.HasCheckConstraint(
                "ck_sales_subtotal_before_discount_non_negative",
                "subtotal_before_discount >= 0");
            t.HasCheckConstraint(
                "ck_sales_total_discount_non_negative",
                "total_discount >= 0");
            t.HasCheckConstraint(
                "ck_sales_subtotal_non_negative",
                "sub_total >= 0");
            t.HasCheckConstraint(
                "ck_sales_comission_non_negative",
                "comission >= 0");
            t.HasCheckConstraint(
                "ck_sales_total_non_negative",
                "total >= 0");
            t.HasCheckConstraint(
                "ck_sales_total_discount_not_greater_than_subtotal_before_discount",
                "total_discount <= subtotal_before_discount");
            t.HasCheckConstraint(
                "ck_sales_subtotal_matches_components",
                "sub_total = subtotal_before_discount - total_discount");
            t.HasCheckConstraint(
                "ck_sales_total_matches_components",
                "total = sub_total - comission");
        });
    }
}
