namespace BranchPilot.Application.Locations;

public sealed record CreateLocationRequest(string Name, string Code, string TimeZone);

public sealed record LocationResponse(Guid Id, string Name, string Code, string TimeZone);
