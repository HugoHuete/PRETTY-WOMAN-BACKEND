using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PrettyWoman.Domain.Entities;
using PrettyWoman.Domain.Enums;

namespace PrettyWoman.Infrastructure.Persistence.Configurations.Products;

public class ProductInventoryIssueTypeConfiguration : IEntityTypeConfiguration<ProductInventoryIssueType>
{
    public void Configure(EntityTypeBuilder<ProductInventoryIssueType> builder)
    {
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(50);

        builder.HasIndex(x => x.Name)
            .IsUnique();

        builder.HasData(
            new ProductInventoryIssueType { Id = (int)ProductInventoryIssueTypeOption.Damaged, Name = nameof(ProductInventoryIssueTypeOption.Damaged) },
            new ProductInventoryIssueType { Id = (int)ProductInventoryIssueTypeOption.Dirty, Name = nameof(ProductInventoryIssueTypeOption.Dirty) },
            new ProductInventoryIssueType { Id = (int)ProductInventoryIssueTypeOption.Missing, Name = nameof(ProductInventoryIssueTypeOption.Missing) },
            new ProductInventoryIssueType { Id = (int)ProductInventoryIssueTypeOption.UnderReview, Name = nameof(ProductInventoryIssueTypeOption.UnderReview) },
            new ProductInventoryIssueType { Id = (int)ProductInventoryIssueTypeOption.Repairing, Name = nameof(ProductInventoryIssueTypeOption.Repairing) }
        );
    }
}
