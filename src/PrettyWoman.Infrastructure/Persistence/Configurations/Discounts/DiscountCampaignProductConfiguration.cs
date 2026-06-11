using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using PrettyWoman.Domain.Entities;
using PrettyWoman.Domain.Enums;

namespace PrettyWoman.Infrastructure.Persistence.Configurations.Discounts;

public class DiscountCampaignProductConfiguration : IEntityTypeConfiguration<DiscountCampaignProduct>
{
    public void Configure(EntityTypeBuilder<DiscountCampaignProduct> builder)
    {
        builder.Property(x => x.DiscountValue).HasPrecision(12, 2);

        builder.HasOne(x => x.Product).WithMany(x => x.DiscountCampaignProducts).HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.DiscountCampaign).WithMany(x => x.DiscountCampaignProducts).HasForeignKey(x => x.DiscountCampaignId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.DiscountType).WithMany().HasForeignKey(x => x.DiscountTypeId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.DiscountCampaignId, x.ProductId }).IsUnique();

        builder.ToTable(t =>
        {
            t.HasCheckConstraint(
                "ck_discount_campaign_product_value_non_negative",
                "discount_value > 0");
        });
    }
}