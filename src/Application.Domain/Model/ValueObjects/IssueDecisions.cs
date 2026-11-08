namespace Application.Domain.Model.ValueObjects;

public sealed record StorekeeperId
{
    public Guid Value { get; }

    public StorekeeperId(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException(
                "Ідентифікатор комірника не може бути порожнім.");

        Value = value;
    }
}

public sealed record IssueApproval
{
    public StorekeeperId ApprovedBy { get; }
    public DateTimeOffset ApprovedAt { get; }

    internal IssueApproval(StorekeeperId approvedBy, DateTimeOffset approvedAt)
    {
        if (approvedBy is null)
            throw new ArgumentException(
                "Не зазначено комірника, який дозволив видачу.");

        if (approvedAt == default)
            throw new ArgumentException(
                "Не зазначено час дозволу видачі.");

        ApprovedBy = approvedBy;
        ApprovedAt = approvedAt;
    }
}

public sealed record IssueRejection
{
    public StorekeeperId RejectedBy { get; }
    public DateTimeOffset RejectedAt { get; }
    public string Reason { get; }

    internal IssueRejection(
        StorekeeperId rejectedBy,
        DateTimeOffset rejectedAt,
        string reason)
    {
        if (rejectedBy is null)
            throw new ArgumentException(
                "Не зазначено комірника, який відхилив заявку.");

        if (rejectedAt == default)
            throw new ArgumentException(
                "Не зазначено час відхилення заявки.");

        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException(
                "Причина відхилення не може бути порожньою.");

        RejectedBy = rejectedBy;
        RejectedAt = rejectedAt;
        Reason = reason;
    }
}