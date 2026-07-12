using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PrettyWoman.Domain.Entities;
using PrettyWoman.Domain.Enums;

namespace PrettyWoman.Infrastructure.Persistence.Configurations.Deliveries;

public class DeliveryStatusConfiguration : IEntityTypeConfiguration<DeliveryStatus>
{
    public void Configure(EntityTypeBuilder<DeliveryStatus> builder)
    {
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(50);

        builder.HasIndex(x => x.Name).IsUnique();

        builder.HasData(
            new DeliveryStatus
            {
                Id = (int)DeliveryStatusCode.Pending,
                Name = nameof(DeliveryStatusCode.Pending)
            },
            new DeliveryStatus
            {
                Id = (int)DeliveryStatusCode.Completed,
                Name = nameof(DeliveryStatusCode.Completed)
            },
            new DeliveryStatus
            {
                Id = (int)DeliveryStatusCode.Cancelled,
                Name = nameof(DeliveryStatusCode.Cancelled)
            },
            new DeliveryStatus
            {
                Id = (int)DeliveryStatusCode.Sent,
                Name = nameof(DeliveryStatusCode.Sent)
            },
            new DeliveryStatus
            {
                Id = (int)DeliveryStatusCode.Failed,
                Name = nameof(DeliveryStatusCode.Failed)
            },
            new DeliveryStatus
            {
                Id = (int)DeliveryStatusCode.DeliveredPendingSelection,
                Name = nameof(DeliveryStatusCode.DeliveredPendingSelection)
            }
        );
    }
}
