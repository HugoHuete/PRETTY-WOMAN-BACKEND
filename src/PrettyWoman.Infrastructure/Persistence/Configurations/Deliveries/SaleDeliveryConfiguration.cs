using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PrettyWoman.Domain.Entities;

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
        builder.Property(x => x.ShippingChargedToClient).HasPrecision(12, 2);
        builder.Property(x => x.ShippingPaidToAgency).HasPrecision(12, 2);
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

        builder.ToTable(t =>
        {
            t.HasCheckConstraint(
                "ck_sale_deliveries_amount_to_collect_non_negative",
                "amount_to_collect >= 0");
            t.HasCheckConstraint(
                "ck_sale_deliveries_shipping_charged_to_client_non_negative",
                "shipping_charged_to_client >= 0");
            t.HasCheckConstraint(
                "ck_sale_deliveries_shipping_paid_to_agency_non_negative",
                "shipping_paid_to_agency >= 0");
        });
    }
}
