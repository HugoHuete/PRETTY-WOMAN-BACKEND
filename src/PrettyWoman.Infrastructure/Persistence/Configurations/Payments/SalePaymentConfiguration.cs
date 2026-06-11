using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PrettyWoman.Domain.Entities;

namespace PrettyWoman.Infrastructure.Persistence.Configurations.Payments;

public class SalePaymentConfiguration : IEntityTypeConfiguration<SalePayment>
{
    public void Configure(EntityTypeBuilder<SalePayment> builder)
    {
        builder.Property(x => x.UserId).IsRequired();
        builder.Property(x => x.Amount).HasPrecision(12, 2);
        builder.Property(x => x.ComissionAmount).HasPrecision(12, 2);
        builder.Property(x => x.NetReceivedAmount).HasPrecision(12, 2);

        builder.HasOne(x => x.Sale).WithMany(x => x.Payments).HasForeignKey(x => x.SaleId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.PaymentMethod).WithMany().HasForeignKey(x => x.PaymentMethodId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.PaymentTerminal).WithMany().HasForeignKey(x => x.PaymentTerminalId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.CreatedAt);
        builder.HasIndex(x => new { x.SaleId, x.CreatedAt });
        builder.HasIndex(x => x.PaymentMethodId);
        builder.HasIndex(x => x.PaymentTerminalId);
        builder.HasIndex(x => new { x.UserId, x.CreatedAt });

        builder.ToTable(t =>
        {
            t.HasCheckConstraint(
                "ck_sale_payments_amount_positive",
                "amount > 0");
            t.HasCheckConstraint(
                "ck_sale_payments_comission_amount_non_negative",
                "comission_amount >= 0");
            t.HasCheckConstraint(
                "ck_sale_payments_net_received_amount_non_negative",
                "net_received_amount >= 0");
            t.HasCheckConstraint(
                "ck_sale_payments_comission_not_greater_than_amount",
                "comission_amount <= amount");
            t.HasCheckConstraint(
                "ck_sale_payments_net_received_amount_matches_components",
                "net_received_amount = amount - comission_amount");
        });
    }
}
