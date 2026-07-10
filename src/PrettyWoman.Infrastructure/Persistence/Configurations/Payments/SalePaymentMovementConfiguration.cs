using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PrettyWoman.Domain.Entities;
using PrettyWoman.Domain.Enums;

namespace PrettyWoman.Infrastructure.Persistence.Configurations.Payments;

public class SalePaymentMovementConfiguration : IEntityTypeConfiguration<SalePaymentMovement>
{
    public void Configure(EntityTypeBuilder<SalePaymentMovement> builder)
    {
        builder.Property(x => x.UserId).IsRequired();
        builder.Property(x => x.GrossAmount).HasPrecision(12, 2);
        builder.Property(x => x.CommissionPercentage).HasPrecision(5, 2);
        builder.Property(x => x.CommissionAmount).HasPrecision(12, 2);
        builder.Property(x => x.IncomeTaxPercentage).HasPrecision(5, 2);
        builder.Property(x => x.IncomeTaxAmount).HasPrecision(12, 2);
        builder.Property(x => x.NetReceivedAmount).HasPrecision(12, 2);
        builder.Property(x => x.MovementDirectionId).HasDefaultValue((int)MovementDirectionOptions.In);
        builder.Property(x => x.UpdatedAt).IsConcurrencyToken();

        builder.HasOne(x => x.Sale).WithMany(x => x.PaymentMovements).HasForeignKey(x => x.SaleId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.MovementDirection).WithMany().HasForeignKey(x => x.MovementDirectionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.PaymentMethod).WithMany().HasForeignKey(x => x.PaymentMethodId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.PaymentTerminal).WithMany().HasForeignKey(x => x.PaymentTerminalId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.ReversedSalePaymentMovement).WithMany(x => x.ReversalMovements).HasForeignKey(x => x.ReversedSalePaymentMovementId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.MovementDate);
        builder.HasIndex(x => x.CreatedAt);
        builder.HasIndex(x => x.MovementDirectionId);
        builder.HasIndex(x => new { x.SaleId, x.MovementDate });
        builder.HasIndex(x => x.PaymentMethodId);
        builder.HasIndex(x => x.PaymentTerminalId);
        builder.HasIndex(x => x.ReversedSalePaymentMovementId);
        builder.HasIndex(x => x.ReversedSalePaymentMovementId)
            .IsUnique()
            .HasFilter($"movement_direction_id = {(int)MovementDirectionOptions.Out} AND payment_method_id = {(int)PaymentMethodOption.Card}");
        builder.HasIndex(x => new { x.UserId, x.MovementDate });

        builder.ToTable("sale_payment_movements", t =>
        {
            t.HasCheckConstraint("ck_sale_payment_movements_gross_amount_positive", "gross_amount > 0");
            t.HasCheckConstraint("ck_sale_payment_movements_commission_percentage_non_negative", "commission_percentage >= 0");
            t.HasCheckConstraint("ck_sale_payment_movements_commission_amount_non_negative", "commission_amount >= 0");
            t.HasCheckConstraint("ck_sale_payment_movements_income_tax_percentage_non_negative", "income_tax_percentage >= 0");
            t.HasCheckConstraint("ck_sale_payment_movements_income_tax_amount_non_negative", "income_tax_amount >= 0");
            t.HasCheckConstraint("ck_sale_payment_movements_net_received_amount_non_negative", "net_received_amount >= 0");
            t.HasCheckConstraint("ck_sale_payment_movements_commission_not_greater_than_amount", "commission_amount + income_tax_amount <= gross_amount");
            t.HasCheckConstraint("ck_sale_payment_movements_net_received_amount_matches_components", "net_received_amount = gross_amount - commission_amount - income_tax_amount");
            t.HasCheckConstraint("ck_sale_payment_movements_in_does_not_reverse", $"movement_direction_id <> {(int)MovementDirectionOptions.In} OR reversed_sale_payment_movement_id IS NULL");
            t.HasCheckConstraint("ck_sale_payment_movements_card_refund_reverses_original", $"movement_direction_id <> {(int)MovementDirectionOptions.Out} OR payment_method_id <> {(int)PaymentMethodOption.Card} OR reversed_sale_payment_movement_id IS NOT NULL");
            t.HasCheckConstraint("ck_sale_payment_movements_card_requires_terminal", $"payment_method_id <> {(int)PaymentMethodOption.Card} OR payment_terminal_id IS NOT NULL");
            t.HasCheckConstraint("ck_sale_payment_movements_only_card_uses_terminal", $"payment_method_id = {(int)PaymentMethodOption.Card} OR payment_terminal_id IS NULL");
        });
    }
}
