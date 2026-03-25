using BranchPilot.Domain.Common;

namespace BranchPilot.Domain.Entities;

public sealed class Category : ITenantEntity
{
    public Guid Id { get; set; }

    public Guid TenantId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public DateTimeOffset CreatedAtUtc { get; set; }
}
