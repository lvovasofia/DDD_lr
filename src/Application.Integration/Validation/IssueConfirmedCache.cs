using Application.Integration.Contracts;

namespace Application.Integration.Validation;

public sealed class IssueConfirmedCache
{
    private readonly Dictionary<Guid, string> _messages = new();
    private readonly Dictionary<(Guid RequestId, long Revision), string> _revisions = new();

    public int Count => _messages.Count;

    public Receipt Apply(string json, DateTimeOffset now)
    {
        var m = IssueConfirmedContract.Parse(json, now);
        var canonical = IssueConfirmedContract.Canonical(m);

        if (_messages.TryGetValue(m.MessageId, out var old))
        {
            if (old == canonical)
                return Receipt.Duplicate;

            throw new InvalidDataException("message_conflict");
        }

        if (_revisions.ContainsKey((m.RequestId, m.Revision)))
            throw new InvalidDataException("revision_conflict");

        long current = 0;
        foreach (var key in _revisions.Keys)
        {
            if (key.RequestId == m.RequestId && key.Revision > current)
                current = key.Revision;
        }

        if (m.Revision != current + 1)
            throw new InvalidDataException("revision_gap_or_stale");

        _messages.Add(m.MessageId, canonical);
        _revisions.Add((m.RequestId, m.Revision), canonical);
        return Receipt.Applied;
    }
}