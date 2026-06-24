using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PrettyWoman.Domain.Entities;
using PrettyWoman.Domain.Enums;

namespace PrettyWoman.Infrastructure.Persistence.Configurations.Payments;

public class SalePaymentStatusConfiguration : IEntityTypeConfiguration<SalePaymentStatus>
{
    public void Configure(EntityTypeBuilder<SalePaymentStatus> builder)
    {
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(50);

        builder.HasIndex(x => x.Name).IsUnique();

        builder.HasData(
            new SalePaymentStatus
            {
                Id = (int) SalePaymentStatusOption.Unpaid,
                Name = nameof(SalePaymentStatusOption.Unpaid)
            },
            new SalePaymentStatus
            {
                Id = (int) SalePaymentStatusOption.PartiallyPaid,
                Name = nameof(SalePaymentStatusOption.PartiallyPaid)
            },
            new SalePaymentStatus
            {
                Id = (int) SalePaymentStatusOption.Paid,
                Name = nameof(SalePaymentStatusOption.Paid)
            }
        );
    }
}
