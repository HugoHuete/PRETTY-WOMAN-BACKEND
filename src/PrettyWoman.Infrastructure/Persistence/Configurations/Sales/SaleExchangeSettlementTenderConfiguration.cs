using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PrettyWoman.Domain.Entities;
using PrettyWoman.Domain.Enums;

namespace PrettyWoman.Infrastructure.Persistence.Configurations.Sales;

public class SaleExchangeSettlementTenderConfiguration : IEntityTypeConfiguration<SaleExchangeSettlementTender>
{
    public void Configure(EntityTypeBuilder<SaleExchangeSettlementTender> builder)
    {
        builder.Property(item => item.Amount).HasPrecision(14, 2);
        builder.Property(item => item.CommissionPercentage).HasPrecision(5, 2);
        builder.Property(item => item.CommissionAmount).HasPrecision(14, 2);
        builder.Property(item => item.IncomeTaxPercentage).HasPrecision(5, 2);
        builder.Property(item => item.IncomeTaxAmount).HasPrecision(14, 2);

        builder.HasOne(item => item.Settlement).WithMany(item => item.Tenders)
            .HasForeignKey(item => item.SaleExchangeSettlementId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(item => item.PaymentMethod).WithMany().HasForeignKey(item => item.PaymentMethodId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(item => item.PaymentTerminal).WithMany().HasForeignKey(item => item.PaymentTerminalId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(item => item.SaleExchangeSettlementId);
        builder.HasIndex(item => item.PaymentMethodId);
        builder.HasIndex(item => item.PaymentTerminalId);

        builder.ToTable(table =>
        {
            table.HasCheckConstraint("ck_sale_exchange_settlement_tenders_amount_positive", "amount > 0");
            table.HasCheckConstraint("ck_sale_exchange_settlement_tenders_commission_non_negative", "commission_percentage >= 0 AND commission_amount >= 0");
            table.HasCheckConstraint("ck_sale_exchange_settlement_tenders_tax_non_negative", "income_tax_percentage >= 0 AND income_tax_amount >= 0");
            table.HasCheckConstraint("ck_sale_exchange_settlement_tenders_card_requires_terminal", $"payment_method_id <> {(int)PaymentMethodOption.Card} OR payment_terminal_id IS NOT NULL");
            table.HasCheckConstraint("ck_sale_exchange_settlement_tenders_only_card_uses_terminal", $"payment_method_id = {(int)PaymentMethodOption.Card} OR payment_terminal_id IS NULL");
        });
    }
}
