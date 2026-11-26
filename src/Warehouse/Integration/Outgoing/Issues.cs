
using System;
using System.Text.Json;

namespace Warehouse;

public sealed record IssueConfirmedMessage(
    Guid MessageId,
    int Version,
    Guid RequestId,
    Guid StorekeeperId,
    long Revision,
    DateTimeOffset IssuedAtUtc,
    string StateCode);


public sealed record IssueConfirmed(
    Guid EventId,
    Guid RequestId,
    Guid StorekeeperId,
    DateTimeOffset IssuedAtUtc);

public sealed class IssueOrder
{
    private readonly Guid _requestId;
    private readonly Guid _storekeeperId;

    public IssueOrder(Guid requestId, Guid storekeeperId)
    {
        _requestId = requestId;
        _storekeeperId = storekeeperId;
    }

    public string Status { get; private set; } = "ReadyForIssue";

    public IssueConfirmedMessage? Pending { get; private set; }

    public IssueConfirmed? Confirmed { get; private set; }

    public void ConfirmIssue(DateTimeOffset now)
    {
        if (Status != "ReadyForIssue" ||
            Pending is not null ||
            _requestId == Guid.Empty ||
            _storekeeperId == Guid.Empty ||
            now == default)
        {
            throw new InvalidOperationException("invalid_issue");
        }

        Confirmed = new IssueConfirmed(
            Guid.NewGuid(),
            _requestId,
            _storekeeperId,
            now.ToUniversalTime());

        Pending = new IssueConfirmedMessage(
            Confirmed.EventId,
            1,
            Confirmed.RequestId,
            Confirmed.StorekeeperId,
            1,
            Confirmed.IssuedAtUtc,
            "ISSUED");

        Status = "Issued";
    }

    public string PendingJson()
    {
        return JsonSerializer.Serialize(
            Pending ?? throw new InvalidOperationException(
                "no_pending_message"));
    }

    public void Acknowledge(Guid id)
    {
        if (Pending?.MessageId != id)
        {
            throw new InvalidOperationException("wrong_ack");
        }

        Pending = null;
    }
}
