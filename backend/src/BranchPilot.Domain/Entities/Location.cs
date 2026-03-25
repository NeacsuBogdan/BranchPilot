using BranchPilot.Domain.Common;

namespace BranchPilot.Domain.Entities;

public sealed class Location : ITenantEntity
{
    public Guid Id { get; set; }

    public Guid TenantId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Code { get; set; } = string.Empty;

    public string TimeZone { get; set; } = string.Empty;

    public DateTimeOffset CreatedAtUtc { get; set; }
}
