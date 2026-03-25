using BranchPilot.Domain.Common;

namespace BranchPilot.Domain.Entities;

public sealed class TaxProfile : ITenantEntity
{
    public Guid Id { get; set; }

    public Guid TenantId { get; set; }

    public string Name { get; set; } = string.Empty;

    public decimal Rate { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }
}
