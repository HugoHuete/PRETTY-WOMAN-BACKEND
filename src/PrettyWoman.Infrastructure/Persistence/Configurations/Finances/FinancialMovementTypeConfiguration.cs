using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PrettyWoman.Domain.Entities;
using PrettyWoman.Domain.Enums;

namespace PrettyWoman.Infrastructure.Persistence.Configurations.Finances;

public class FinancialMovementTypeConfiguration : IEntityTypeConfiguration<FinancialMovementType>
{
    public void Configure(EntityTypeBuilder<FinancialMovementType> builder)
    {
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(50);

        builder.HasIndex(x => x.Name).IsUnique();

        builder.HasData(
            new FinancialMovementType { Id = (int)FinancialMovementTypeOption.OwnerInvestment, Name = nameof(FinancialMovementTypeOption.OwnerInvestment) },
            new FinancialMovementType { Id = (int)FinancialMovementTypeOption.SupplierPayment, Name = nameof(FinancialMovementTypeOption.SupplierPayment) },
            new FinancialMovementType { Id = (int)FinancialMovementTypeOption.SalePayment, Name = nameof(FinancialMovementTypeOption.SalePayment) },
            new FinancialMovementType { Id = (int)FinancialMovementTypeOption.Expense, Name = nameof(FinancialMovementTypeOption.Expense) },
            new FinancialMovementType { Id = (int)FinancialMovementTypeOption.OwnerWithdrawal, Name = nameof(FinancialMovementTypeOption.OwnerWithdrawal) },
            new FinancialMovementType { Id = (int)FinancialMovementTypeOption.SupplierRefund, Name = nameof(FinancialMovementTypeOption.SupplierRefund) },
            new FinancialMovementType { Id = (int)FinancialMovementTypeOption.CustomerRefund, Name = nameof(FinancialMovementTypeOption.CustomerRefund) },
            new FinancialMovementType { Id = (int)FinancialMovementTypeOption.LoanReceived, Name = nameof(FinancialMovementTypeOption.LoanReceived) },
            new FinancialMovementType { Id = (int)FinancialMovementTypeOption.LoanPayment, Name = nameof(FinancialMovementTypeOption.LoanPayment) },
            new FinancialMovementType { Id = (int)FinancialMovementTypeOption.WarehouseShippingPayment, Name = nameof(FinancialMovementTypeOption.WarehouseShippingPayment) },
            new FinancialMovementType { Id = (int)FinancialMovementTypeOption.Adjustment, Name = nameof(FinancialMovementTypeOption.Adjustment) },
            new FinancialMovementType { Id = (int)FinancialMovementTypeOption.DeliveryAgencyReconciliation, Name = nameof(FinancialMovementTypeOption.DeliveryAgencyReconciliation) }
        );
    }
}
