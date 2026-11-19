using Application.Domain.Model.Entities;
using Application.Domain.Model.ValueObjects;
using Xunit;

namespace Application.Tests.Application.Domain;

public class ApprovalTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 11, 10, 8, 0, 0, TimeSpan.Zero);

    private static readonly UsagePeriod Period =
        new(Now.AddHours(1), Now.AddDays(2));

    private static readonly StorekeeperId Storekeeper =
        new(Guid.NewGuid());

    private static IssueRequest SubmittedRequest(int toolCount = 1)
    {
        var request = IssueRequest.CreateDraft(
            Period, new ExecutorId(Guid.NewGuid()));

        for (var i = 0; i < toolCount; i++)
            request.AddItem(new ToolId(Guid.NewGuid()));

        request.Submit();
        return request;
    }

    private static IssueConfirmations Confirmations(
        IssueRequest request,
        ToolId? brokenTool = null,
        ToolId? toolWithoutPermit = null,
        DateTimeOffset? permitValidUntil = null,
        ToolId? measuringTool = null,
        DateTimeOffset? calibrationValidUntil = null)
    {
        var executor = request.Executor!;
        var tools = request.Items;

        return new IssueConfirmations(
            tools.Select(t => new AvailabilityConfirmation(t, true)),
            tools.Select(t => new ConditionConfirmation(t, t != brokenTool)),
            tools.Select(t => t == toolWithoutPermit
                ? new PermitConfirmation(executor, t, PermitStatus.NotGranted)
                : new PermitConfirmation(executor, t, PermitStatus.Granted,
                    permitValidUntil ?? Now.AddDays(30))),
            tools.Select(t => t == measuringTool
                ? new CalibrationConfirmation(t, true, calibrationValidUntil)
                : new CalibrationConfirmation(t, false)));
    }

    private sealed record Snapshot(
        RequestStatus Status,
        ToolId[] Items,
        string[] Events,
        IssueApproval? Approval,
        IssueRejection? Rejection);

    private static Snapshot Take(IssueRequest r) =>
        new(r.Status, r.Items.ToArray(), r.Events.ToArray(),
            r.Approval, r.Rejection);

    private static void AssertUnchanged(Snapshot before, IssueRequest r)
    {
        Assert.Equal(before.Status, r.Status);
        Assert.Equal(before.Items, r.Items);
        Assert.Equal(before.Events, r.Events);
        Assert.Equal(before.Approval, r.Approval);
        Assert.Equal(before.Rejection, r.Rejection);
    }


    [Fact]
    public void ApproveForIssue_AllConditionsMet_Succeeds()
    {
        // Arrange
        var request = SubmittedRequest(toolCount: 2);
        var confirmations = Confirmations(request);

        // Act
        request.ApproveForIssue(Storekeeper, confirmations, Now);

        // Assert
        Assert.Equal(RequestStatus.ApprovedForIssue, request.Status);
        Assert.Contains("RequestApprovedForIssue", request.Events);
        Assert.NotNull(request.Approval);
        Assert.Equal(Storekeeper, request.Approval!.ApprovedBy);
        Assert.Equal(Now, request.Approval.ApprovedAt);
    }

    [Fact]
    public void ApproveForIssue_WhenPermitNotGranted_Rejects()
    {
        // Arrange
        var request = SubmittedRequest();
        var confirmations = Confirmations(
            request, toolWithoutPermit: request.Items[0]);
        var before = Take(request);

        // Act
        var act = () => request.ApproveForIssue(Storekeeper, confirmations, Now);

        // Assert
        Assert.Throws<InvalidOperationException>(act);
        AssertUnchanged(before, request);
    }

    [Fact]
    public void ApproveForIssue_WhenPermitExpired_Rejects()
    {
        // Arrange
        var request = SubmittedRequest();
        var confirmations = Confirmations(
            request, permitValidUntil: Now.AddDays(-1));
        var before = Take(request);

        // Act
        var act = () => request.ApproveForIssue(Storekeeper, confirmations, Now);

        // Assert
        Assert.Throws<InvalidOperationException>(act);
        AssertUnchanged(before, request);
    }

    [Fact]
    public void ApproveForIssue_CalibrationEndsOnLastDay_Succeeds()
    {
        // Arrange
        var request = SubmittedRequest();
        var confirmations = Confirmations(
            request,
            measuringTool: request.Items[0],
            calibrationValidUntil: Period.End);

        // Act
        request.ApproveForIssue(Storekeeper, confirmations, Now);

        // Assert
        Assert.Equal(RequestStatus.ApprovedForIssue, request.Status);
    }

    [Fact]
    public void ApproveForIssue_CalibrationEndsBeforeLastDay_Rejects()
    {
        // Arrange
        var request = SubmittedRequest();
        var confirmations = Confirmations(
            request,
            measuringTool: request.Items[0],
            calibrationValidUntil: Period.End.AddDays(-1));
        var before = Take(request);

        // Act
        var act = () => request.ApproveForIssue(Storekeeper, confirmations, Now);

        // Assert
        Assert.Throws<InvalidOperationException>(act);
        AssertUnchanged(before, request);
    }

    [Fact]
    public void ApproveForIssue_OneToolBroken_RequestUnchanged()
    {
        // Arrange
        var request = SubmittedRequest(toolCount: 3);
        var confirmations = Confirmations(
            request, brokenTool: request.Items[2]);
        var before = Take(request);

        // Act
        var act = () => request.ApproveForIssue(Storekeeper, confirmations, Now);

        // Assert
        Assert.Throws<InvalidOperationException>(act);
        AssertUnchanged(before, request);
    }

    [Fact]
    public void ApproveForIssue_WhenConfirmationMissing_Rejects()
    {
        // Arrange
        var request = SubmittedRequest();
        var empty = new IssueConfirmations([], [], [], []);
        var before = Take(request);

        // Act
        var act = () => request.ApproveForIssue(Storekeeper, empty, Now);

        // Assert
        Assert.Throws<InvalidOperationException>(act);
        AssertUnchanged(before, request);
    }

    [Fact]
    public void ApproveForIssue_WhenDraft_Rejects()
    {
        // Arrange
        var request = IssueRequest.CreateDraft(
            Period, new ExecutorId(Guid.NewGuid()));
        request.AddItem(new ToolId(Guid.NewGuid()));
        var confirmations = Confirmations(request);
        var before = Take(request);

        // Act
        var act = () => request.ApproveForIssue(Storekeeper, confirmations, Now);

        // Assert
        Assert.Throws<InvalidOperationException>(act);
        AssertUnchanged(before, request);
    }

    [Fact]
    public void ApproveForIssue_WhenAlreadyApproved_Rejects()
    {
        // Arrange
        var request = SubmittedRequest();
        var confirmations = Confirmations(request);
        request.ApproveForIssue(Storekeeper, confirmations, Now);
        var before = Take(request);

        // Act
        var act = () => request.ApproveForIssue(Storekeeper, confirmations, Now);

        // Assert
        Assert.Throws<InvalidOperationException>(act);
        AssertUnchanged(before, request);
    }

    [Fact]
    public void RejectIssue_AfterFailedCheck_Succeeds()
    {
        // Arrange
        var request = SubmittedRequest();
        var confirmations = Confirmations(
            request, brokenTool: request.Items[0]);

        // Act
        request.RejectIssue(Storekeeper, confirmations, Now);

        // Assert
        Assert.Equal(RequestStatus.Rejected, request.Status);
        Assert.Contains("RequestRejected", request.Events);
        Assert.NotNull(request.Rejection);
        Assert.Contains("несправний", request.Rejection!.Reason);
    }

    [Fact]
    public void RejectIssue_WhenAllConditionsMet_Rejects()
    {
        // Arrange
        var request = SubmittedRequest();
        var confirmations = Confirmations(request);
        var before = Take(request);

        // Act
        var act = () => request.RejectIssue(Storekeeper, confirmations, Now);

        // Assert
        Assert.Throws<InvalidOperationException>(act);
        AssertUnchanged(before, request);
    }
}