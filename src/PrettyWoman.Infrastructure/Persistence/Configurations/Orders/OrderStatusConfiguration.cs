using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using PrettyWoman.Domain.Entities;
using PrettyWoman.Domain.Enums;

namespace PrettyWoman.Infrastructure.Persistence.Configurations.Orders;

public class OrderStatusConfiguration : IEntityTypeConfiguration<OrderStatus>
{
    public void Configure (EntityTypeBuilder<OrderStatus> builder)
    {
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(50);

        builder.HasIndex(x => x.Name)
            .IsUnique();

        builder.HasData(
            new OrderStatus
            {
                Id = (int)OrderStatusCode.Pending,
                Name = nameof(OrderStatusCode.Pending)
            },
            new OrderStatus
            {
                Id = (int)OrderStatusCode.PartiallyReceived,
                Name = nameof(OrderStatusCode.PartiallyReceived)
            },
            new OrderStatus
            {
                Id = (int)OrderStatusCode.Received,
                Name = nameof(OrderStatusCode.Received)
            },
            new OrderStatus
            {
                Id = (int)OrderStatusCode.Cancelled,
                Name = nameof(OrderStatusCode.Cancelled)
            },
            new OrderStatus
            {
                Id = (int)OrderStatusCode.PendingRefund,
                Name = nameof(OrderStatusCode.PendingRefund)
            }
        );
    }
}
