using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PrettyWoman.Domain.Entities;

namespace PrettyWoman.Infrastructure.Persistence.Configurations.Finances;

public class FinancialMovementConfiguration : IEntityTypeConfiguration<FinancialMovement>
{
    public void Configure(EntityTypeBuilder<FinancialMovement> builder)
    {
        builder.Property(x => x.Amount).HasPrecision(12, 2);
        builder.Property(x => x.ExchangeRate).HasPrecision(10, 4);
        builder.Property(x => x.Description).HasMaxLength(300);
        builder.Property(x => x.Comments).HasMaxLength(300);

        builder.HasOne(x => x.MovementDirection).WithMany().HasForeignKey(x => x.MovementDirectionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.FinancialMovementType).WithMany().HasForeignKey(x => x.FinancialMovementTypeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.ExpenseCategory).WithMany().HasForeignKey(x => x.ExpenseCategoryId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Order).WithMany().HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.SalePaymentMovement).WithMany().HasForeignKey(x => x.SalePaymentMovementId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Loan).WithMany(x => x.FinancialMovements).HasForeignKey(x => x.LoanId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.LoanPayment).WithMany(x => x.FinancialMovements).HasForeignKey(x => x.LoanPaymentId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.MovementDate);
        builder.HasIndex(x => x.CreatedAt);
        builder.HasIndex(x => x.MovementDirectionId);
        builder.HasIndex(x => x.FinancialMovementTypeId);
        builder.HasIndex(x => x.ExpenseCategoryId);
        builder.HasIndex(x => x.OrderId);
        builder.HasIndex(x => x.SalePaymentMovementId);
        builder.HasIndex(x => x.LoanId);
        builder.HasIndex(x => x.LoanPaymentId);
        builder.HasIndex(x => new { x.FinancialMovementTypeId, x.MovementDate });
        builder.HasIndex(x => new { x.MovementDirectionId, x.MovementDate });

        builder.ToTable(t =>
        {
            t.HasCheckConstraint(
                "ck_financial_movements_amount_positive",
                "amount > 0");
            t.HasCheckConstraint(
                "ck_financial_movements_exchange_rate_positive",
                "exchange_rate > 0");
        });
    }
}

