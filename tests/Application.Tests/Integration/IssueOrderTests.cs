using System;
using Warehouse;
using Xunit;

namespace Application.Tests.Integration;

public sealed class IssueOrderTests
{
    [Fact]
    public void ConfirmIssue_CreatesMessage()
    {
        var order = new IssueOrder(Guid.NewGuid(), Guid.NewGuid());

        order.ConfirmIssue(DateTimeOffset.UtcNow);

        Assert.NotNull(order.Pending);
    }

    [Fact]
    public void PendingJson_ReturnsSameMessage()
    {
        var order = new IssueOrder(Guid.NewGuid(), Guid.NewGuid());
        order.ConfirmIssue(DateTimeOffset.UtcNow);

        var json = order.PendingJson();

        Assert.Equal(json, order.PendingJson());
    }

    [Fact]
    public void Acknowledge_ClearsPending()
    {
        var order = new IssueOrder(Guid.NewGuid(), Guid.NewGuid());
        order.ConfirmIssue(DateTimeOffset.UtcNow);

        order.Acknowledge(order.Pending!.MessageId);

        Assert.Null(order.Pending);
    }
[Fact]
public void Acknowledge_WrongId_Throws()
{
    var order = new IssueOrder(Guid.NewGuid(), Guid.NewGuid());
    order.ConfirmIssue(DateTimeOffset.UtcNow);

    Assert.Throws<InvalidOperationException>(
        () => order.Acknowledge(Guid.Empty));

    Assert.NotNull(order.Pending);
}
}