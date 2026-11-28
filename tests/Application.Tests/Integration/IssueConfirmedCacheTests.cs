using System.Text.Json;
using System.Text.Json.Nodes;
using Application.Integration.Contracts;
using Application.Integration.Validation;
using Xunit;

namespace Application.Tests.Integration;

public sealed class IssueConfirmedCacheTests
{
    [Fact]
    public void RepeatedMessage_IsDuplicateWithoutNewEntry()
    {
        // Arrange
        var json = JsonSerializer.Serialize(IssueMessages.Valid());
        var cache = new IssueConfirmedCache();

        // Act
        var first = cache.Apply(json, IssueMessages.Now);
        var second = cache.Apply(json, IssueMessages.Now);

        // Assert
        Assert.Equal(Receipt.Applied, first);
        Assert.Equal(Receipt.Duplicate, second);
        Assert.Equal(1, cache.Count);
    }

    [Theory]
    [InlineData("version")]
    [InlineData("state")]
    [InlineData("null")]
    public void InvalidMessage_RejectsWithoutCacheMutation(string kind)
    {
        // Arrange
        var json = JsonSerializer.Serialize(IssueMessages.Valid());
        var data = JsonNode.Parse(json)!.AsObject();

        if (kind == "version") data["Version"] = 2;
        if (kind == "state") data["StateCode"] = "RETURNED";
        if (kind == "null") data["StorekeeperId"] = null;

        var cache = new IssueConfirmedCache();

        // Act
        Action act = () => cache.Apply(data.ToJsonString(), IssueMessages.Now);

        // Assert
        Assert.Throws<JsonException>(act);
        Assert.Equal(0, cache.Count);
    }
}