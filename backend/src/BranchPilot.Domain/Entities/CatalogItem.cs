using BranchPilot.Domain.Common;
using BranchPilot.Domain.Enums;

namespace BranchPilot.Domain.Entities;

public sealed class CatalogItem : ITenantEntity
{
    public Guid Id { get; set; }

    public Guid TenantId { get; set; }

    public Guid? CategoryId { get; set; }

    public Guid TaxProfileId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Code { get; set; } = string.Empty;

    public CatalogItemType ItemType { get; set; }

    public string Description { get; set; } = string.Empty;

    public int? DurationInMinutes { get; set; }

    public bool IsActive { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }
}
