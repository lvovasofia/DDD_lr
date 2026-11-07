namespace Application.Domain.Model.ValueObjects;

public sealed record IssueRequestId
{
    public Guid Value { get; }

    public IssueRequestId(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException(
                "Ідентифікатор заявки не може бути порожнім.");

        Value = value;
    }

    public static IssueRequestId New() =>
        new(Guid.NewGuid());
}

public sealed record ToolId
{
    public Guid Value { get; }

    public ToolId(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException(
                "Ідентифікатор інструмента не може бути порожнім.");

        Value = value;
    }
}

public sealed record ExecutorId
{
    public Guid Value { get; }

    public ExecutorId(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException(
                "Ідентифікатор виконавця не може бути порожнім.");

        Value = value;
    }
}