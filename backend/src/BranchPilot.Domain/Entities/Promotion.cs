using BranchPilot.Domain.Common;

namespace BranchPilot.Domain.Entities;

public sealed class Promotion : ITenantEntity
{
    public Guid Id { get; set; }

    public Guid TenantId { get; set; }

    public Guid CatalogItemId { get; set; }

    public Guid LocationId { get; set; }

    public string Name { get; set; } = string.Empty;

    public decimal DiscountPercentage { get; set; }

    public DateTimeOffset StartsAtUtc { get; set; }

    public DateTimeOffset EndsAtUtc { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }
}
