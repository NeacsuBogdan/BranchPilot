namespace BranchPilot.Application.Common;

public sealed class AppException : Exception
{
    public AppException(int statusCode, string title, string detail)
        : base(detail)
    {
        StatusCode = statusCode;
        Title = title;
    }

    public int StatusCode { get; }

    public string Title { get; }
}
