using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PrettyWoman.Domain.Entities;

namespace PrettyWoman.Infrastructure.Persistence.Configurations.Deliveries;

public class DeliveryAgencyReconciliationConfiguration : IEntityTypeConfiguration<DeliveryAgencyReconciliation>
{
    public void Configure(EntityTypeBuilder<DeliveryAgencyReconciliation> builder)
    {
        builder.Property(x => x.ReconciliationDate).IsRequired();
        builder.Property(x => x.SettlementExchangeRate).HasPrecision(10, 4);
        builder.Property(x => x.AmountReceivedNio).HasPrecision(12, 2);
        builder.Property(x => x.AmountReceivedUsd).HasPrecision(12, 2);
        builder.Property(x => x.AmountPaidToAgencyNio).HasPrecision(12, 2);
        builder.Property(x => x.Comments).HasMaxLength(500);

        builder.HasOne(x => x.DeliveryAgency)
            .WithMany(x => x.Reconciliations)
            .HasForeignKey(x => x.DeliveryAgencyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.ReconciliationDate);
        builder.HasIndex(x => new { x.DeliveryAgencyId, x.ReconciliationDate });

        builder.ToTable("delivery_agency_reconciliations", table =>
        {
            table.HasCheckConstraint("ck_delivery_agency_reconciliations_settlement_exchange_rate_positive", "settlement_exchange_rate > 0");
            table.HasCheckConstraint("ck_delivery_agency_reconciliations_amount_received_nio_non_negative", "amount_received_nio >= 0");
            table.HasCheckConstraint("ck_delivery_agency_reconciliations_amount_received_usd_non_negative", "amount_received_usd >= 0");
            table.HasCheckConstraint("ck_delivery_agency_reconciliations_amount_paid_nio_non_negative", "amount_paid_to_agency_nio >= 0");
        });
    }
}
