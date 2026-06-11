using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PrettyWoman.Domain.Entities;

namespace PrettyWoman.Infrastructure.Persistence.Configurations.Finances;

public class LoanOwnerConfiguration : IEntityTypeConfiguration<LoanOwner>
{
    public void Configure(EntityTypeBuilder<LoanOwner> builder)
    {
        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(100);


        builder.HasIndex(x => x.Name).IsUnique(); ;
    }
}
