using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PrettyWoman.Domain.Entities;

namespace PrettyWoman.Infrastructure.Persistence.Configurations.Payments;

public class PaymentTerminalConfiguration : IEntityTypeConfiguration<PaymentTerminal>
{
    public void Configure(EntityTypeBuilder<PaymentTerminal> builder)
    {
        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(100);
        builder.Property(x => x.ComissionPercentage).HasPrecision(5, 2);


        builder.HasIndex(x => x.Name).IsUnique();
        builder.HasIndex(x => x.Enabled);

        builder.ToTable(t =>
        {
            t.HasCheckConstraint(
                "ck_payment_terminals_comission_percentage_range",
                "comission_percentage >= 0 AND comission_percentage <= 100");
        });
    }
}
