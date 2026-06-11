using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PrettyWoman.Domain.Entities;

namespace PrettyWoman.Infrastructure.Persistence.Configurations.Sales;

public class SaleProductConfiguration : IEntityTypeConfiguration<SaleProduct>
{
    public void Configure(EntityTypeBuilder<SaleProduct> builder)
    {
        builder.Property(x => x.CostAtSale).HasPrecision(12, 2);
        builder.Property(x => x.OriginalSalePrice).HasPrecision(12, 2);
        builder.Property(x => x.DiscountAmount).HasPrecision(12, 2);
        builder.Property(x => x.FinalSalePrice).HasPrecision(12, 2);
        builder.Property(x => x.PaymentComission).HasPrecision(12, 2);
        builder.Property(x => x.GrossProfit).HasPrecision(12, 2);

        builder.HasOne(x => x.Sale).WithMany(x => x.Products).HasForeignKey(x => x.SaleId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.DiscountSource).WithMany().HasForeignKey(x => x.DiscountSourceId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.DiscountCampaign).WithMany().HasForeignKey(x => x.DiscountCampaignId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.SaleProductStatus).WithMany().HasForeignKey(x => x.SaleProductStatusId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.SaleId);
        builder.HasIndex(x => new { x.SaleId, x.ProductId });
        builder.HasIndex(x => new { x.SaleId, x.SaleProductStatusId });
        builder.HasIndex(x => x.ProductId);
        builder.HasIndex(x => x.DiscountSourceId);
        builder.HasIndex(x => x.DiscountCampaignId);
        builder.HasIndex(x => x.SaleProductStatusId);

        builder.ToTable(t =>
        {
            t.HasCheckConstraint(
                "ck_sale_products_quantity_positive",
                "quantity > 0");
            t.HasCheckConstraint(
                "ck_sale_products_cost_at_sale_non_negative",
                "cost_at_sale >= 0");
            t.HasCheckConstraint(
                "ck_sale_products_original_sale_price_non_negative",
                "original_sale_price >= 0");
            t.HasCheckConstraint(
                "ck_sale_products_discount_amount_non_negative",
                "discount_amount >= 0");
            t.HasCheckConstraint(
                "ck_sale_products_final_sale_price_non_negative",
                "final_sale_price >= 0");
            t.HasCheckConstraint(
                "ck_sale_products_payment_comission_non_negative",
                "payment_comission >= 0");
            t.HasCheckConstraint(
                "ck_sale_products_discount_amount_not_greater_than_original_sale_price",
                "discount_amount <= original_sale_price");
            t.HasCheckConstraint(
                "ck_sale_products_gross_profit_matches_components",
                "gross_profit = final_sale_price - payment_comission - cost_at_sale");
        });
    }
}
