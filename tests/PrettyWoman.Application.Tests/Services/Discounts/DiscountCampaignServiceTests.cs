using Microsoft.EntityFrameworkCore;
using PrettyWoman.Application.DTOs.Discounts;
using PrettyWoman.Application.Exceptions;
using PrettyWoman.Application.Services;
using PrettyWoman.Domain.Entities;
using PrettyWoman.Domain.Enums;
using PrettyWoman.Infrastructure.Persistence;

namespace PrettyWoman.Application.Tests.Services.Discounts;

public class DiscountCampaignServiceTests
{
    [Fact]
    public async Task CreateAsync_CreatesCampaignWithProductsAndTrimmedName()
    {
        await using var context = CreateContext();
        var product = await AddProductAsync(context, "Vestido lino", 101);
        await AddDiscountTypesAsync(context);
        var service = CreateService(context);

        var campaignId = await service.CreateAsync(new CreateDiscountCampaignDTO
        {
            Name = "  Promo verano  ",
            StartDate = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc),
            EndDate = new DateTime(2026, 6, 30, 0, 0, 0, DateTimeKind.Utc),
            Products =
            [
                new CreateDiscountCampaignProductDTO
                {
                    ProductId = product.Id,
                    DiscountTypeId = (int)DiscountTypeOption.Percentage,
                    DiscountValue = 15
                }
            ]
        });

        var campaign = await context.DiscountCampaigns
            .Include(discountCampaign => discountCampaign.DiscountCampaignProducts)
            .SingleAsync();

        Assert.Equal(campaign.Id, campaignId);
        Assert.Equal("Promo verano", campaign.Name);
        Assert.True(campaign.Enabled);
        Assert.Single(campaign.DiscountCampaignProducts);
        Assert.Equal(product.Id, campaign.DiscountCampaignProducts.Single().ProductId);
    }

    [Fact]
    public async Task CreateAsync_ThrowsWhenProductIsRepeated()
    {
        await using var context = CreateContext();
        var product = await AddProductAsync(context, "Blusa", 102);
        await AddDiscountTypesAsync(context);
        var service = CreateService(context);

        var exception = await Assert.ThrowsAsync<AppBadRequestException>(() => service.CreateAsync(new CreateDiscountCampaignDTO
        {
            Name = "Promo repetida",
            StartDate = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc),
            EndDate = new DateTime(2026, 6, 30, 0, 0, 0, DateTimeKind.Utc),
            Products =
            [
                new CreateDiscountCampaignProductDTO
                {
                    ProductId = product.Id,
                    DiscountTypeId = (int)DiscountTypeOption.FixedAmount,
                    DiscountValue = 100
                },
                new CreateDiscountCampaignProductDTO
                {
                    ProductId = product.Id,
                    DiscountTypeId = (int)DiscountTypeOption.Percentage,
                    DiscountValue = 10
                }
            ]
        }));

        Assert.Contains($"El producto con id '{product.Id}'", exception.Message);
        Assert.Contains("repetido", exception.Message);
    }

    [Fact]
    public async Task CreateAsync_ThrowsWhenPercentageIsGreaterThanOneHundred()
    {
        await using var context = CreateContext();
        var product = await AddProductAsync(context, "Falda", 103);
        await AddDiscountTypesAsync(context);
        var service = CreateService(context);

        var exception = await Assert.ThrowsAsync<AppBadRequestException>(() => service.CreateAsync(new CreateDiscountCampaignDTO
        {
            Name = "Promo inválida",
            StartDate = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc),
            EndDate = new DateTime(2026, 6, 30, 0, 0, 0, DateTimeKind.Utc),
            Products =
            [
                new CreateDiscountCampaignProductDTO
                {
                    ProductId = product.Id,
                    DiscountTypeId = (int)DiscountTypeOption.Percentage,
                    DiscountValue = 101
                }
            ]
        }));

        Assert.Equal("El porcentaje de descuento no puede ser mayor que 100.", exception.Message);
    }

    [Fact]
    public async Task UpdateAsync_ReplacesCampaignProducts()
    {
        await using var context = CreateContext();
        var firstProduct = await AddProductAsync(context, "Vestido", 104);
        var secondProduct = await AddProductAsync(context, "Pantalón", 105);
        await AddDiscountTypesAsync(context);
        var campaign = new DiscountCampaign
        {
            Name = "Promo",
            StartDate = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc),
            EndDate = new DateTime(2026, 6, 30, 0, 0, 0, DateTimeKind.Utc),
            DiscountCampaignProducts =
            [
                new DiscountCampaignProduct
                {
                    ProductId = firstProduct.Id,
                    DiscountTypeId = (int)DiscountTypeOption.FixedAmount,
                    DiscountValue = 100
                }
            ]
        };
        context.DiscountCampaigns.Add(campaign);
        await context.SaveChangesAsync();
        var service = CreateService(context);

        await service.UpdateAsync(campaign.Id, new UpdateDiscountCampaignDTO
        {
            Name = "  Promo actualizada  ",
            StartDate = new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc),
            EndDate = new DateTime(2026, 7, 31, 0, 0, 0, DateTimeKind.Utc),
            Enabled = false,
            Products =
            [
                new UpdateDiscountCampaignProductDTO
                {
                    ProductId = secondProduct.Id,
                    DiscountTypeId = (int)DiscountTypeOption.FixedPrice,
                    DiscountValue = 450
                }
            ]
        });

        var updatedCampaign = await context.DiscountCampaigns
            .Include(discountCampaign => discountCampaign.DiscountCampaignProducts)
            .SingleAsync();

        Assert.Equal("Promo actualizada", updatedCampaign.Name);
        Assert.False(updatedCampaign.Enabled);
        Assert.Single(updatedCampaign.DiscountCampaignProducts);
        Assert.Equal(secondProduct.Id, updatedCampaign.DiscountCampaignProducts.Single().ProductId);
        Assert.Equal((int)DiscountTypeOption.FixedPrice, updatedCampaign.DiscountCampaignProducts.Single().DiscountTypeId);
    }

    [Fact]
    public async Task UpdateAsync_KeepsExistingProductAndUpdatesItsDiscountData()
    {
        await using var context = CreateContext();
        var firstProduct = await AddProductAsync(context, "Vestido", 106);
        var secondProduct = await AddProductAsync(context, "Pantalón", 107);
        await AddDiscountTypesAsync(context);
        var campaign = new DiscountCampaign
        {
            Name = "Promo",
            StartDate = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc),
            EndDate = new DateTime(2026, 6, 30, 0, 0, 0, DateTimeKind.Utc),
            DiscountCampaignProducts =
            [
                new DiscountCampaignProduct
                {
                    ProductId = firstProduct.Id,
                    DiscountTypeId = (int)DiscountTypeOption.FixedAmount,
                    DiscountValue = 100
                },
                new DiscountCampaignProduct
                {
                    ProductId = secondProduct.Id,
                    DiscountTypeId = (int)DiscountTypeOption.Percentage,
                    DiscountValue = 10
                }
            ]
        };
        context.DiscountCampaigns.Add(campaign);
        await context.SaveChangesAsync();
        var originalDiscountCampaignProductId = campaign.DiscountCampaignProducts
            .Single(product => product.ProductId == firstProduct.Id)
            .Id;
        var service = CreateService(context);

        await service.UpdateAsync(campaign.Id, new UpdateDiscountCampaignDTO
        {
            Name = "Promo actualizada",
            StartDate = new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc),
            EndDate = new DateTime(2026, 7, 31, 0, 0, 0, DateTimeKind.Utc),
            Enabled = true,
            Products =
            [
                new UpdateDiscountCampaignProductDTO
                {
                    ProductId = firstProduct.Id,
                    DiscountTypeId = (int)DiscountTypeOption.FixedPrice,
                    DiscountValue = 450
                }
            ]
        });

        var updatedCampaign = await context.DiscountCampaigns
            .Include(discountCampaign => discountCampaign.DiscountCampaignProducts)
            .SingleAsync();
        var keptProduct = updatedCampaign.DiscountCampaignProducts.Single();

        Assert.Equal(originalDiscountCampaignProductId, keptProduct.Id);
        Assert.Equal(firstProduct.Id, keptProduct.ProductId);
        Assert.Equal((int)DiscountTypeOption.FixedPrice, keptProduct.DiscountTypeId);
        Assert.Equal(450, keptProduct.DiscountValue);
    }

    [Fact]
    public async Task DisableAsync_DisablesCampaign()
    {
        await using var context = CreateContext();
        var campaign = new DiscountCampaign
        {
            Name = "Promo",
            StartDate = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc),
            EndDate = new DateTime(2026, 6, 30, 0, 0, 0, DateTimeKind.Utc),
            Enabled = true
        };
        context.DiscountCampaigns.Add(campaign);
        await context.SaveChangesAsync();
        var service = CreateService(context);

        await service.DisableAsync(campaign.Id);

        Assert.False(campaign.Enabled);
    }

    private static DiscountCampaignService CreateService(ApplicationDbContext context)
    {
        return new DiscountCampaignService(context);
    }

    private static async Task AddDiscountTypesAsync(ApplicationDbContext context)
    {
        context.DiscountTypes.AddRange(
            new DiscountType { Id = (int)DiscountTypeOption.FixedAmount, Name = nameof(DiscountTypeOption.FixedAmount) },
            new DiscountType { Id = (int)DiscountTypeOption.Percentage, Name = nameof(DiscountTypeOption.Percentage) },
            new DiscountType { Id = (int)DiscountTypeOption.FixedPrice, Name = nameof(DiscountTypeOption.FixedPrice) });
        await context.SaveChangesAsync();
    }

    private static async Task<Product> AddProductAsync(ApplicationDbContext context, string name, int code)
    {
        var product = new Product
        {
            ProductDetail = new ProductDetail
            {
                SupplierProductCode = code.ToString(),
                Code = code,
                Name = name,
                SubcategoryId = 1
            },
            Size = new Size
            {
                Id = code,
                Name = "M",
                SizeGroupId = 1,
                DisplayOrder = 1
            },
            SizeId = code,
            Quantity = 1,
            ReceivedQuantity = 1,
            AvailableQuantity = 1,
            SalePrice = 500
        };

        context.Products.Add(product);
        await context.SaveChangesAsync();

        return product;
    }

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }
}
