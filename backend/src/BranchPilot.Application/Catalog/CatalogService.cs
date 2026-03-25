using BranchPilot.Application.Abstractions.Persistence;
using BranchPilot.Application.Abstractions.Security;
using BranchPilot.Application.Abstractions.Time;
using BranchPilot.Application.Common;
using BranchPilot.Domain.Entities;
using BranchPilot.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace BranchPilot.Application.Catalog;

public sealed class CatalogService
{
    private const int DefaultPageSize = 10;
    private const int MaxPageSize = 50;

    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IDateTimeProvider _dateTimeProvider;

    public CatalogService(
        IApplicationDbContext dbContext,
        ICurrentUserContext currentUserContext,
        IDateTimeProvider dateTimeProvider)
    {
        _dbContext = dbContext;
        _currentUserContext = currentUserContext;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<PagedResponse<CategoryResponse>> GetCategoriesAsync(
        GetCatalogReferencesRequest request,
        CancellationToken cancellationToken)
    {
        EnsureAuthenticatedTenant();

        var page = request.Page > 0 ? request.Page : 1;
        var pageSize = request.PageSize > 0
            ? Math.Min(request.PageSize, MaxPageSize)
            : DefaultPageSize;
        var normalizedSearch = request.Search?.Trim().ToUpperInvariant();

        var query = _dbContext.Categories.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(normalizedSearch))
        {
            query = query.Where(category => category.Name.ToUpper().Contains(normalizedSearch));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(category => category.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(category => new CategoryResponse(category.Id, category.Name, category.Description))
            .ToListAsync(cancellationToken);

        return new PagedResponse<CategoryResponse>(items, page, pageSize, totalCount);
    }

    public async Task<CategoryResponse> CreateCategoryAsync(
        CreateCategoryRequest request,
        CancellationToken cancellationToken)
    {
        var tenantId = EnsureAuthenticatedTenant();
        var normalizedName = NormalizeName(request.Name);

        var exists = await _dbContext.Categories
            .AnyAsync(category => category.Name.ToUpper() == normalizedName, cancellationToken);

        if (exists)
        {
            throw new AppException(409, "Category already exists", "A category with this name already exists.");
        }

        var category = new Category
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = request.Name.Trim(),
            Description = request.Description?.Trim() ?? string.Empty,
            CreatedAtUtc = _dateTimeProvider.UtcNow,
        };

        await _dbContext.Categories.AddAsync(category, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new CategoryResponse(category.Id, category.Name, category.Description);
    }

    public async Task<PagedResponse<TaxProfileResponse>> GetTaxProfilesAsync(
        GetCatalogReferencesRequest request,
        CancellationToken cancellationToken)
    {
        EnsureAuthenticatedTenant();

        var page = request.Page > 0 ? request.Page : 1;
        var pageSize = request.PageSize > 0
            ? Math.Min(request.PageSize, MaxPageSize)
            : DefaultPageSize;
        var normalizedSearch = request.Search?.Trim().ToUpperInvariant();

        var query = _dbContext.TaxProfiles.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(normalizedSearch))
        {
            query = query.Where(taxProfile => taxProfile.Name.ToUpper().Contains(normalizedSearch));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(taxProfile => taxProfile.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(taxProfile => new TaxProfileResponse(taxProfile.Id, taxProfile.Name, taxProfile.Rate))
            .ToListAsync(cancellationToken);

        return new PagedResponse<TaxProfileResponse>(items, page, pageSize, totalCount);
    }

    public async Task<TaxProfileResponse> CreateTaxProfileAsync(
        CreateTaxProfileRequest request,
        CancellationToken cancellationToken)
    {
        var tenantId = EnsureAuthenticatedTenant();
        var normalizedName = NormalizeName(request.Name);

        var exists = await _dbContext.TaxProfiles
            .AnyAsync(taxProfile => taxProfile.Name.ToUpper() == normalizedName, cancellationToken);

        if (exists)
        {
            throw new AppException(409, "Tax profile already exists", "A tax profile with this name already exists.");
        }

        var taxProfile = new TaxProfile
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = request.Name.Trim(),
            Rate = request.Rate,
            CreatedAtUtc = _dateTimeProvider.UtcNow,
        };

        await _dbContext.TaxProfiles.AddAsync(taxProfile, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new TaxProfileResponse(taxProfile.Id, taxProfile.Name, taxProfile.Rate);
    }

    public async Task<CatalogOptionsResponse> GetCatalogOptionsAsync(CancellationToken cancellationToken)
    {
        EnsureAuthenticatedTenant();

        var categoriesTask = _dbContext.Categories
            .AsNoTracking()
            .OrderBy(category => category.Name)
            .Select(category => new CategoryResponse(category.Id, category.Name, category.Description))
            .ToListAsync(cancellationToken);

        var taxProfilesTask = _dbContext.TaxProfiles
            .AsNoTracking()
            .OrderBy(taxProfile => taxProfile.Name)
            .Select(taxProfile => new TaxProfileResponse(taxProfile.Id, taxProfile.Name, taxProfile.Rate))
            .ToListAsync(cancellationToken);

        var locationsTask = _dbContext.Locations
            .AsNoTracking()
            .OrderBy(location => location.Name)
            .Select(location => new LocationOptionResponse(location.Id, location.Name, location.Code))
            .ToListAsync(cancellationToken);

        await Task.WhenAll(categoriesTask, taxProfilesTask, locationsTask);

        return new CatalogOptionsResponse(
            categoriesTask.Result,
            taxProfilesTask.Result,
            locationsTask.Result,
            Enum.GetValues<CatalogItemType>()
                .Select(
                    itemType => new CatalogItemTypeOptionResponse(
                        itemType.ToString(),
                        itemType.ToString(),
                        GetItemTypeDescription(itemType)))
                .ToArray());
    }

    public async Task<CatalogItemPageResponse> GetCatalogItemsAsync(
        GetCatalogItemsRequest request,
        CancellationToken cancellationToken)
    {
        EnsureAuthenticatedTenant();

        var page = request.Page > 0 ? request.Page : 1;
        var pageSize = request.PageSize > 0
            ? Math.Min(request.PageSize, MaxPageSize)
            : DefaultPageSize;
        var normalizedSearch = request.Search?.Trim().ToUpperInvariant();
        CatalogItemType? itemTypeFilter = null;

        if (!string.IsNullOrWhiteSpace(request.ItemType))
        {
            itemTypeFilter = ParseItemType(request.ItemType);
        }

        var query = _dbContext.CatalogItems.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(normalizedSearch))
        {
            query = query.Where(
                item =>
                    item.Name.ToUpper().Contains(normalizedSearch) ||
                    item.Code.ToUpper().Contains(normalizedSearch));
        }

        if (itemTypeFilter.HasValue)
        {
            query = query.Where(item => item.ItemType == itemTypeFilter.Value);
        }

        if (request.CategoryId.HasValue)
        {
            query = query.Where(item => item.CategoryId == request.CategoryId.Value);
        }

        if (request.IsActive.HasValue)
        {
            query = query.Where(item => item.IsActive == request.IsActive.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var pageItems = await query
            .OrderBy(item => item.Name)
            .ThenBy(item => item.Code)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var itemIds = pageItems.Select(item => item.Id).ToArray();
        var categoryIds = pageItems
            .Where(item => item.CategoryId.HasValue)
            .Select(item => item.CategoryId!.Value)
            .Distinct()
            .ToArray();
        var taxProfileIds = pageItems
            .Select(item => item.TaxProfileId)
            .Distinct()
            .ToArray();

        var categories = await LoadCategoriesAsync(categoryIds, cancellationToken);
        var taxProfiles = await LoadTaxProfilesAsync(taxProfileIds, cancellationToken);
        var pricingByItem = await LoadLocationPricesByItemAsync(itemIds, cancellationToken);
        var activePromotionCounts = await LoadActivePromotionCountsAsync(itemIds, cancellationToken);

        var items = pageItems
            .Select(
                item => ToCatalogItemSummaryResponse(
                    item,
                    categories,
                    taxProfiles,
                    pricingByItem,
                    activePromotionCounts))
            .ToArray();

        return new CatalogItemPageResponse(items, page, pageSize, totalCount);
    }

    public async Task<CatalogItemDetailsResponse> GetCatalogItemAsync(Guid itemId, CancellationToken cancellationToken)
    {
        EnsureAuthenticatedTenant();

        var item = await _dbContext.CatalogItems
            .AsNoTracking()
            .SingleOrDefaultAsync(existingItem => existingItem.Id == itemId, cancellationToken);

        if (item is null)
        {
            throw new AppException(404, "Catalog item not found", "The selected catalog item could not be found.");
        }

        var category = item.CategoryId.HasValue
            ? await _dbContext.Categories
                .AsNoTracking()
                .Where(existingCategory => existingCategory.Id == item.CategoryId.Value)
                .Select(existingCategory => new CategoryResponse(existingCategory.Id, existingCategory.Name, existingCategory.Description))
                .SingleOrDefaultAsync(cancellationToken)
            : null;

        var taxProfile = await _dbContext.TaxProfiles
            .AsNoTracking()
            .Where(existingTaxProfile => existingTaxProfile.Id == item.TaxProfileId)
            .Select(existingTaxProfile => new TaxProfileResponse(existingTaxProfile.Id, existingTaxProfile.Name, existingTaxProfile.Rate))
            .SingleOrDefaultAsync(cancellationToken);

        if (taxProfile is null)
        {
            throw new AppException(404, "Tax profile not found", "The selected tax profile could not be found.");
        }

        var locationPrices = await (
            from locationPrice in _dbContext.LocationPrices.AsNoTracking()
            join location in _dbContext.Locations.AsNoTracking() on locationPrice.LocationId equals location.Id
            where locationPrice.CatalogItemId == item.Id
            orderby location.Name
            select new LocationPriceResponse(
                location.Id,
                location.Name,
                location.Code,
                locationPrice.PriceAmount,
                locationPrice.CurrencyCode))
            .ToListAsync(cancellationToken);

        var now = _dateTimeProvider.UtcNow;
        var promotions = await (
            from promotion in _dbContext.Promotions.AsNoTracking()
            join location in _dbContext.Locations.AsNoTracking() on promotion.LocationId equals location.Id
            where promotion.CatalogItemId == item.Id
            orderby promotion.StartsAtUtc descending, promotion.Name
            select new PromotionResponse(
                promotion.Id,
                promotion.Name,
                location.Id,
                location.Name,
                location.Code,
                promotion.DiscountPercentage,
                promotion.StartsAtUtc,
                promotion.EndsAtUtc,
                promotion.StartsAtUtc <= now && promotion.EndsAtUtc >= now))
            .ToListAsync(cancellationToken);

        return new CatalogItemDetailsResponse(
            item.Id,
            item.Name,
            item.Code,
            item.ItemType.ToString(),
            item.IsActive,
            item.Description,
            item.DurationInMinutes,
            category,
            taxProfile,
            locationPrices,
            promotions);
    }

    public async Task<CatalogItemDetailsResponse> CreateCatalogItemAsync(
        UpsertCatalogItemRequest request,
        CancellationToken cancellationToken)
    {
        var tenantId = EnsureAuthenticatedTenant();
        var itemType = ParseItemType(request.ItemType);
        var normalizedCode = NormalizeCode(request.Code);

        CatalogBusinessRules.EnsureValidItemShape(itemType, request.DurationInMinutes);
        CatalogBusinessRules.EnsureDistinctLocationPrices(request.LocationPrices);
        CatalogBusinessRules.EnsureSingleCurrency(request.LocationPrices);
        CatalogBusinessRules.EnsurePromotionLocationsHavePrices(request.LocationPrices, request.Promotions);
        CatalogBusinessRules.EnsureNoOverlappingPromotions(request.Promotions);

        await EnsureCodeAvailableAsync(normalizedCode, null, cancellationToken);

        var category = await ResolveCategoryAsync(request.CategoryId, cancellationToken);
        var taxProfile = await ResolveTaxProfileAsync(request.TaxProfileId, cancellationToken);
        var locationsById = await ResolveLocationsAsync(
            request.LocationPrices.Select(locationPrice => locationPrice.LocationId)
                .Concat(request.Promotions.Select(promotion => promotion.LocationId))
                .Distinct()
                .ToArray(),
            cancellationToken);

        var now = _dateTimeProvider.UtcNow;
        var item = new CatalogItem
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            CategoryId = category?.Id,
            TaxProfileId = taxProfile.Id,
            Name = request.Name.Trim(),
            Code = normalizedCode,
            ItemType = itemType,
            Description = request.Description?.Trim() ?? string.Empty,
            DurationInMinutes = request.DurationInMinutes,
            IsActive = request.IsActive,
            CreatedAtUtc = now,
        };

        var locationPrices = request.LocationPrices
            .Select(
                locationPrice => new LocationPrice
                {
                    CatalogItemId = item.Id,
                    LocationId = locationPrice.LocationId,
                    TenantId = tenantId,
                    PriceAmount = locationPrice.PriceAmount,
                    CurrencyCode = locationPrice.CurrencyCode.Trim().ToUpperInvariant(),
                    UpdatedAtUtc = now,
                })
            .ToArray();

        var promotions = request.Promotions
            .Select(
                promotion => new Promotion
                {
                    Id = Guid.NewGuid(),
                    CatalogItemId = item.Id,
                    LocationId = promotion.LocationId,
                    TenantId = tenantId,
                    Name = promotion.Name.Trim(),
                    DiscountPercentage = promotion.DiscountPercentage,
                    StartsAtUtc = promotion.StartsAtUtc,
                    EndsAtUtc = promotion.EndsAtUtc,
                    CreatedAtUtc = now,
                })
            .ToArray();

        await _dbContext.CatalogItems.AddAsync(item, cancellationToken);
        await _dbContext.LocationPrices.AddRangeAsync(locationPrices, cancellationToken);
        await _dbContext.Promotions.AddRangeAsync(promotions, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return ToCatalogItemDetailsResponse(
            item,
            ToCategoryResponse(category),
            ToTaxProfileResponse(taxProfile),
            locationPrices,
            promotions,
            locationsById);
    }

    public async Task<CatalogItemDetailsResponse> UpdateCatalogItemAsync(
        Guid itemId,
        UpsertCatalogItemRequest request,
        CancellationToken cancellationToken)
    {
        var tenantId = EnsureAuthenticatedTenant();
        var itemType = ParseItemType(request.ItemType);
        var normalizedCode = NormalizeCode(request.Code);

        CatalogBusinessRules.EnsureValidItemShape(itemType, request.DurationInMinutes);
        CatalogBusinessRules.EnsureDistinctLocationPrices(request.LocationPrices);
        CatalogBusinessRules.EnsureSingleCurrency(request.LocationPrices);
        CatalogBusinessRules.EnsurePromotionLocationsHavePrices(request.LocationPrices, request.Promotions);
        CatalogBusinessRules.EnsureNoOverlappingPromotions(request.Promotions);

        var item = await _dbContext.CatalogItems
            .SingleOrDefaultAsync(existingItem => existingItem.Id == itemId, cancellationToken);

        if (item is null)
        {
            throw new AppException(404, "Catalog item not found", "The selected catalog item could not be found.");
        }

        await EnsureCodeAvailableAsync(normalizedCode, item.Id, cancellationToken);

        var category = await ResolveCategoryAsync(request.CategoryId, cancellationToken);
        var taxProfile = await ResolveTaxProfileAsync(request.TaxProfileId, cancellationToken);
        var locationsById = await ResolveLocationsAsync(
            request.LocationPrices.Select(locationPrice => locationPrice.LocationId)
                .Concat(request.Promotions.Select(promotion => promotion.LocationId))
                .Distinct()
                .ToArray(),
            cancellationToken);

        item.CategoryId = category?.Id;
        item.TaxProfileId = taxProfile.Id;
        item.Name = request.Name.Trim();
        item.Code = normalizedCode;
        item.ItemType = itemType;
        item.Description = request.Description?.Trim() ?? string.Empty;
        item.DurationInMinutes = request.DurationInMinutes;
        item.IsActive = request.IsActive;

        var existingLocationPrices = await _dbContext.LocationPrices
            .Where(locationPrice => locationPrice.CatalogItemId == item.Id)
            .ToListAsync(cancellationToken);

        var existingPromotions = await _dbContext.Promotions
            .Where(promotion => promotion.CatalogItemId == item.Id)
            .ToListAsync(cancellationToken);

        _dbContext.LocationPrices.RemoveRange(existingLocationPrices);
        _dbContext.Promotions.RemoveRange(existingPromotions);

        var now = _dateTimeProvider.UtcNow;
        var replacementLocationPrices = request.LocationPrices
            .Select(
                locationPrice => new LocationPrice
                {
                    CatalogItemId = item.Id,
                    LocationId = locationPrice.LocationId,
                    TenantId = tenantId,
                    PriceAmount = locationPrice.PriceAmount,
                    CurrencyCode = locationPrice.CurrencyCode.Trim().ToUpperInvariant(),
                    UpdatedAtUtc = now,
                })
            .ToArray();

        var replacementPromotions = request.Promotions
            .Select(
                promotion => new Promotion
                {
                    Id = Guid.NewGuid(),
                    CatalogItemId = item.Id,
                    LocationId = promotion.LocationId,
                    TenantId = tenantId,
                    Name = promotion.Name.Trim(),
                    DiscountPercentage = promotion.DiscountPercentage,
                    StartsAtUtc = promotion.StartsAtUtc,
                    EndsAtUtc = promotion.EndsAtUtc,
                    CreatedAtUtc = now,
                })
            .ToArray();

        await _dbContext.LocationPrices.AddRangeAsync(replacementLocationPrices, cancellationToken);
        await _dbContext.Promotions.AddRangeAsync(replacementPromotions, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return ToCatalogItemDetailsResponse(
            item,
            ToCategoryResponse(category),
            ToTaxProfileResponse(taxProfile),
            replacementLocationPrices,
            replacementPromotions,
            locationsById);
    }

    private Guid EnsureAuthenticatedTenant()
    {
        if (!_currentUserContext.IsAuthenticated || _currentUserContext.TenantId is null)
        {
            throw new AppException(401, "Authentication required", "A valid access token is required.");
        }

        return _currentUserContext.TenantId.Value;
    }

    private async Task EnsureCodeAvailableAsync(
        string normalizedCode,
        Guid? existingItemId,
        CancellationToken cancellationToken)
    {
        var exists = await _dbContext.CatalogItems
            .AnyAsync(
                item => item.Code == normalizedCode &&
                        (!existingItemId.HasValue || item.Id != existingItemId.Value),
                cancellationToken);

        if (exists)
        {
            throw new AppException(409, "Catalog code already exists", "A catalog item with this code already exists.");
        }
    }

    private async Task<Category?> ResolveCategoryAsync(Guid? categoryId, CancellationToken cancellationToken)
    {
        if (!categoryId.HasValue)
        {
            return null;
        }

        var category = await _dbContext.Categories
            .SingleOrDefaultAsync(existingCategory => existingCategory.Id == categoryId.Value, cancellationToken);

        if (category is null)
        {
            throw new AppException(400, "Category not found", "The selected category could not be found in the current tenant.");
        }

        return category;
    }

    private async Task<TaxProfile> ResolveTaxProfileAsync(Guid taxProfileId, CancellationToken cancellationToken)
    {
        var taxProfile = await _dbContext.TaxProfiles
            .SingleOrDefaultAsync(existingTaxProfile => existingTaxProfile.Id == taxProfileId, cancellationToken);

        if (taxProfile is null)
        {
            throw new AppException(
                400,
                "Tax profile not found",
                "The selected tax profile could not be found in the current tenant.");
        }

        return taxProfile;
    }

    private async Task<IReadOnlyDictionary<Guid, LocationOptionResponse>> ResolveLocationsAsync(
        IReadOnlyCollection<Guid> locationIds,
        CancellationToken cancellationToken)
    {
        if (locationIds.Count == 0)
        {
            return new Dictionary<Guid, LocationOptionResponse>();
        }

        var locations = await _dbContext.Locations
            .AsNoTracking()
            .Where(location => locationIds.Contains(location.Id))
            .Select(location => new LocationOptionResponse(location.Id, location.Name, location.Code))
            .ToListAsync(cancellationToken);

        if (locations.Count != locationIds.Count)
        {
            throw new AppException(
                400,
                "Location not found",
                "One or more selected locations could not be found in the current tenant.");
        }

        return locations.ToDictionary(location => location.Id);
    }

    private async Task<IReadOnlyDictionary<Guid, CategoryResponse>> LoadCategoriesAsync(
        IReadOnlyCollection<Guid> categoryIds,
        CancellationToken cancellationToken)
    {
        if (categoryIds.Count == 0)
        {
            return new Dictionary<Guid, CategoryResponse>();
        }

        return await _dbContext.Categories
            .AsNoTracking()
            .Where(category => categoryIds.Contains(category.Id))
            .ToDictionaryAsync(
                category => category.Id,
                category => new CategoryResponse(category.Id, category.Name, category.Description),
                cancellationToken);
    }

    private async Task<IReadOnlyDictionary<Guid, TaxProfileResponse>> LoadTaxProfilesAsync(
        IReadOnlyCollection<Guid> taxProfileIds,
        CancellationToken cancellationToken)
    {
        return await _dbContext.TaxProfiles
            .AsNoTracking()
            .Where(taxProfile => taxProfileIds.Contains(taxProfile.Id))
            .ToDictionaryAsync(
                taxProfile => taxProfile.Id,
                taxProfile => new TaxProfileResponse(taxProfile.Id, taxProfile.Name, taxProfile.Rate),
                cancellationToken);
    }

    private async Task<IReadOnlyDictionary<Guid, IReadOnlyCollection<LocationPriceResponse>>> LoadLocationPricesByItemAsync(
        IReadOnlyCollection<Guid> itemIds,
        CancellationToken cancellationToken)
    {
        if (itemIds.Count == 0)
        {
            return new Dictionary<Guid, IReadOnlyCollection<LocationPriceResponse>>();
        }

        var prices = await (
            from locationPrice in _dbContext.LocationPrices.AsNoTracking()
            join location in _dbContext.Locations.AsNoTracking() on locationPrice.LocationId equals location.Id
            where itemIds.Contains(locationPrice.CatalogItemId)
            orderby location.Name
            select new
            {
                locationPrice.CatalogItemId,
                Response = new LocationPriceResponse(
                    location.Id,
                    location.Name,
                    location.Code,
                    locationPrice.PriceAmount,
                    locationPrice.CurrencyCode),
            })
            .ToListAsync(cancellationToken);

        return prices
            .GroupBy(item => item.CatalogItemId)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyCollection<LocationPriceResponse>)group.Select(item => item.Response).ToArray());
    }

    private async Task<IReadOnlyDictionary<Guid, int>> LoadActivePromotionCountsAsync(
        IReadOnlyCollection<Guid> itemIds,
        CancellationToken cancellationToken)
    {
        if (itemIds.Count == 0)
        {
            return new Dictionary<Guid, int>();
        }

        var now = _dateTimeProvider.UtcNow;

        var counts = await _dbContext.Promotions
            .AsNoTracking()
            .Where(
                promotion =>
                    itemIds.Contains(promotion.CatalogItemId) &&
                    promotion.StartsAtUtc <= now &&
                    promotion.EndsAtUtc >= now)
            .GroupBy(promotion => promotion.CatalogItemId)
            .Select(group => new { group.Key, Count = group.Count() })
            .ToListAsync(cancellationToken);

        return counts.ToDictionary(item => item.Key, item => item.Count);
    }

    private CatalogItemSummaryResponse ToCatalogItemSummaryResponse(
        CatalogItem item,
        IReadOnlyDictionary<Guid, CategoryResponse> categories,
        IReadOnlyDictionary<Guid, TaxProfileResponse> taxProfiles,
        IReadOnlyDictionary<Guid, IReadOnlyCollection<LocationPriceResponse>> pricingByItem,
        IReadOnlyDictionary<Guid, int> activePromotionCounts)
    {
        var category = item.CategoryId.HasValue && categories.TryGetValue(item.CategoryId.Value, out var matchedCategory)
            ? matchedCategory
            : null;
        var taxProfile = taxProfiles[item.TaxProfileId];
        var pricing = pricingByItem.TryGetValue(item.Id, out var locationPrices)
            ? locationPrices
            : [];
        var priceRange = pricing.Count == 0
            ? new PriceRangeResponse(0m, 0m, "EUR", 0)
            : new PriceRangeResponse(
                pricing.Min(locationPrice => locationPrice.PriceAmount),
                pricing.Max(locationPrice => locationPrice.PriceAmount),
                pricing.First().CurrencyCode,
                pricing.Count);

        return new CatalogItemSummaryResponse(
            item.Id,
            item.Name,
            item.Code,
            item.ItemType.ToString(),
            item.IsActive,
            item.DurationInMinutes,
            category,
            taxProfile,
            priceRange,
            activePromotionCounts.GetValueOrDefault(item.Id));
    }

    private CatalogItemDetailsResponse ToCatalogItemDetailsResponse(
        CatalogItem item,
        CategoryResponse? category,
        TaxProfileResponse taxProfile,
        IReadOnlyCollection<LocationPrice> locationPrices,
        IReadOnlyCollection<Promotion> promotions,
        IReadOnlyDictionary<Guid, LocationOptionResponse> locationsById)
    {
        var now = _dateTimeProvider.UtcNow;

        return new CatalogItemDetailsResponse(
            item.Id,
            item.Name,
            item.Code,
            item.ItemType.ToString(),
            item.IsActive,
            item.Description,
            item.DurationInMinutes,
            category,
            taxProfile,
            locationPrices
                .OrderBy(locationPrice => locationsById[locationPrice.LocationId].Name)
                .Select(
                    locationPrice =>
                    {
                        var location = locationsById[locationPrice.LocationId];
                        return new LocationPriceResponse(
                            location.Id,
                            location.Name,
                            location.Code,
                            locationPrice.PriceAmount,
                            locationPrice.CurrencyCode);
                    })
                .ToArray(),
            promotions
                .OrderByDescending(promotion => promotion.StartsAtUtc)
                .ThenBy(promotion => promotion.Name)
                .Select(
                    promotion =>
                    {
                        var location = locationsById[promotion.LocationId];
                        return new PromotionResponse(
                            promotion.Id,
                            promotion.Name,
                            location.Id,
                            location.Name,
                            location.Code,
                            promotion.DiscountPercentage,
                            promotion.StartsAtUtc,
                            promotion.EndsAtUtc,
                            promotion.StartsAtUtc <= now && promotion.EndsAtUtc >= now);
                    })
                .ToArray());
    }

    private static CatalogItemType ParseItemType(string value)
    {
        if (!CatalogItemTypeParser.TryParse(value, out var itemType))
        {
            throw new AppException(400, "Unsupported item type", "The selected item type is not supported.");
        }

        return itemType;
    }

    private static string NormalizeCode(string value)
    {
        return value.Trim().ToUpperInvariant();
    }

    private static string NormalizeName(string value)
    {
        return value.Trim().ToUpperInvariant();
    }

    private static CategoryResponse? ToCategoryResponse(Category? category)
    {
        return category is null
            ? null
            : new CategoryResponse(category.Id, category.Name, category.Description);
    }

    private static TaxProfileResponse ToTaxProfileResponse(TaxProfile taxProfile)
    {
        return new TaxProfileResponse(taxProfile.Id, taxProfile.Name, taxProfile.Rate);
    }

    private static string GetItemTypeDescription(CatalogItemType itemType)
    {
        return itemType switch
        {
            CatalogItemType.Product => "Physical goods or retail items with location-specific pricing.",
            CatalogItemType.Service => "Bookable or operational services with a required duration and location-specific pricing.",
            _ => "Standard catalog item.",
        };
    }
}
