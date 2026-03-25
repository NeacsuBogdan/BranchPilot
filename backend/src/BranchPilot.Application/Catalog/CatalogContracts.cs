using BranchPilot.Application.Common;

namespace BranchPilot.Application.Catalog;

public sealed record GetCatalogItemsRequest(
    int Page = 1,
    int PageSize = 10,
    string? Search = null,
    string? ItemType = null,
    Guid? CategoryId = null,
    bool? IsActive = null);

public sealed record GetCatalogReferencesRequest(
    int Page = 1,
    int PageSize = 10,
    string? Search = null);

public sealed record CreateCategoryRequest(string Name, string? Description);

public sealed record CreateTaxProfileRequest(string Name, decimal Rate);

public sealed record UpsertCatalogItemRequest(
    string Name,
    string Code,
    string ItemType,
    Guid? CategoryId,
    Guid TaxProfileId,
    string? Description,
    int? DurationInMinutes,
    bool IsActive,
    IReadOnlyCollection<LocationPriceInputRequest> LocationPrices,
    IReadOnlyCollection<PromotionInputRequest> Promotions);

public sealed record LocationPriceInputRequest(Guid LocationId, decimal PriceAmount, string CurrencyCode);

public sealed record PromotionInputRequest(
    string Name,
    Guid LocationId,
    decimal DiscountPercentage,
    DateTimeOffset StartsAtUtc,
    DateTimeOffset EndsAtUtc);

public sealed record CategoryResponse(Guid Id, string Name, string Description);

public sealed record TaxProfileResponse(Guid Id, string Name, decimal Rate);

public sealed record LocationOptionResponse(Guid Id, string Name, string Code);

public sealed record CatalogItemTypeOptionResponse(string Code, string Name, string Description);

public sealed record CatalogOptionsResponse(
    IReadOnlyCollection<CategoryResponse> Categories,
    IReadOnlyCollection<TaxProfileResponse> TaxProfiles,
    IReadOnlyCollection<LocationOptionResponse> Locations,
    IReadOnlyCollection<CatalogItemTypeOptionResponse> ItemTypes);

public sealed record PriceRangeResponse(
    decimal MinimumAmount,
    decimal MaximumAmount,
    string CurrencyCode,
    int LocationCount);

public sealed record CatalogItemSummaryResponse(
    Guid Id,
    string Name,
    string Code,
    string ItemType,
    bool IsActive,
    int? DurationInMinutes,
    CategoryResponse? Category,
    TaxProfileResponse TaxProfile,
    PriceRangeResponse PriceRange,
    int ActivePromotionCount);

public sealed record CatalogItemDetailsResponse(
    Guid Id,
    string Name,
    string Code,
    string ItemType,
    bool IsActive,
    string Description,
    int? DurationInMinutes,
    CategoryResponse? Category,
    TaxProfileResponse TaxProfile,
    IReadOnlyCollection<LocationPriceResponse> LocationPrices,
    IReadOnlyCollection<PromotionResponse> Promotions);

public sealed record LocationPriceResponse(Guid LocationId, string LocationName, string LocationCode, decimal PriceAmount, string CurrencyCode);

public sealed record PromotionResponse(
    Guid Id,
    string Name,
    Guid LocationId,
    string LocationName,
    string LocationCode,
    decimal DiscountPercentage,
    DateTimeOffset StartsAtUtc,
    DateTimeOffset EndsAtUtc,
    bool IsActive);

public sealed record CatalogItemPageResponse(
    IReadOnlyCollection<CatalogItemSummaryResponse> Items,
    int Page,
    int PageSize,
    int TotalCount) : PagedResponse<CatalogItemSummaryResponse>(Items, Page, PageSize, TotalCount);
