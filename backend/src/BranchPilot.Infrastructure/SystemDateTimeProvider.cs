using BranchPilot.Application.Abstractions.Time;

namespace BranchPilot.Infrastructure;

public sealed class SystemDateTimeProvider : IDateTimeProvider
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
