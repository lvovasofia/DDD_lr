using System.Text.Json;
using System.Text.Json.Nodes;
using Application.Integration.Contracts;
using Application.Integration.Translation;
using Application.Integration.Validation;
using Xunit;

namespace Application.Tests.Integration;

public static class IssueMessages
{
    public static readonly DateTimeOffset Now =
        new(2026, 11, 10, 8, 0, 0, TimeSpan.Zero);

    public static IssueConfirmedV1 Valid() => new()
    {
        MessageId = Guid.NewGuid(),
        Version = 1,
        RequestId = Guid.NewGuid(),
        StorekeeperId = Guid.NewGuid(),
        Revision = 1,
        IssuedAtUtc = Now.AddMinutes(-30),
        StateCode = "ISSUED"
    };
}

public sealed class ContractTests
{
    [Fact]
    public void ValidMessage_IsTranslatedToLocalFact()
    {
        // Arrange
        var message = IssueMessages.Valid();
        var json = JsonSerializer.Serialize(message);

        // Act
        var parsed = IssueConfirmedContract.Parse(json, IssueMessages.Now);
        var fact = IssueConfirmedTranslator.ToLocal(parsed);
        var requestId = IssueConfirmedTranslator.RequestIdOf(parsed);

        // Assert
        Assert.Equal(message.RequestId, requestId.Value);
        Assert.Equal(message.StorekeeperId, fact.Storekeeper.Value);
        Assert.Equal(message.IssuedAtUtc, fact.IssuedAt);
    }

    [Fact]
    public void MissingEachRequiredField_RejectsWithoutCacheMutation()
    {
        // Arrange
        var json = JsonSerializer.Serialize(IssueMessages.Valid());
        var original = JsonNode.Parse(json)!.AsObject();
        var cache = new IssueConfirmedCache();

        foreach (var name in original.Select(x => x.Key).ToArray())
        {
            var copy = JsonNode.Parse(json)!.AsObject();
            copy.Remove(name);

            // Act
            Action act = () => cache.Apply(copy.ToJsonString(), IssueMessages.Now);

            // Assert
            Assert.Throws<JsonException>(act);
            Assert.Equal(0, cache.Count);
        }
    }

    [Fact]
    public void SameMessageId_ChangedContent_IsConflict()
    {
        // Arrange
        var message = IssueMessages.Valid();
        var cache = new IssueConfirmedCache();
        cache.Apply(JsonSerializer.Serialize(message), IssueMessages.Now);
        var changed = message with { StorekeeperId = Guid.NewGuid() };

        // Act
        Action act = () => cache.Apply(JsonSerializer.Serialize(changed), IssueMessages.Now);

        // Assert
        Assert.Throws<InvalidDataException>(act);
        Assert.Equal(1, cache.Count);
    }

    [Fact]
    public void SameMoment_DifferentOffset_HasSameCanonical()
    {
        // Arrange
        var utc = IssueMessages.Valid();
        var kyiv = utc with { IssuedAtUtc = utc.IssuedAtUtc.ToOffset(TimeSpan.FromHours(2)) };

        // Act
        var a = IssueConfirmedContract.Parse(JsonSerializer.Serialize(utc), IssueMessages.Now);
        var b = IssueConfirmedContract.Parse(JsonSerializer.Serialize(kyiv), IssueMessages.Now);

        // Assert
        Assert.Equal(IssueConfirmedContract.Canonical(a), IssueConfirmedContract.Canonical(b));
    }
}