
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
}
