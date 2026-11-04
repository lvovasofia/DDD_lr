
using Application.Domain.Model.Entities;
using Application.Domain.Model.ValueObjects;
using Xunit;

namespace Application.Tests.Application.Domain;

public class IssueRequestTests
{
    private static UsagePeriod ValidPeriod()
    {
        var start = new DateTimeOffset(
            2026, 11, 10, 9, 0, 0, TimeSpan.Zero);

        return new UsagePeriod(
            start, start.AddDays(2));
    }

    private static IssueRequest ValidDraft()
    {
        var request = IssueRequest.CreateDraft(
            ValidPeriod(),
            new ExecutorId(Guid.NewGuid()));

        request.AddItem(new ToolId(Guid.NewGuid()));

        return request;
    }

    [Fact]
    public void CreateDraft_Succeeds()
    {
        var request = IssueRequest.CreateDraft(
            ValidPeriod());

        Assert.Equal(RequestStatus.Draft, request.Status);
        Assert.Empty(request.Items);
        Assert.Contains("RequestDraftCreated", request.Events);
    }

    [Fact]
    public void AddItem_Succeeds()
    {
        var request = IssueRequest.CreateDraft(
            ValidPeriod());

        var tool = new ToolId(Guid.NewGuid());

        request.AddItem(tool);

        Assert.Contains(tool, request.Items);
        Assert.Contains("RequestItemAdded", request.Events);
    }

    [Fact]
    public void RemoveItem_Succeeds()
    {
        var request = ValidDraft();
        var tool = request.Items[0];

        request.RemoveItem(tool);

        Assert.DoesNotContain(tool, request.Items);
        Assert.Contains("RequestItemRemoved", request.Events);
    }

    [Fact]
    public void ChangePeriod_Succeeds()
    {
        var request = ValidDraft();

        var start = new DateTimeOffset(
            2026, 12, 1, 9, 0, 0, TimeSpan.Zero);

        var newPeriod = new UsagePeriod(
            start, start.AddDays(3));

        request.ChangePeriod(newPeriod);

        Assert.Equal(newPeriod, request.Period);
        Assert.Contains("RequestPeriodChanged", request.Events);
    }

    [Fact]
    public void Submit_ValidRequest_Succeeds()
    {
        var request = ValidDraft();

        request.Submit();

        Assert.Equal(
            RequestStatus.Submitted, request.Status);

        Assert.Contains("RequestSubmitted", request.Events);
    }

    [Fact]
    public void Submit_EmptyRequest_Rejects()
    {
        var request = IssueRequest.CreateDraft(
            ValidPeriod(),
            new ExecutorId(Guid.NewGuid()));

        var statusBefore = request.Status;
        var itemsBefore = request.Items.ToArray();
        var eventsBefore = request.Events.ToArray();

        Assert.Throws<InvalidOperationException>(
            () => request.Submit());

        Assert.Equal(statusBefore, request.Status);
        Assert.Equal(itemsBefore, request.Items);
        Assert.Equal(eventsBefore, request.Events);
    }

    [Fact]
    public void Submit_WithoutExecutor_Rejects()
    {
        var request = IssueRequest.CreateDraft(
            ValidPeriod());

        request.AddItem(new ToolId(Guid.NewGuid()));

        var statusBefore = request.Status;
        var eventsBefore = request.Events.ToArray();

        Assert.Throws<InvalidOperationException>(
            () => request.Submit());

        Assert.Equal(statusBefore, request.Status);
        Assert.Equal(eventsBefore, request.Events);
    }

    [Fact]
    public void AddItem_DuplicateTool_Rejects()
    {
        var request = ValidDraft();
        var tool = request.Items[0];

        var itemsBefore = request.Items.ToArray();
        var eventsBefore = request.Events.ToArray();

        Assert.Throws<InvalidOperationException>(
            () => request.AddItem(tool));

        Assert.Equal(itemsBefore, request.Items);
        Assert.Equal(eventsBefore, request.Events);
    }

    [Fact]
    public void AddItem_AfterSubmit_Rejects()
    {
        var request = ValidDraft();
        request.Submit();

        var statusBefore = request.Status;
        var itemsBefore = request.Items.ToArray();
        var eventsBefore = request.Events.ToArray();

        Assert.Throws<InvalidOperationException>(
            () => request.AddItem(
                new ToolId(Guid.NewGuid())));

        Assert.Equal(statusBefore, request.Status);
        Assert.Equal(itemsBefore, request.Items);
        Assert.Equal(eventsBefore, request.Events);
    }

    
[Fact]
public void ChangePeriod_AfterSubmit_Rejects()
{
    var request = ValidDraft();
    request.Submit();

    var periodBefore = request.Period;
    var eventsBefore = request.Events.ToArray();

    var start = new DateTimeOffset(
        2026, 12, 1, 9, 0, 0, TimeSpan.Zero);

    var newPeriod = new UsagePeriod(
        start, start.AddDays(5));

    Assert.Throws<InvalidOperationException>(
        () => request.ChangePeriod(newPeriod));

    Assert.Equal(periodBefore, request.Period);
    Assert.Equal(eventsBefore, request.Events);
}


[Fact]
public void RemoveItem_AfterSubmit_Rejects()
{
    var request = ValidDraft();
    request.Submit();

    var tool = request.Items[0];

    var itemsBefore = request.Items.ToArray();
    var eventsBefore = request.Events.ToArray();

    Assert.Throws<InvalidOperationException>(
        () => request.RemoveItem(tool));

    Assert.Equal(RequestStatus.Submitted, request.Status);
    Assert.Equal(itemsBefore, request.Items);
    Assert.Equal(eventsBefore, request.Events);
}
[Fact]
public void SetExecutor_InDraft_Succeeds()
{
    var request = IssueRequest.CreateDraft(ValidPeriod());
    var executor = new ExecutorId(Guid.NewGuid());

    request.SetExecutor(executor);

    Assert.Equal(executor, request.Executor);
    Assert.Contains("RequestExecutorAssigned", request.Events);
}

[Fact]

public void SetExecutor_AfterSubmit_Rejects()
{
    var request = ValidDraft();
    request.Submit();

    var executorBefore = request.Executor;
    var eventsBefore = request.Events.ToArray();

    Assert.Throws<InvalidOperationException>(
        () => request.SetExecutor(
            new ExecutorId(Guid.NewGuid())));

    Assert.Equal(executorBefore, request.Executor);
    Assert.Equal(RequestStatus.Submitted, request.Status);
    Assert.Equal(eventsBefore, request.Events);
}

[Fact]
public void TwoRequests_HaveDifferentIdentities()
{
    var first = IssueRequest.CreateDraft(ValidPeriod());
    var second = IssueRequest.CreateDraft(ValidPeriod());

    Assert.NotEqual(first.Id, second.Id);
}

}
