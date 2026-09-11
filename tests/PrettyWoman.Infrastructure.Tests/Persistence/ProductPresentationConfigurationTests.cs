using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using PrettyWoman.Domain.Entities;
using PrettyWoman.Infrastructure.Persistence;

namespace PrettyWoman.Infrastructure.Tests.Persistence;

public class ProductPresentationConfigurationTests
{
    [Fact]
    public void ProductPresentation_Model_HasRequiredProductAndOptionalName()
    {
        using var context = CreateContext();
        var entity = context.GetService<IDesignTimeModel>().Model.FindEntityType(typeof(ProductPresentation))!;

        Assert.False(entity.FindProperty(nameof(ProductPresentation.ProductId))!.IsNullable);
        Assert.True(entity.FindProperty(nameof(ProductPresentation.Name))!.IsNullable);
        Assert.Equal(50, entity.FindProperty(nameof(ProductPresentation.Name))!.GetMaxLength());
        Assert.Equal(50, entity.FindProperty(nameof(ProductPresentation.NormalizedName))!.GetMaxLength());
    }

    [Fact]
    public void ProductPresentation_Model_HasRequiredIndexesAndSortOrderConstraint()
    {
        using var context = CreateContext();
        var entity = context.GetService<IDesignTimeModel>().Model.FindEntityType(typeof(ProductPresentation))!;
        var indexes = entity.GetIndexes().ToArray();

        var namedPresentationIndex = Assert.Single(indexes, index =>
            index.Properties.Select(property => property.Name)
                .SequenceEqual([nameof(ProductPresentation.ProductId), nameof(ProductPresentation.NormalizedName)]));
        Assert.True(namedPresentationIndex.IsUnique);
        Assert.Equal("normalized_name is not null", namedPresentationIndex.GetFilter());

        var defaultPresentationIndex = Assert.Single(indexes, index =>
            index.Properties.Select(property => property.Name)
                .SequenceEqual([nameof(ProductPresentation.ProductId)]));
        Assert.True(defaultPresentationIndex.IsUnique);
        Assert.Equal("normalized_name is null", defaultPresentationIndex.GetFilter());

        Assert.Contains(entity.GetCheckConstraints(), constraint => constraint.Sql == "sort_order >= 0");
    }

    [Fact]
    public void ProductPresentation_Relationships_UseRequiredDeleteBehaviors()
    {
        using var context = CreateContext();
        var presentation = context.Model.FindEntityType(typeof(ProductPresentation))!;
        var variant = context.Model.FindEntityType(typeof(ProductVariant))!;
        var image = context.Model.FindEntityType(typeof(ProductImage))!;

        Assert.Equal(DeleteBehavior.Cascade, presentation.GetForeignKeys().Single(foreignKey =>
            foreignKey.PrincipalEntityType.ClrType == typeof(Product)).DeleteBehavior);
        Assert.Equal(DeleteBehavior.Restrict, variant.GetForeignKeys().Single(foreignKey =>
            foreignKey.PrincipalEntityType.ClrType == typeof(ProductPresentation)).DeleteBehavior);
        Assert.Equal(DeleteBehavior.Cascade, image.GetForeignKeys().Single(foreignKey =>
            foreignKey.PrincipalEntityType.ClrType == typeof(ProductPresentation)).DeleteBehavior);
    }

    [Fact]
    public void ProductVariant_Model_RequiresPresentationOwnedBySameProduct()
    {
        using var context = CreateContext();
        var entity = context.Model.FindEntityType(typeof(ProductVariant))!;
        var presentationForeignKey = entity.GetForeignKeys().Single(foreignKey =>
            foreignKey.PrincipalEntityType.ClrType == typeof(ProductPresentation));

        Assert.False(entity.FindProperty(nameof(ProductVariant.ProductPresentationId))!.IsNullable);
        Assert.Null(entity.FindProperty("Variant"));
        Assert.Equal(
            [nameof(ProductVariant.ProductPresentationId), nameof(ProductVariant.ProductId)],
            presentationForeignKey.Properties.Select(property => property.Name));
        var presentationSizeIndex = Assert.Single(entity.GetIndexes(), index =>
            index.Properties.Select(property => property.Name)
                .SequenceEqual([nameof(ProductVariant.ProductPresentationId), nameof(ProductVariant.SizeId)]));
        Assert.True(presentationSizeIndex.IsUnique);
    }

    [Fact]
    public void ProductImage_Model_AllowsOptionalPresentationOwnedBySameProduct()
    {
        using var context = CreateContext();
        var entity = context.Model.FindEntityType(typeof(ProductImage))!;
        var presentationForeignKey = entity.GetForeignKeys().Single(foreignKey =>
            foreignKey.PrincipalEntityType.ClrType == typeof(ProductPresentation));

        Assert.True(entity.FindProperty(nameof(ProductImage.ProductPresentationId))!.IsNullable);
        Assert.Equal(
            [nameof(ProductImage.ProductPresentationId), nameof(ProductImage.ProductId)],
            presentationForeignKey.Properties.Select(property => property.Name));

        var indexes = entity.GetIndexes().ToArray();
        var generalPrimaryIndex = Assert.Single(indexes, index => index.GetDatabaseName() == "ix_product_images_product_id_general_primary");
        Assert.True(generalPrimaryIndex.IsUnique);
        Assert.Equal("is_primary = true AND product_presentation_id IS NULL", generalPrimaryIndex.GetFilter());

        var presentationPrimaryIndex = Assert.Single(indexes, index => index.GetDatabaseName() == "ix_product_images_product_id_presentation_primary");
        Assert.True(presentationPrimaryIndex.IsUnique);
        Assert.Equal("is_primary = true AND product_presentation_id IS NOT NULL", presentationPrimaryIndex.GetFilter());
    }

    [Fact]
    public void ApplicationDbContext_ExposesProductPresentations()
    {
        using var context = CreateContext();

        Assert.NotNull(context.ProductPresentations);
    }

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql("Host=localhost;Database=pretty_woman_model_tests;Username=test;Password=test")
            .Options;

        return new ApplicationDbContext(options);
    }
}
