using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PrettyWoman.Domain.Entities;
using PrettyWoman.Domain.Enums;

namespace PrettyWoman.Infrastructure.Persistence.Configurations.Deliveries;

public class SaleDeliveryConfiguration : IEntityTypeConfiguration<SaleDelivery>
{
    public void Configure(EntityTypeBuilder<SaleDelivery> builder)
    {
        builder.Property(x => x.Code)
            .IsRequired()
            .HasMaxLength(50);
        builder.Property(x => x.UserId).IsRequired();
        builder.Property(x => x.AmountToCollect).HasPrecision(12, 2);
        builder.Property(x => x.AmountCollectedNio).HasPrecision(12, 2);
        builder.Property(x => x.AmountCollectedUsd).HasPrecision(12, 2);
        builder.Property(x => x.ShippingChargedToClient).HasPrecision(12, 2);
        builder.Property(x => x.ShippingPaidToAgency).HasPrecision(12, 2);
        builder.Property(x => x.ChangeAmount).HasPrecision(12, 2);
        builder.Property(x => x.ExchangeRate).HasPrecision(10, 4);
        builder.Property(x => x.Comments).HasMaxLength(500);

        builder.HasOne(x => x.Sale).WithMany(x => x.Deliveries).HasForeignKey(x => x.SaleId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Municipality).WithMany().HasForeignKey(x => x.MunicipalityId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.DeliveryAgency).WithMany(x => x.SaleDeliveries).HasForeignKey(x => x.DeliveryAgencyId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.DeliveryStatus).WithMany().HasForeignKey(x => x.DeliveryStatusId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Client).WithMany().HasForeignKey(x => x.ClientId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.CreatedAt);
        builder.HasIndex(x => x.MunicipalityId);
        builder.HasIndex(x => x.DeliveryAgencyId);
        builder.HasIndex(x => x.DeliveryStatusId);
        builder.HasIndex(x => new { x.UserId, x.CreatedAt });
        builder.HasIndex(x => x.SaleId)
            .IsUnique()
            .HasDatabaseName("ux_sale_deliveries_sale_id_active")
            .HasFilter($"delivery_status_id <> {(int)DeliveryStatusCode.Completed} AND delivery_status_id <> {(int)DeliveryStatusCode.Cancelled}");

        builder.ToTable(t =>
        {
            t.HasCheckConstraint(
                "ck_sale_deliveries_amount_to_collect_non_negative",
                "amount_to_collect >= 0");
            t.HasCheckConstraint(
                "ck_sale_deliveries_amount_collected_nio_non_negative",
                "amount_collected_nio >= 0");
            t.HasCheckConstraint(
                "ck_sale_deliveries_amount_collected_usd_non_negative",
                "amount_collected_usd >= 0");
            t.HasCheckConstraint(
                "ck_sale_deliveries_amount_transferred_nio_non_negative",
                "amount_transferred_nio >= 0");
            t.HasCheckConstraint(
                "ck_sale_deliveries_amount_transferred_usd_non_negative",
                "amount_transferred_usd >= 0");
            t.HasCheckConstraint(
                "ck_sale_deliveries_shipping_charged_to_client_non_negative",
                "shipping_charged_to_client >= 0");
            t.HasCheckConstraint(
                "ck_sale_deliveries_shipping_paid_to_agency_non_negative",
                "shipping_paid_to_agency >= 0");
            t.HasCheckConstraint(
                "ck_sale_deliveries_change_amount_non_negative",
                "change_amount >= 0");
            t.HasCheckConstraint(
                "ck_sale_deliveries_exchange_rate_required_for_usd",
                @"(
                    (amount_collected_usd = 0 AND amount_transferred_usd = 0 AND exchange_rate IS NULL)
                    OR
                    ((amount_collected_usd > 0 OR amount_transferred_usd > 0) AND exchange_rate > 0)
                )");
        });
    }
}
