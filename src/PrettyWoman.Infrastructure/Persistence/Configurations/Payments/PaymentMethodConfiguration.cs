using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PrettyWoman.Domain.Entities;
using PrettyWoman.Domain.Enums;

namespace PrettyWoman.Infrastructure.Persistence.Configurations.Payments;

public class PaymentMethodConfiguration : IEntityTypeConfiguration<PaymentMethod>
{
    public void Configure(EntityTypeBuilder<PaymentMethod> builder)
    {
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(50);

        builder.HasIndex(x => x.Name).IsUnique();

        builder.HasData(
            new PaymentMethod
            {
                Id = (int)PaymentMethodOption.Cash,
                Name = nameof(PaymentMethodOption.Cash)
            },
            new PaymentMethod
            {
                Id = (int)PaymentMethodOption.Transfer,
                Name = nameof(PaymentMethodOption.Transfer)
            },
            new PaymentMethod
            {
                Id = (int)PaymentMethodOption.Card,
                Name = nameof(PaymentMethodOption.Card)
            }
        );
    }
}
