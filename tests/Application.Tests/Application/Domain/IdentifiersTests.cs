
using Application.Domain.Model.ValueObjects;
using Xunit;

namespace Application.Tests.Application.Domain;

public class IdentifiersTests
{
    [Fact]
    public void NewIssueRequestIds_AreDifferent()
    {
        var first = IssueRequestId.New();
        var second = IssueRequestId.New();

        Assert.NotEqual(first, second);
    }

    [Fact]
    public void ToolIds_WithSameValue_AreEqual()
    {
        var value = Guid.NewGuid();

        var first = new ToolId(value);
        var second = new ToolId(value);

        Assert.Equal(first, second);
    }

    [Fact]
    public void ExecutorIds_WithSameValue_AreEqual()
    {
        var value = Guid.NewGuid();

        var first = new ExecutorId(value);
        var second = new ExecutorId(value);

        Assert.Equal(first, second);
    }
    [Fact]
public void IssueRequestId_EmptyGuid_Rejects()
{
    Assert.Throws<ArgumentException>(() =>
        new IssueRequestId(Guid.Empty));
}

[Fact]
public void ToolId_EmptyGuid_Rejects()
{
    Assert.Throws<ArgumentException>(() =>
        new ToolId(Guid.Empty));
}

[Fact]
public void ExecutorId_EmptyGuid_Rejects()
{
    Assert.Throws<ArgumentException>(() =>
        new ExecutorId(Guid.Empty));
}
}
