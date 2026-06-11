using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using PrettyWoman.Domain.Entities;
using PrettyWoman.Domain.Enums;

namespace PrettyWoman.Infrastructure.Persistence.Configurations.Discounts;

public class DiscountCampaignConfiguration : IEntityTypeConfiguration<DiscountCampaign>
{
    public void Configure (EntityTypeBuilder<DiscountCampaign> builder)
    {
        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(200);
        
        builder.HasIndex(x => new {x.Enabled, x.StartDate, x.EndDate});

        builder.ToTable(t =>
        {
            t.HasCheckConstraint(
                "ck_discount_campaigns_end_date_after_start_date",
                "end_date > start_date");
        });
    }
}