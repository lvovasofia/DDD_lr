using Application.Domain.Model.Entities;
using Application.Domain.Model.ValueObjects;
using Xunit;

namespace Application.Tests.Application.Domain;

public class RestoreTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 11, 10, 8, 0, 0, TimeSpan.Zero);

    private static readonly UsagePeriod Period =
        new(Now.AddHours(1), Now.AddDays(2));

    private static readonly StorekeeperId Storekeeper = new(Guid.NewGuid());

    private static IssueRequest Submitted()
    {
        var request = IssueRequest.CreateDraft(Period, new ExecutorId(Guid.NewGuid()));
        request.AddItem(new ToolId(Guid.NewGuid()));
        request.Submit();
        return request;
    }

    private static IssueConfirmations Confirmations(IssueRequest request, bool serviceable = true)
    {
        var executor = request.Executor!;
        var tools = request.Items;
        return new IssueConfirmations(
            tools.Select(t => new AvailabilityConfirmation(t, true)),
            tools.Select(t => new ConditionConfirmation(t, serviceable)),
            tools.Select(t => new PermitConfirmation(executor, t, PermitStatus.Granted, Now.AddDays(30))),
            tools.Select(t => new CalibrationConfirmation(t, false)));
    }

    [Fact]
    public void Restore_ApprovedWithoutApproval_Rejects()
    {
        // Arrange
        var request = Submitted();
        request.ApproveForIssue(Storekeeper, Confirmations(request), Now);
        var broken = request.ToState() with { Approval = null };

        // Act
        var act = () => IssueRequest.Restore(broken);

        // Assert
        Assert.Throws<InvalidOperationException>(act);
    }

    [Fact]
    public void Restore_RejectedWithoutRejection_Rejects()
    {
        // Arrange
        var request = Submitted();
        request.RejectIssue(Storekeeper, Confirmations(request, serviceable: false), Now);
        var broken = request.ToState() with { Rejection = null };

        // Act
        var act = () => IssueRequest.Restore(broken);

        // Assert
        Assert.Throws<InvalidOperationException>(act);
    }

    [Fact]
    public void Restore_SubmittedWithoutItems_Rejects()
    {
        // Arrange
        var broken = Submitted().ToState() with { Items = Array.Empty<ToolId>() };

        // Act
        var act = () => IssueRequest.Restore(broken);

        // Assert
        Assert.Throws<InvalidOperationException>(act);
    }

    [Fact]
    public void Restore_DuplicateItems_Rejects()
    {
        // Arrange
        var state = Submitted().ToState();
        var tool = state.Items[0];
        var broken = state with { Items = new[] { tool, tool } };

        // Act
        var act = () => IssueRequest.Restore(broken);

        // Assert
        Assert.Throws<InvalidOperationException>(act);
    }
}