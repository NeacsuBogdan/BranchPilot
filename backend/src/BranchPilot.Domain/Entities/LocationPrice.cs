using BranchPilot.Domain.Common;

namespace BranchPilot.Domain.Entities;

public sealed class LocationPrice : ITenantEntity
{
    public Guid CatalogItemId { get; set; }

    public Guid LocationId { get; set; }

    public Guid TenantId { get; set; }

    public decimal PriceAmount { get; set; }

    public string CurrencyCode { get; set; } = string.Empty;

    public DateTimeOffset UpdatedAtUtc { get; set; }
}
