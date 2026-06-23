using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PrettyWoman.Domain.Entities;

namespace PrettyWoman.Infrastructure.Persistence.Configurations.Finances;

public class LoanPaymentConfiguration : IEntityTypeConfiguration<LoanPayment>
{
    public void Configure(EntityTypeBuilder<LoanPayment> builder)
    {
        builder.Property(x => x.PrincipalAmount).HasPrecision(12, 2);
        builder.Property(x => x.InterestAmount).HasPrecision(12, 2);
        builder.Property(x => x.ExchangeRate).HasPrecision(10, 4);
        builder.Property(x => x.Comments).HasMaxLength(300);

        builder.HasOne(x => x.Loan).WithMany(x => x.LoanPayments).HasForeignKey(x => x.LoanId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.LoanId);
        builder.HasIndex(x => x.CreatedAt);
        builder.HasIndex(x => new { x.LoanId, x.CreatedAt });

        builder.ToTable(t =>
        {
            t.HasCheckConstraint(
                "ck_loan_payments_principal_amount_positive",
                "principal_amount > 0");
            t.HasCheckConstraint(
                "ck_loan_payments_interest_amount_non_negative",
                "interest_amount >= 0");
            t.HasCheckConstraint(
                "ck_loan_payments_exchange_rate_positive",
                "exchange_rate > 0");
        });
    }
}
