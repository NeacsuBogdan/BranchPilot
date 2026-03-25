using BranchPilot.Domain.Common;

namespace BranchPilot.Domain.Entities;

public sealed class BookingLine : ITenantEntity
{
    public Guid Id { get; set; }

    public Guid TenantId { get; set; }

    public Guid BookingId { get; set; }

    public Guid CatalogItemId { get; set; }

    public string ItemName { get; set; } = string.Empty;

    public int Quantity { get; set; }

    public int DurationInMinutes { get; set; }

    public decimal UnitPriceAmount { get; set; }

    public decimal LineTotalAmount { get; set; }
}
