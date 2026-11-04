
namespace Application.Domain.Model.ValueObjects;

public sealed record IssueRequestId(Guid Value)
{
    public static IssueRequestId New() =>
        new(Guid.NewGuid());
}

public sealed record ToolId(Guid Value);

public sealed record ExecutorId(Guid Value);
