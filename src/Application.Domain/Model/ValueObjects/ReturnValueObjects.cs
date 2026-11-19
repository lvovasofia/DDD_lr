namespace Application.Domain.Model.ValueObjects;


public sealed record IssueHandover
{
    public StorekeeperId IssuedBy { get; }
    public DateTimeOffset IssuedAt { get; }

    public IssueHandover(StorekeeperId issuedBy, DateTimeOffset issuedAt)
    {
        if (issuedBy is null)
            throw new ArgumentException(
                "Не зазначено комірника, який видав інструменти.",
                nameof(issuedBy));

        IssuedBy = issuedBy;
        IssuedAt = issuedAt;
    }
}


public sealed record ToolReturn
{
    public ToolId Tool { get; }
    public DateTimeOffset ReturnedAt { get; }

    public ToolReturn(ToolId tool, DateTimeOffset returnedAt)
    {
        if (tool is null)
            throw new ArgumentException(
                "Не зазначено інструмент, який повертають.",
                nameof(tool));

        Tool = tool;
        ReturnedAt = returnedAt;
    }
}


public sealed record ReturnInspection
{
    public ToolId Tool { get; }
    public bool IsOperational { get; }

    public ReturnInspection(ToolId tool, bool isOperational)
    {
        if (tool is null)
            throw new ArgumentException(
                "Не зазначено інструмент, який оглянули.",
                nameof(tool));

        Tool = tool;
        IsOperational = isOperational;
    }
}