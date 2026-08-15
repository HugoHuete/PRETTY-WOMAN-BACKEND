using Microsoft.EntityFrameworkCore;
using PrettyWoman.Application.Common.Discounts;
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
    public void Resolve_ReturnsCancelled_WhenCampaignHasBeenCancelled()
    {
        var campaign = CreateCampaign(
            startDate: new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc),
            endDate: new DateTime(2026, 6, 30, 0, 0, 0, DateTimeKind.Utc),
            cancelledAt: new DateTime(2026, 5, 15, 0, 0, 0, DateTimeKind.Utc));

        var status = DiscountCampaignStatusResolver.Resolve(
            campaign,
            new DateTime(2026, 6, 15, 0, 0, 0, DateTimeKind.Utc));

        Assert.Equal(DiscountCampaignStatusOption.Cancelled, status);
    }

    [Fact]
    public void Resolve_ReturnsScheduled_WhenCampaignHasNotStarted()
    {
        var campaign = CreateCampaign(
            startDate: new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc),
            endDate: new DateTime(2026, 6, 30, 0, 0, 0, DateTimeKind.Utc));

        var status = DiscountCampaignStatusResolver.Resolve(
            campaign,
            new DateTime(2026, 5, 31, 23, 59, 59, DateTimeKind.Utc));

        Assert.Equal(DiscountCampaignStatusOption.Scheduled, status);
    }

    [Theory]
    [InlineData(2026, 6, 1)]
    [InlineData(2026, 6, 30)]
    public void Resolve_ReturnsActive_WhenNowIsWithinCampaignDateRange(int year, int month, int day)
    {
        var campaign = CreateCampaign(
            startDate: new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc),
            endDate: new DateTime(2026, 6, 30, 0, 0, 0, DateTimeKind.Utc));

        var status = DiscountCampaignStatusResolver.Resolve(
            campaign,
            new DateTime(year, month, day, 0, 0, 0, DateTimeKind.Utc));

        Assert.Equal(DiscountCampaignStatusOption.Active, status);
    }

    [Fact]
    public void Resolve_ReturnsFinished_WhenCampaignHasEnded()
    {
        var campaign = CreateCampaign(
            startDate: new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc),
            endDate: new DateTime(2026, 6, 30, 0, 0, 0, DateTimeKind.Utc));

        var status = DiscountCampaignStatusResolver.Resolve(
            campaign,
            new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc));

        Assert.Equal(DiscountCampaignStatusOption.Finished, status);
    }

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
            StartDate = DateTime.UtcNow.AddDays(-3),
            EndDate = DateTime.UtcNow.AddDays(-2),
            Products =
            [
                new CreateDiscountCampaignProductDTO
                {
                    ProductDetailId = product.ProductDetailId,
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
        Assert.Null(campaign.CancelledAt);
        Assert.Single(campaign.DiscountCampaignProducts);
        Assert.Equal(product.ProductDetailId, campaign.DiscountCampaignProducts.Single().ProductDetailId);

        var result = await service.GetByIdAsync(campaignId);

        Assert.Equal((int)DiscountCampaignStatusOption.Finished, result.StatusId);
        Assert.Equal(nameof(DiscountCampaignStatusOption.Finished), result.StatusName);
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
                    ProductDetailId = product.ProductDetailId,
                    DiscountTypeId = (int)DiscountTypeOption.FixedAmount,
                    DiscountValue = 100
                },
                new CreateDiscountCampaignProductDTO
                {
                    ProductDetailId = product.ProductDetailId,
                    DiscountTypeId = (int)DiscountTypeOption.Percentage,
                    DiscountValue = 10
                }
            ]
        }));

        Assert.Contains($"El producto detalle con id '{product.ProductDetailId}'", exception.Message);
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
                    ProductDetailId = product.ProductDetailId,
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
            CancelledAt = new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc),
            DiscountCampaignProducts =
            [
                new DiscountCampaignProduct
                {
                    ProductDetailId = firstProduct.ProductDetailId,
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
            Products =
            [
                new UpdateDiscountCampaignProductDTO
                {
                    ProductDetailId = secondProduct.ProductDetailId,
                    DiscountTypeId = (int)DiscountTypeOption.FixedPrice,
                    DiscountValue = 450
                }
            ]
        });

        var updatedCampaign = await context.DiscountCampaigns
            .Include(discountCampaign => discountCampaign.DiscountCampaignProducts)
            .SingleAsync();

        Assert.Equal("Promo actualizada", updatedCampaign.Name);
        Assert.Equal(new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc), updatedCampaign.CancelledAt);
        Assert.Single(updatedCampaign.DiscountCampaignProducts);
        Assert.Equal(secondProduct.ProductDetailId, updatedCampaign.DiscountCampaignProducts.Single().ProductDetailId);
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
                    ProductDetailId = firstProduct.ProductDetailId,
                    DiscountTypeId = (int)DiscountTypeOption.FixedAmount,
                    DiscountValue = 100
                },
                new DiscountCampaignProduct
                {
                    ProductDetailId = secondProduct.ProductDetailId,
                    DiscountTypeId = (int)DiscountTypeOption.Percentage,
                    DiscountValue = 10
                }
            ]
        };
        context.DiscountCampaigns.Add(campaign);
        await context.SaveChangesAsync();
        var originalDiscountCampaignProductId = campaign.DiscountCampaignProducts
            .Single(product => product.ProductDetailId == firstProduct.ProductDetailId)
            .Id;
        var service = CreateService(context);

        await service.UpdateAsync(campaign.Id, new UpdateDiscountCampaignDTO
        {
            Name = "Promo actualizada",
            StartDate = new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc),
            EndDate = new DateTime(2026, 7, 31, 0, 0, 0, DateTimeKind.Utc),
            Products =
            [
                new UpdateDiscountCampaignProductDTO
                {
                    ProductDetailId = firstProduct.ProductDetailId,
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
        Assert.Equal(firstProduct.ProductDetailId, keptProduct.ProductDetailId);
        Assert.Equal((int)DiscountTypeOption.FixedPrice, keptProduct.DiscountTypeId);
        Assert.Equal(450, keptProduct.DiscountValue);
    }

    [Fact]
    public async Task CancelAsync_CancelsCampaign()
    {
        await using var context = CreateContext();
        var campaign = new DiscountCampaign
        {
            Name = "Promo",
            StartDate = DateTime.UtcNow.AddDays(-1),
            EndDate = DateTime.UtcNow.AddDays(1)
        };
        context.DiscountCampaigns.Add(campaign);
        await context.SaveChangesAsync();
        var service = CreateService(context);

        await service.CancelAsync(campaign.Id);

        Assert.NotNull(campaign.CancelledAt);

        var result = await service.GetByIdAsync(campaign.Id);

        Assert.Equal((int)DiscountCampaignStatusOption.Cancelled, result.StatusId);
        Assert.Equal(nameof(DiscountCampaignStatusOption.Cancelled), result.StatusName);
    }

    [Fact]
    public async Task ReactivateAsync_ReactivatesFutureCampaignAsScheduled()
    {
        await using var context = CreateContext();
        var campaign = new DiscountCampaign
        {
            Name = "Promo futura",
            StartDate = DateTime.UtcNow.AddDays(2),
            EndDate = DateTime.UtcNow.AddDays(3),
            CancelledAt = DateTime.UtcNow.AddDays(-1)
        };
        context.DiscountCampaigns.Add(campaign);
        await context.SaveChangesAsync();

        await CreateService(context).ReactivateAsync(campaign.Id);

        var result = await CreateService(context).GetByIdAsync(campaign.Id);

        Assert.Null(campaign.CancelledAt);
        Assert.Equal((int)DiscountCampaignStatusOption.Scheduled, result.StatusId);
        Assert.Equal(nameof(DiscountCampaignStatusOption.Scheduled), result.StatusName);
    }

    [Fact]
    public async Task ReactivateAsync_ReactivatesExpiredCampaignAsFinished()
    {
        await using var context = CreateContext();
        var campaign = new DiscountCampaign
        {
            Name = "Promo finalizada",
            StartDate = DateTime.UtcNow.AddDays(-3),
            EndDate = DateTime.UtcNow.AddDays(-2),
            CancelledAt = DateTime.UtcNow.AddDays(-1)
        };
        context.DiscountCampaigns.Add(campaign);
        await context.SaveChangesAsync();

        await CreateService(context).ReactivateAsync(campaign.Id);

        var result = await CreateService(context).GetByIdAsync(campaign.Id);

        Assert.Null(campaign.CancelledAt);
        Assert.Equal((int)DiscountCampaignStatusOption.Finished, result.StatusId);
        Assert.Equal(nameof(DiscountCampaignStatusOption.Finished), result.StatusName);
    }

    [Fact]
    public async Task CancelAsync_ThrowsWhenCampaignDoesNotExist()
    {
        await using var context = CreateContext();

        var exception = await Assert.ThrowsAsync<AppNotFoundException>(() => CreateService(context).CancelAsync(999));

        Assert.Equal("La campania de descuento con id '999' no existe.", exception.Message);
    }

    [Fact]
    public async Task ReactivateAsync_ThrowsWhenCampaignDoesNotExist()
    {
        await using var context = CreateContext();

        var exception = await Assert.ThrowsAsync<AppNotFoundException>(() => CreateService(context).ReactivateAsync(999));

        Assert.Equal("La campania de descuento con id '999' no existe.", exception.Message);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAPagedCampaignSummaryWithoutProducts()
    {
        await using var context = CreateContext();
        context.DiscountCampaigns.AddRange(
            new DiscountCampaign
            {
                Name = "Promo enero",
                StartDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                EndDate = new DateTime(2026, 1, 31, 0, 0, 0, DateTimeKind.Utc)
            },
            new DiscountCampaign
            {
                Name = "Promo febrero",
                StartDate = new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc),
                EndDate = new DateTime(2026, 2, 28, 0, 0, 0, DateTimeKind.Utc)
            },
            new DiscountCampaign
            {
                Name = "Promo marzo",
                StartDate = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc),
                EndDate = new DateTime(2026, 3, 31, 0, 0, 0, DateTimeKind.Utc)
            });
        await context.SaveChangesAsync();

        var result = await CreateService(context).GetAllAsync(new DiscountCampaignQueryDTO
        {
            Page = 2,
            PageSize = 1
        });

        var campaign = Assert.Single(result.Items);
        Assert.IsType<DiscountCampaignSummaryDTO>(campaign);
        Assert.Null(typeof(DiscountCampaignSummaryDTO).GetProperty("Products"));
        Assert.Equal("Promo febrero", campaign.Name);
        Assert.Equal((int)DiscountCampaignStatusOption.Finished, campaign.StatusId);
        Assert.Equal(nameof(DiscountCampaignStatusOption.Finished), campaign.StatusName);
        Assert.Equal(2, result.Page);
        Assert.Equal(1, result.PageSize);
        Assert.Equal(3, result.TotalCount);
        Assert.Equal(3, result.TotalPages);
    }

    [Fact]
    public async Task GetAllAsync_FiltersCampaignsByStatusWhenProvided()
    {
        await using var context = CreateContext();
        var now = DateTime.UtcNow;
        context.DiscountCampaigns.AddRange(
            new DiscountCampaign
            {
                Name = "Promo programada",
                StartDate = now.AddDays(2),
                EndDate = now.AddDays(3)
            },
            new DiscountCampaign
            {
                Name = "Promo activa",
                StartDate = now.AddDays(-1),
                EndDate = now.AddDays(1)
            },
            new DiscountCampaign
            {
                Name = "Promo finalizada",
                StartDate = now.AddDays(-3),
                EndDate = now.AddDays(-2)
            },
            new DiscountCampaign
            {
                Name = "Promo cancelada",
                StartDate = now.AddDays(-1),
                EndDate = now.AddDays(1),
                CancelledAt = now.AddDays(-1)
            });
        await context.SaveChangesAsync();

        var scheduled = await CreateService(context).GetAllAsync(new DiscountCampaignQueryDTO { Status = DiscountCampaignStatusOption.Scheduled });
        var active = await CreateService(context).GetAllAsync(new DiscountCampaignQueryDTO { Status = DiscountCampaignStatusOption.Active });
        var finished = await CreateService(context).GetAllAsync(new DiscountCampaignQueryDTO { Status = DiscountCampaignStatusOption.Finished });
        var cancelled = await CreateService(context).GetAllAsync(new DiscountCampaignQueryDTO { Status = DiscountCampaignStatusOption.Cancelled });
        var all = await CreateService(context).GetAllAsync(new DiscountCampaignQueryDTO { Status = null });

        Assert.Collection(scheduled.Items, item => Assert.Equal("Promo programada", item.Name));
        Assert.Collection(active.Items, item => Assert.Equal("Promo activa", item.Name));
        Assert.Collection(finished.Items, item => Assert.Equal("Promo finalizada", item.Name));
        Assert.Collection(cancelled.Items, item => Assert.Equal("Promo cancelada", item.Name));
        Assert.Equal(1, scheduled.TotalCount);
        Assert.Equal(1, active.TotalCount);
        Assert.Equal(1, finished.TotalCount);
        Assert.Equal(1, cancelled.TotalCount);
        Assert.Equal(4, all.TotalCount);
    }

    [Fact]
    public async Task GetAllAsync_ThrowsWhenStatusIsNotDefined()
    {
        await using var context = CreateContext();

        await Assert.ThrowsAsync<AppBadRequestException>(() => CreateService(context).GetAllAsync(new DiscountCampaignQueryDTO
        {
            Status = (DiscountCampaignStatusOption)999
        }));
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsProjectedProducts()
    {
        await using var context = CreateContext();
        var product = await AddProductAsync(context, "Vestido detalle", 108);
        await AddDiscountTypesAsync(context);
        var campaign = new DiscountCampaign
        {
            Name = "Promo detalle",
            StartDate = DateTime.UtcNow.AddDays(-1),
            EndDate = DateTime.UtcNow.AddDays(1),
            DiscountCampaignProducts =
            [
                new DiscountCampaignProduct
                {
                    ProductDetailId = product.ProductDetailId,
                    DiscountTypeId = (int)DiscountTypeOption.Percentage,
                    DiscountValue = 20
                }
            ]
        };
        context.DiscountCampaigns.Add(campaign);
        await context.SaveChangesAsync();

        var result = await CreateService(context).GetByIdAsync(campaign.Id);

        var detail = Assert.Single(result.Products);
        Assert.Equal(product.ProductDetailId, detail.ProductDetailId);
        Assert.Equal("Vestido detalle", detail.ProductName);
        Assert.Equal(product.ProductDetail!.Code, detail.ProductCode);
        Assert.Equal((int)DiscountTypeOption.Percentage, detail.DiscountTypeId);
        Assert.Equal(nameof(DiscountTypeOption.Percentage), detail.DiscountTypeName);
        Assert.Equal(20, detail.DiscountValue);
        Assert.Equal((int)DiscountCampaignStatusOption.Active, result.StatusId);
        Assert.Equal(nameof(DiscountCampaignStatusOption.Active), result.StatusName);
    }

    [Fact]
    public async Task GetAllAsync_NormalizesPaginationAndHandlesExtremePages()
    {
        await using var context = CreateContext();
        context.DiscountCampaigns.AddRange(
            new DiscountCampaign
            {
                Name = "Promo uno",
                StartDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                EndDate = new DateTime(2026, 1, 31, 0, 0, 0, DateTimeKind.Utc)
            },
            new DiscountCampaign
            {
                Name = "Promo dos",
                StartDate = new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc),
                EndDate = new DateTime(2026, 2, 28, 0, 0, 0, DateTimeKind.Utc)
            },
            new DiscountCampaign
            {
                Name = "Promo tres",
                StartDate = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc),
                EndDate = new DateTime(2026, 3, 31, 0, 0, 0, DateTimeKind.Utc)
            });
        await context.SaveChangesAsync();
        var service = CreateService(context);

        var normalized = await service.GetAllAsync(new DiscountCampaignQueryDTO { Page = 0, PageSize = 101 });
        var defaulted = await service.GetAllAsync(new DiscountCampaignQueryDTO { Page = 0, PageSize = 0 });
        var middle = await service.GetAllAsync(new DiscountCampaignQueryDTO { Page = 2, PageSize = 1 });
        var extreme = await service.GetAllAsync(new DiscountCampaignQueryDTO { Page = int.MaxValue, PageSize = 100 });

        Assert.Equal(1, normalized.Page);
        Assert.Equal(100, normalized.PageSize);
        Assert.Equal(20, defaulted.PageSize);
        Assert.False(normalized.HasPreviousPage);
        Assert.False(normalized.HasNextPage);
        Assert.True(middle.HasPreviousPage);
        Assert.True(middle.HasNextPage);
        Assert.Empty(extreme.Items);
    }

    [Fact]
    public async Task GetAllAsync_UsesNameAndIdAsStableTieBreakers()
    {
        await using var context = CreateContext();
        var startDate = new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc);
        context.DiscountCampaigns.AddRange(
            new DiscountCampaign { Name = "B", StartDate = startDate, EndDate = startDate.AddDays(1) },
            new DiscountCampaign { Name = "A", StartDate = startDate, EndDate = startDate.AddDays(1) },
            new DiscountCampaign { Name = "A", StartDate = startDate, EndDate = startDate.AddDays(1) });
        await context.SaveChangesAsync();

        var result = await CreateService(context).GetAllAsync(new DiscountCampaignQueryDTO { PageSize = 3 });

        Assert.Equal(["A", "A", "B"], result.Items.Select(item => item.Name).ToArray());
        var duplicateIds = result.Items.Where(item => item.Name == "A").Select(item => item.Id).ToArray();
        Assert.Equal(duplicateIds.OrderBy(id => id), duplicateIds);
    }

    private static DiscountCampaignService CreateService(ApplicationDbContext context)
    {
        return new DiscountCampaignService(context);
    }

    private static DiscountCampaign CreateCampaign(DateTime startDate, DateTime endDate, DateTime? cancelledAt = null)
    {
        return new DiscountCampaign
        {
            Name = "Promo",
            StartDate = startDate,
            EndDate = endDate,
            CancelledAt = cancelledAt
        };
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
