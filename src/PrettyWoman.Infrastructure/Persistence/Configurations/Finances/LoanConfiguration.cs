using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PrettyWoman.Domain.Entities;

namespace PrettyWoman.Infrastructure.Persistence.Configurations.Finances;

public class LoanConfiguration : IEntityTypeConfiguration<Loan>
{
    public void Configure(EntityTypeBuilder<Loan> builder)
    {
        builder.Property(x => x.InitialAmount).HasPrecision(12, 2);
        builder.Property(x => x.InitialAmountUsd).HasPrecision(12, 2);
        builder.Property(x => x.Balance).HasPrecision(12, 2);
        builder.Property(x => x.ExchangeRate).HasPrecision(10, 4);
        builder.Property(x => x.Comments).HasMaxLength(500);

        builder.HasOne(x => x.LoanOwner).WithMany(x => x.Loans).HasForeignKey(x => x.LoanOwnerId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.CreatedAt);
        builder.HasIndex(x => x.LoanOwnerId);
        builder.HasIndex(x => new { x.LoanOwnerId, x.CreatedAt });

        builder.ToTable(t =>
        {
            t.HasCheckConstraint(
                "ck_loans_initial_amount_non_negative",
                "initial_amount >= 0");
            t.HasCheckConstraint(
                "ck_loans_initial_amount_usd_non_negative",
                "initial_amount_usd >= 0");
            t.HasCheckConstraint(
                "ck_loans_balance_non_negative",
                "balance >= 0");
            t.HasCheckConstraint(
                "ck_loans_exchange_rate_positive",
                "exchange_rate > 0");
        });
    }
}
