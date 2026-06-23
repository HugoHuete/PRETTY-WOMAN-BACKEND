using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PrettyWoman.Domain.Entities;
using PrettyWoman.Domain.Enums;

namespace PrettyWoman.Infrastructure.Persistence.Configurations.Products;

public class ProductInventoryIssueStatusConfiguration : IEntityTypeConfiguration<ProductInventoryIssueStatus>
{
    public void Configure(EntityTypeBuilder<ProductInventoryIssueStatus> builder)
    {
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(50);

        builder.HasIndex(x => x.Name)
            .IsUnique();

        builder.HasData(
            new ProductInventoryIssueStatus { Id = (int)ProductInventoryIssueStatusOption.Open, Name = nameof(ProductInventoryIssueStatusOption.Open) },
            new ProductInventoryIssueStatus { Id = (int)ProductInventoryIssueStatusOption.ResolvedToAvailable, Name = nameof(ProductInventoryIssueStatusOption.ResolvedToAvailable) },
            new ProductInventoryIssueStatus { Id = (int)ProductInventoryIssueStatusOption.Discarded, Name = nameof(ProductInventoryIssueStatusOption.Discarded) },
            new ProductInventoryIssueStatus { Id = (int)ProductInventoryIssueStatusOption.ConfirmedLost, Name = nameof(ProductInventoryIssueStatusOption.ConfirmedLost) },
            new ProductInventoryIssueStatus { Id = (int)ProductInventoryIssueStatusOption.Cancelled, Name = nameof(ProductInventoryIssueStatusOption.Cancelled) }
        );
    }
}
