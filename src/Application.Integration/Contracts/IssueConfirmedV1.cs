namespace Application.Integration.Contracts;

public sealed record IssueConfirmedV1
{
    public required Guid MessageId { get; init; }
    public required int Version { get; init; }
    public required Guid RequestId { get; init; }
    public required Guid StorekeeperId { get; init; }
    public required long Revision { get; init; }
    public required DateTimeOffset IssuedAtUtc { get; init; }
    public required string StateCode { get; init; }
}

public enum Receipt { Applied, Duplicate }