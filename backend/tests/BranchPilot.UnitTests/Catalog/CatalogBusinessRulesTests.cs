using BranchPilot.Application.Catalog;
using BranchPilot.Application.Common;
using BranchPilot.Domain.Enums;

namespace BranchPilot.UnitTests.Catalog;

public sealed class CatalogBusinessRulesTests
{
    [Fact]
    public void EnsureValidItemShape_WhenServiceHasNoDuration_Throws()
    {
        var exception = Assert.Throws<AppException>(
            () => CatalogBusinessRules.EnsureValidItemShape(CatalogItemType.Service, null));

        Assert.Equal(400, exception.StatusCode);
    }

    [Fact]
    public void EnsureValidItemShape_WhenProductHasDuration_Throws()
    {
        var exception = Assert.Throws<AppException>(
            () => CatalogBusinessRules.EnsureValidItemShape(CatalogItemType.Product, 30));

        Assert.Equal(400, exception.StatusCode);
    }

    [Fact]
    public void EnsureDistinctLocationPrices_WhenDuplicateLocationExists_Throws()
    {
        var locationId = Guid.NewGuid();

        var exception = Assert.Throws<AppException>(
            () => CatalogBusinessRules.EnsureDistinctLocationPrices(
                [
                    new LocationPriceInputRequest(locationId, 25m, "EUR"),
                    new LocationPriceInputRequest(locationId, 30m, "EUR"),
                ]));

        Assert.Equal(400, exception.StatusCode);
    }

    [Fact]
    public void EnsurePromotionLocationsHavePrices_WhenPromotionLocationMissingPrice_Throws()
    {
        var exception = Assert.Throws<AppException>(
            () => CatalogBusinessRules.EnsurePromotionLocationsHavePrices(
                [new LocationPriceInputRequest(Guid.NewGuid(), 40m, "EUR")],
                [new PromotionInputRequest("Launch", Guid.NewGuid(), 10m, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(2))]));

        Assert.Equal(400, exception.StatusCode);
    }

    [Fact]
    public void EnsureNoOverlappingPromotions_WhenSameLocationOverlaps_Throws()
    {
        var locationId = Guid.NewGuid();
        var start = DateTimeOffset.UtcNow;

        var exception = Assert.Throws<AppException>(
            () => CatalogBusinessRules.EnsureNoOverlappingPromotions(
                [
                    new PromotionInputRequest("Week 1", locationId, 10m, start, start.AddDays(4)),
                    new PromotionInputRequest("Week 2", locationId, 12m, start.AddDays(3), start.AddDays(7)),
                ]));

        Assert.Equal(409, exception.StatusCode);
    }
}
