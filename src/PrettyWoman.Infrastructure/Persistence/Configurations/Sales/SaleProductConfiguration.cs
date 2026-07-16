using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PrettyWoman.Domain.Entities;

namespace PrettyWoman.Infrastructure.Persistence.Configurations.Sales;

public class SaleProductConfiguration : IEntityTypeConfiguration<SaleProduct>
{
    public void Configure(EntityTypeBuilder<SaleProduct> builder)
    {
        builder.Property(x => x.UnitCostAtSale).HasPrecision(18, 6);
        builder.Property(x => x.TotalCostAtSale).HasPrecision(18, 6);
        builder.Property(x => x.OriginalUnitPrice).HasPrecision(14, 2);
        builder.Property(x => x.DiscountAmount).HasPrecision(14, 2);
        builder.Property(x => x.FinalUnitPrice).HasPrecision(14, 2);
        builder.Property(x => x.LineTotal).HasPrecision(14, 2);
        builder.Property(x => x.GrossProfit).HasPrecision(18, 6);

        builder.HasOne(x => x.Sale).WithMany(x => x.Products).HasForeignKey(x => x.SaleId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.DiscountSource).WithMany().HasForeignKey(x => x.DiscountSourceId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.DiscountCampaign).WithMany().HasForeignKey(x => x.DiscountCampaignId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.SaleId);
        builder.HasIndex(x => new { x.SaleId, x.ProductId });
        builder.HasIndex(x => x.ProductId);
        builder.HasIndex(x => x.DiscountSourceId);
        builder.HasIndex(x => x.DiscountCampaignId);

        builder.ToTable(t =>
        {
            t.HasCheckConstraint(
                "ck_sale_details_quantity_positive",
                "quantity > 0");

            t.HasCheckConstraint(
                "ck_sale_details_original_unit_price_non_negative",
                "original_unit_price >= 0");

            t.HasCheckConstraint(
                "ck_sale_details_discount_amount_non_negative",
                "discount_amount >= 0");

            t.HasCheckConstraint(
                "ck_sale_details_discount_amount_not_greater_than_original_unit_price",
                "discount_amount <= original_unit_price");

            t.HasCheckConstraint(
                "ck_sale_details_final_unit_price_non_negative",
                "final_unit_price >= 0");

            t.HasCheckConstraint(
                "ck_sale_details_line_total_non_negative",
                "line_total >= 0");

            t.HasCheckConstraint(
                "ck_sale_details_unit_cost_at_sale_non_negative",
                "unit_cost_at_sale >= 0");

            t.HasCheckConstraint(
                "ck_sale_details_total_cost_at_sale_non_negative",
                "total_cost_at_sale >= 0");

            t.HasCheckConstraint(
                "ck_sale_details_gross_profit_matches_components",
                "gross_profit = line_total - total_cost_at_sale");
        });
    }
}

