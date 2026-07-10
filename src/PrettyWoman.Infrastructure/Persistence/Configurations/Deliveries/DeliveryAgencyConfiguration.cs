using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PrettyWoman.Domain.Entities;

namespace PrettyWoman.Infrastructure.Persistence.Configurations.Deliveries;

public class DeliveryAgencyConfiguration : IEntityTypeConfiguration<DeliveryAgency>
{
    public void Configure(EntityTypeBuilder<DeliveryAgency> builder)
    {
        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(100);
        builder.Property(x => x.PhoneNumber)
            .IsRequired()
            .HasMaxLength(20);
        builder.Property(x => x.CanCollectCashOnDelivery).HasDefaultValue(false);

        builder.HasIndex(x => x.Name).IsUnique();
    }
}
