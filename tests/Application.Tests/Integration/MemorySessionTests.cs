using Application.Domain.Model.Entities;
using Application.Domain.Model.ValueObjects;
using Application.Infrastructure.Persistence;
using Xunit;

namespace Application.Tests.Integration;

public class MemorySessionTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 11, 10, 8, 0, 0, TimeSpan.Zero);

    private static readonly UsagePeriod Period =
        new(Now.AddHours(1), Now.AddDays(2));

    private static readonly StorekeeperId Storekeeper = new(Guid.NewGuid());

    private static IssueRequest Submitted()
    {
        var r = IssueRequest.CreateDraft(Period, new ExecutorId(Guid.NewGuid()));
        r.AddItem(new ToolId(Guid.NewGuid()));
        r.Submit();
        return r;
    }

    private static IssueConfirmations Confirmations(IssueRequest request)
    {
        var executor = request.Executor!;
        var tools = request.Items;
        return new IssueConfirmations(
            tools.Select(t => new AvailabilityConfirmation(t, true)),
            tools.Select(t => new ConditionConfirmation(t, true)),
            tools.Select(t => new PermitConfirmation(
                executor, t, PermitStatus.Granted, Now.AddDays(30))),
            tools.Select(t => new CalibrationConfirmation(t, false)));
    }

    [Fact]
    public async Task Restore_ReturnsNewInstance()
    {
        var original = Submitted();
        var session = new MemorySession(original);

        var loaded = await session.FindAsync(original.Id);

        Assert.NotNull(loaded);
        Assert.NotSame(original, loaded!.Aggregate);
        Assert.Equal(original.Id, loaded.Aggregate.Id);
        Assert.Equal(original.Status, loaded.Aggregate.Status);
        Assert.Equal(original.Items, loaded.Aggregate.Items);
        Assert.Empty(loaded.Aggregate.Events);
    }

    [Fact]
    public async Task Restored_CanBeApproved()
    {
        var original = Submitted();
        var session = new MemorySession(original);
        var loaded = await session.FindAsync(original.Id);
        var request = loaded!.Aggregate;

        request.ApproveForIssue(Storekeeper, Confirmations(request), Now);
        session.Stage(request, loaded.Revision);
        await session.SaveChangesAsync();

        var reloaded = await session.FindAsync(original.Id);
        Assert.Equal(RequestStatus.ApprovedForIssue, reloaded!.Aggregate.Status);
        Assert.Equal(2, reloaded.Revision);
        Assert.Empty(reloaded.Aggregate.Events);
    }
    [Fact]
    public async Task Conflict_IsRejected()
    {
        var original = Submitted();
        var session = new MemorySession(original);
        var first = await session.FindAsync(original.Id);
        var second = await session.FindAsync(original.Id);

        first!.Aggregate.ApproveForIssue(
            Storekeeper, Confirmations(first.Aggregate), Now);
        session.Stage(first.Aggregate, first.Revision);
        await session.SaveChangesAsync();

        second!.Aggregate.ApproveForIssue(
            Storekeeper, Confirmations(second.Aggregate), Now);
        session.Stage(second.Aggregate, second.Revision);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => session.SaveChangesAsync());

        var current = await session.FindAsync(original.Id);
        Assert.Equal(2, current!.Revision);
    }

    [Fact]
    public async Task Saved_ApprovalIsPreserved()
    {
        // Arrange
        var original = Submitted();
        var session = new MemorySession(original);
        var loaded = await session.FindAsync(original.Id);
        var request = loaded!.Aggregate;

        // Act
        request.ApproveForIssue(Storekeeper, Confirmations(request), Now);
        session.Stage(request, loaded.Revision);
        await session.SaveChangesAsync();
        var reloaded = await session.FindAsync(original.Id);

        // Assert
        var approval = reloaded!.Aggregate.Approval;
        Assert.NotNull(approval);
        Assert.Equal(Storekeeper, approval!.ApprovedBy);
        Assert.Equal(Now, approval.ApprovedAt);
    }
}