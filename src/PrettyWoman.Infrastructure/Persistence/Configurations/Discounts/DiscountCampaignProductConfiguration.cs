using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PrettyWoman.Domain.Entities;

namespace PrettyWoman.Infrastructure.Persistence.Configurations.Discounts;

public class DiscountCampaignProductConfiguration : IEntityTypeConfiguration<DiscountCampaignProduct>
{
    public void Configure(EntityTypeBuilder<DiscountCampaignProduct> builder)
    {
        builder.Property(x => x.DiscountValue).HasPrecision(12, 2);

        builder.HasOne(x => x.Product).WithMany(x => x.DiscountCampaignProducts).HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.ProductVariant).WithMany(x => x.DiscountCampaignProducts).HasForeignKey(x => x.ProductVariantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.DiscountCampaign).WithMany(x => x.DiscountCampaignProducts).HasForeignKey(x => x.DiscountCampaignId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.DiscountType).WithMany().HasForeignKey(x => x.DiscountTypeId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.DiscountCampaignId, x.ProductId })
            .IsUnique()
            .HasFilter("product_id IS NOT NULL");
        builder.HasIndex(x => new { x.DiscountCampaignId, x.ProductVariantId })
            .IsUnique()
            .HasFilter("product_variant_id IS NOT NULL");

        builder.ToTable(t =>
        {
            t.HasCheckConstraint(
                "ck_discount_campaign_product_value_non_negative",
                "discount_value > 0");
            t.HasCheckConstraint(
                "ck_discount_campaign_product_exactly_one_target",
                "(product_id IS NOT NULL) <> (product_variant_id IS NOT NULL)");
        });
    }
}
