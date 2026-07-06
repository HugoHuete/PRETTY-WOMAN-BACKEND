using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PrettyWoman.Domain.Entities;

namespace PrettyWoman.Infrastructure.Persistence.Configurations.Products;

public class ProductInventoryIssueConfiguration : IEntityTypeConfiguration<ProductInventoryIssue>
{
    public void Configure(EntityTypeBuilder<ProductInventoryIssue> builder)
    {
        builder.Property(x => x.Comments).HasMaxLength(500);

        builder.HasOne(x => x.Product).WithMany(x => x.ProductInventoryIssues).HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.ProductInventoryIssueType).WithMany().HasForeignKey(x => x.ProductInventoryIssueTypeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.ProductInventoryIssueStatus).WithMany().HasForeignKey(x => x.ProductInventoryIssueStatusId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.ProductId);
        builder.HasIndex(x => x.ProductInventoryIssueTypeId);
        builder.HasIndex(x => x.ProductInventoryIssueStatusId);
        builder.HasIndex(x => x.IssueDate);
        builder.HasIndex(x => x.CreatedAt);

        builder.ToTable(t =>
        {
            t.HasCheckConstraint(
                "ck_product_inventory_issues_quantity_positive",
                "quantity > 0");
        });
    }
}
