using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using PrettyWoman.Domain.Entities;
using PrettyWoman.Infrastructure.Persistence;

namespace PrettyWoman.Infrastructure.Tests.Persistence;

public class DiscountCampaignProductConfigurationTests
{
    [Fact]
    public void DiscountCampaignProduct_Model_UsesProductOrPresentationAsTarget()
    {
        using var context = CreateContext();
        var entity = context.GetService<IDesignTimeModel>().Model.FindEntityType(typeof(DiscountCampaignProduct))!;

        Assert.NotNull(entity.FindProperty(nameof(DiscountCampaignProduct.ProductId)));
        Assert.NotNull(entity.FindProperty("ProductPresentationId"));
        Assert.Null(entity.FindProperty("ProductVariantId"));
        Assert.NotNull(entity.FindNavigation("ProductPresentation"));

        var presentationForeignKey = Assert.Single(entity.GetForeignKeys(), foreignKey =>
            foreignKey.PrincipalEntityType.ClrType == typeof(ProductPresentation));
        Assert.Equal(DeleteBehavior.Restrict, presentationForeignKey.DeleteBehavior);

        Assert.Contains(entity.GetCheckConstraints(), constraint =>
            constraint.Sql == "(product_id IS NOT NULL) <> (product_presentation_id IS NOT NULL)");
    }

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql("Host=localhost;Database=pretty_woman_model_tests;Username=test;Password=test")
            .Options;

        return new ApplicationDbContext(options);
    }
}
