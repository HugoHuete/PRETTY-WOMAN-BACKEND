using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PrettyWoman.Domain.Entities;

namespace PrettyWoman.Infrastructure.Persistence.Configurations.Finances;

public class DollarExchangeRateConfiguration : IEntityTypeConfiguration<DollarExchangeRate>
{
    public void Configure(EntityTypeBuilder<DollarExchangeRate> builder)
    {
        builder.Property(x => x.StoreRate).HasPrecision(10, 4);
        builder.Property(x => x.BankRate).HasPrecision(10, 4);


        builder.HasIndex(x => x.StartDate).IsUnique();

        builder.ToTable(t =>
        {
            t.HasCheckConstraint(
                "ck_dollar_exchange_rates_store_rate_positive",
                "store_rate > 0");
            t.HasCheckConstraint(
                "ck_dollar_exchange_rates_bank_rate_positive",
                "bank_rate > 0");
        });
    }
}
