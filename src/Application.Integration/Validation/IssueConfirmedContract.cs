using System.Text.Json;
using System.Text.Json.Serialization;
using Application.Integration.Contracts;

namespace Application.Integration.Validation;

public static class IssueConfirmedContract
{
    private static readonly string[] Fields =
    [
        "MessageId", "Version", "RequestId", "StorekeeperId",
        "Revision", "IssuedAtUtc", "StateCode"
    ];

    private static readonly JsonSerializerOptions Options = new()
    {
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow
    };

    public static IssueConfirmedV1 Parse(string json, DateTimeOffset now)
    {
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        if (root.ValueKind != JsonValueKind.Object)
            throw new JsonException("object_required");

        var names = new List<string>();
        foreach (var property in root.EnumerateObject())
        {
            if (names.Contains(property.Name))
                throw new JsonException("duplicate_field");
            names.Add(property.Name);
        }

        foreach (var field in Fields)
        {
            if (!root.TryGetProperty(field, out var value) ||
                value.ValueKind == JsonValueKind.Null)
                throw new JsonException("required_field");
        }

        if (names.Count != Fields.Length)
            throw new JsonException("unknown_field");

        var m = JsonSerializer.Deserialize<IssueConfirmedV1>(json, Options)
                ?? throw new JsonException("empty_message");

        if (m.Version != 1)
            throw new JsonException("unknown_version");

        if (m.MessageId == Guid.Empty || m.RequestId == Guid.Empty ||
            m.StorekeeperId == Guid.Empty)
            throw new JsonException("empty_id");

        if (m.Revision <= 0)
            throw new JsonException("invalid_revision");

        if (m.StateCode != "ISSUED")
            throw new JsonException("unknown_state");

        if (m.IssuedAtUtc == default || m.IssuedAtUtc > now)
            throw new JsonException("invalid_time");

        return m with { IssuedAtUtc = m.IssuedAtUtc.ToUniversalTime() };
    }

    public static string Canonical(IssueConfirmedV1 m) =>
        JsonSerializer.Serialize(m);
}