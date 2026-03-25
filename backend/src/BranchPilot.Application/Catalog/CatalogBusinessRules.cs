using BranchPilot.Application.Common;
using BranchPilot.Domain.Enums;

namespace BranchPilot.Application.Catalog;

public static class CatalogBusinessRules
{
    public static void EnsureValidItemShape(CatalogItemType itemType, int? durationInMinutes)
    {
        if (itemType == CatalogItemType.Service && (!durationInMinutes.HasValue || durationInMinutes.Value <= 0))
        {
            throw new AppException(
                400,
                "Service duration required",
                "Service catalog items must define a duration in minutes.");
        }

        if (itemType == CatalogItemType.Product && durationInMinutes.HasValue)
        {
            throw new AppException(
                400,
                "Unsupported product duration",
                "Product catalog items cannot define a service duration.");
        }
    }

    public static void EnsureDistinctLocationPrices(IReadOnlyCollection<LocationPriceInputRequest> locationPrices)
    {
        var duplicateLocationId = locationPrices
            .GroupBy(locationPrice => locationPrice.LocationId)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .FirstOrDefault();

        if (duplicateLocationId != Guid.Empty)
        {
            throw new AppException(
                400,
                "Duplicate location price",
                "Each location can only have one configured price per catalog item.");
        }
    }

    public static void EnsureSingleCurrency(IReadOnlyCollection<LocationPriceInputRequest> locationPrices)
    {
        var distinctCurrencies = locationPrices
            .Select(locationPrice => locationPrice.CurrencyCode.Trim().ToUpperInvariant())
            .Distinct()
            .ToArray();

        if (distinctCurrencies.Length > 1)
        {
            throw new AppException(
                400,
                "Mixed currencies not supported",
                "A catalog item must use the same currency across all configured locations.");
        }
    }

    public static void EnsurePromotionLocationsHavePrices(
        IReadOnlyCollection<LocationPriceInputRequest> locationPrices,
        IReadOnlyCollection<PromotionInputRequest> promotions)
    {
        var pricedLocationIds = locationPrices
            .Select(locationPrice => locationPrice.LocationId)
            .ToHashSet();

        if (promotions.Any(promotion => !pricedLocationIds.Contains(promotion.LocationId)))
        {
            throw new AppException(
                400,
                "Promotion location unavailable",
                "Promotions can only be configured for locations that already have a price.");
        }
    }

    public static void EnsureNoOverlappingPromotions(IReadOnlyCollection<PromotionInputRequest> promotions)
    {
        foreach (var locationGroup in promotions
                     .OrderBy(promotion => promotion.StartsAtUtc)
                     .GroupBy(promotion => promotion.LocationId))
        {
            PromotionInputRequest? previousPromotion = null;

            foreach (var promotion in locationGroup.OrderBy(item => item.StartsAtUtc))
            {
                if (previousPromotion is not null && promotion.StartsAtUtc < previousPromotion.EndsAtUtc)
                {
                    throw new AppException(
                        409,
                        "Promotion overlap detected",
                        "Promotions for the same location cannot overlap.");
                }

                previousPromotion = promotion;
            }
        }
    }
}
