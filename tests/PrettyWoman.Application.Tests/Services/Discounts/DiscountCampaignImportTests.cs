using Microsoft.EntityFrameworkCore;
using PrettyWoman.Application.DTOs.Discounts;
using PrettyWoman.Application.Exceptions;
using PrettyWoman.Application.Services;
using PrettyWoman.Domain.Entities;
using PrettyWoman.Domain.Enums;
using PrettyWoman.Infrastructure.Persistence;

namespace PrettyWoman.Application.Tests.Services.Discounts;

public class DiscountCampaignImportTests
{
    [Fact]
    public async Task ValidateImportAsync_ResolvesProductCode()
    {
        await using var context = CreateContext();
        var productVariant = await AddProductAsync(context, "Vestido", 101);
        await AddDiscountTypesAsync(context);
        var campaign = await AddCampaignAsync(context);
        var result = await CreateService(context).ValidateImportAsync(campaign.Id, new DiscountCampaignImportRequestDTO
        {
            Rows =
            [
                new DiscountCampaignImportRowDTO
                {
                    RowNumber = 2,
                    ProductCode = 101,
                    DiscountTypeId = (int)DiscountTypeOption.Percentage,
                    DiscountValue = 20
                }
            ]
        });

        var row = Assert.Single(result.Rows);
        Assert.True(result.IsValid);
        Assert.True(row.IsValid);
        Assert.Equal(productVariant.ProductId, row.ProductId);
        Assert.Equal("Vestido", row.ProductName);
    }

    [Fact]
    public async Task ValidateImportAsync_RejectsUnknownProductCode()
    {
        await using var context = CreateContext();
        await AddDiscountTypesAsync(context);
        var campaign = await AddCampaignAsync(context);

        var result = await CreateService(context).ValidateImportAsync(campaign.Id, new DiscountCampaignImportRequestDTO
        {
            Rows =
            [
                new DiscountCampaignImportRowDTO
                {
                    RowNumber = 2,
                    ProductCode = 999,
                    DiscountTypeId = (int)DiscountTypeOption.Percentage,
                    DiscountValue = 20
                }
            ]
        });

        var row = Assert.Single(result.Rows);
        Assert.False(result.IsValid);
        Assert.Contains("codigo de producto '999' no existe", row.Errors.Single());
    }

    [Fact]
    public async Task ValidateImportAsync_RejectsDiscountValueAboveMaximum()
    {
        await using var context = CreateContext();
        await AddProductAsync(context, "Vestido", 101);
        await AddDiscountTypesAsync(context);
        var campaign = await AddCampaignAsync(context);

        var result = await CreateService(context).ValidateImportAsync(campaign.Id, new DiscountCampaignImportRequestDTO
        {
            Rows =
            [
                new DiscountCampaignImportRowDTO
                {
                    RowNumber = 2,
                    ProductCode = 101,
                    DiscountTypeId = (int)DiscountTypeOption.FixedAmount,
                    DiscountValue = 10_000_000_000m
                }
            ]
        });

        var row = Assert.Single(result.Rows);
        Assert.False(result.IsValid);
        Assert.False(row.IsValid);
        Assert.Contains("no puede ser mayor que 9999999999", row.Errors.Single());
    }

    [Fact]
    public async Task ValidateImportAsync_RejectsNullRows()
    {
        await using var context = CreateContext();
        var campaign = await AddCampaignAsync(context);

        await Assert.ThrowsAsync<AppBadRequestException>(() =>
            CreateService(context).ValidateImportAsync(campaign.Id, new DiscountCampaignImportRequestDTO
            {
                Rows = [null!]
            }));
    }

    [Fact]
    public async Task ValidateImportAsync_RejectsPresentationFromAnotherProduct()
    {
        await using var context = CreateContext();
        var firstProductVariant = await AddProductAsync(context, "Vestido", 101);
        var secondProductVariant = await AddProductAsync(context, "Pantalon", 102);
        await AddDiscountTypesAsync(context);
        var campaign = await AddCampaignAsync(context);

        var result = await CreateService(context).ValidateImportAsync(campaign.Id, new DiscountCampaignImportRequestDTO
        {
            Rows =
            [
                new DiscountCampaignImportRowDTO
                {
                    RowNumber = 2,
                    ProductCode = firstProductVariant.Product!.Code,
                    ProductPresentationId = secondProductVariant.ProductPresentationId,
                    DiscountTypeId = (int)DiscountTypeOption.Percentage,
                    DiscountValue = 20
                }
            ]
        });

        var row = Assert.Single(result.Rows);
        Assert.False(result.IsValid);
        Assert.Contains("no pertenece al producto", row.Errors.Single());
    }

    [Fact]
    public async Task ValidateImportAsync_ReportsRepeatedTarget()
    {
        await using var context = CreateContext();
        await AddProductAsync(context, "Vestido", 101);
        await AddDiscountTypesAsync(context);
        var campaign = await AddCampaignAsync(context);

        var result = await CreateService(context).ValidateImportAsync(campaign.Id, new DiscountCampaignImportRequestDTO
        {
            Rows =
            [
                new DiscountCampaignImportRowDTO
                {
                    RowNumber = 2,
                    ProductCode = 101,
                    DiscountTypeId = (int)DiscountTypeOption.Percentage,
                    DiscountValue = 20
                },
                new DiscountCampaignImportRowDTO
                {
                    RowNumber = 3,
                    ProductCode = 101,
                    DiscountTypeId = (int)DiscountTypeOption.FixedAmount,
                    DiscountValue = 50
                }
            ]
        });

        Assert.False(result.IsValid);
        Assert.All(result.Rows, row => Assert.Contains("destino esta repetido", row.Errors.Single()));
    }

    [Fact]
    public async Task ImportAsync_ReplacesTargetsAtomically()
    {
        await using var context = CreateContext();
        var firstProductVariant = await AddProductAsync(context, "Vestido", 101);
        var secondProductVariant = await AddProductAsync(context, "Pantalon", 102);
        await AddDiscountTypesAsync(context);
        var campaign = await AddCampaignAsync(context);
        campaign.DiscountCampaignProducts =
        [
            new DiscountCampaignProduct
            {
                ProductId = firstProductVariant.ProductId,
                DiscountTypeId = (int)DiscountTypeOption.Percentage,
                DiscountValue = 10
            }
        ];
        await context.SaveChangesAsync();

        await CreateService(context).ImportAsync(campaign.Id, new DiscountCampaignImportRequestDTO
        {
            Rows =
            [
                new DiscountCampaignImportRowDTO
                {
                    RowNumber = 2,
                    ProductCode = secondProductVariant.Product!.Code,
                    ProductPresentationId = secondProductVariant.ProductPresentationId,
                    DiscountTypeId = (int)DiscountTypeOption.FixedPrice,
                    DiscountValue = 450
                }
            ]
        });

        var targets = await context.DiscountCampaignProducts
            .Where(target => target.DiscountCampaignId == campaign.Id)
            .ToListAsync();

        var target = Assert.Single(targets);
        Assert.Null(target.ProductId);
        Assert.Equal(secondProductVariant.ProductPresentationId, target.ProductPresentationId);
        Assert.Equal((int)DiscountTypeOption.FixedPrice, target.DiscountTypeId);
    }

    [Fact]
    public async Task ImportAsync_DoesNotModifyCampaignWhenAnyRowIsInvalid()
    {
        await using var context = CreateContext();
        var productVariant = await AddProductAsync(context, "Vestido", 101);
        await AddDiscountTypesAsync(context);
        var campaign = await AddCampaignAsync(context);
        campaign.DiscountCampaignProducts =
        [
            new DiscountCampaignProduct
            {
                ProductId = productVariant.ProductId,
                DiscountTypeId = (int)DiscountTypeOption.Percentage,
                DiscountValue = 10
            }
        ];
        await context.SaveChangesAsync();

        await Assert.ThrowsAsync<AppBadRequestException>(() => CreateService(context).ImportAsync(
            campaign.Id,
            new DiscountCampaignImportRequestDTO
            {
                Rows =
                [
                    new DiscountCampaignImportRowDTO
                    {
                        RowNumber = 2,
                        ProductCode = 101,
                        DiscountTypeId = (int)DiscountTypeOption.Percentage,
                        DiscountValue = 20
                    },
                    new DiscountCampaignImportRowDTO
                    {
                        RowNumber = 3,
                        ProductCode = 999,
                        DiscountTypeId = (int)DiscountTypeOption.Percentage,
                        DiscountValue = 30
                    }
                ]
            }));

        var target = await context.DiscountCampaignProducts.SingleAsync();
        Assert.Equal(productVariant.ProductId, target.ProductId);
        Assert.Equal(10, target.DiscountValue);
    }

    private static DiscountCampaignService CreateService(ApplicationDbContext context) =>
        new(context);

    private static async Task<DiscountCampaign> AddCampaignAsync(ApplicationDbContext context)
    {
        var campaign = new DiscountCampaign
        {
            Name = $"Promo {Guid.NewGuid()}",
            StartDate = DateTime.UtcNow.AddDays(-1),
            EndDate = DateTime.UtcNow.AddDays(1)
        };
        context.DiscountCampaigns.Add(campaign);
        await context.SaveChangesAsync();
        return campaign;
    }

    private static async Task AddDiscountTypesAsync(ApplicationDbContext context)
    {
        context.DiscountTypes.AddRange(
            new DiscountType { Id = (int)DiscountTypeOption.FixedAmount, Name = nameof(DiscountTypeOption.FixedAmount) },
            new DiscountType { Id = (int)DiscountTypeOption.Percentage, Name = nameof(DiscountTypeOption.Percentage) },
            new DiscountType { Id = (int)DiscountTypeOption.FixedPrice, Name = nameof(DiscountTypeOption.FixedPrice) });
        await context.SaveChangesAsync();
    }

    private static async Task<ProductVariant> AddProductAsync(ApplicationDbContext context, string name, int code)
    {
        var product = new Product
        {
            SupplierProductCode = code.ToString(),
            Code = code,
            Name = name,
            SubcategoryId = 1
        };
        var productVariant = new ProductVariant
        {
            Product = product,
            Size = new Size
            {
                Id = code,
                Name = "M",
                SizeGroupId = 1,
                DisplayOrder = 1
            },
            SizeId = code,
            ProductPresentation = new ProductPresentation
            {
                Product = product,
                Name = "Azul",
                NormalizedName = "AZUL"
            },
            Quantity = 1,
            ReceivedQuantity = 1,
            AvailableQuantity = 1,
            SalePrice = 500
        };

        context.ProductVariants.Add(productVariant);
        await context.SaveChangesAsync();
        return productVariant;
    }

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }
}
