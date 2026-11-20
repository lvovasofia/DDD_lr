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
public async Task SaveChangesAsync_TechnicalFailure_DoesNotPersistApproval()
{
    var original = Submitted();

    var session = new MemorySession(original)
    {
        FailNextSave = true
    };

    var loaded = (await session.FindAsync(original.Id))!;

    loaded.Aggregate.ApproveForIssue(
        Storekeeper,
        Confirmations(loaded.Aggregate),
        Now
    );

    session.Stage(loaded.Aggregate, loaded.Revision);

    await Assert.ThrowsAsync<InvalidOperationException>(
        () => session.SaveChangesAsync()
    );

    var persisted = (await session.FindAsync(original.Id))!;

    Assert.Equal(
        RequestStatus.Submitted,
        persisted.Aggregate.Status
    );

    Assert.Null(persisted.Aggregate.Approval);
    Assert.Equal(1, persisted.Revision);
    Assert.Empty(persisted.Aggregate.Events);
}

[Fact]
public async Task FailedSave_ReloadAndRetry_PersistsApprovalOnce()
{
    var original = Submitted();

    var session = new MemorySession(original)
    {
        FailNextSave = true
    };

    var first = (await session.FindAsync(original.Id))!;

    first.Aggregate.ApproveForIssue(
        Storekeeper,
        Confirmations(first.Aggregate),
        Now
    );

    session.Stage(first.Aggregate, first.Revision);

    await Assert.ThrowsAsync<InvalidOperationException>(
        () => session.SaveChangesAsync()
    );

    // Повторно завантажуємо заявку після збою
    var retry = (await session.FindAsync(original.Id))!;

    Assert.NotSame(first.Aggregate, retry.Aggregate);
    Assert.Equal(RequestStatus.Submitted, retry.Aggregate.Status);
    Assert.Equal(1, retry.Revision);

    // Повторно виконуємо погодження
    retry.Aggregate.ApproveForIssue(
        Storekeeper,
        Confirmations(retry.Aggregate),
        Now
    );

    session.Stage(retry.Aggregate, retry.Revision);

    await session.SaveChangesAsync();

    // Перевіряємо збережений стан
    var saved = (await session.FindAsync(original.Id))!;

    Assert.Equal(
        RequestStatus.ApprovedForIssue,
        saved.Aggregate.Status
    );

    Assert.NotNull(saved.Aggregate.Approval);
    Assert.Equal(2, saved.Revision);
    Assert.Empty(saved.Aggregate.Events);
}

[Fact]
public async Task RestoredApprovedRequest_ConfirmIssued_EmitsRequestIssued()
{
    var original = Submitted();
    var session = new MemorySession(original);

    // Крок 1: погодження заявки
    var loaded = (await session.FindAsync(original.Id))!;

    loaded.Aggregate.ApproveForIssue(
        Storekeeper,
        Confirmations(loaded.Aggregate),
        Now
    );

    session.Stage(loaded.Aggregate, loaded.Revision);
    await session.SaveChangesAsync();

    // Крок 2: відновлення погодженої заявки
    var restored = (await session.FindAsync(original.Id))!;

    Assert.Equal(
        RequestStatus.ApprovedForIssue,
        restored.Aggregate.Status
    );

    Assert.Empty(restored.Aggregate.Events);

    // Крок 3: підтвердження видачі
    restored.Aggregate.ConfirmIssued(
        Storekeeper,
        Now.AddMinutes(5)
    );

    Assert.Equal(
        RequestStatus.Issued,
        restored.Aggregate.Status
    );

    Assert.Contains(
        "RequestIssued",
        restored.Aggregate.Events
    );

    // Крок 4: збереження
    session.Stage(restored.Aggregate, restored.Revision);
    await session.SaveChangesAsync();

    // Крок 5: повторне завантаження
    var persisted = (await session.FindAsync(original.Id))!;

    Assert.Equal(
        RequestStatus.Issued,
        persisted.Aggregate.Status
    );

    Assert.NotNull(persisted.Aggregate.Handover);
    Assert.Equal(3, persisted.Revision);
    Assert.Empty(persisted.Aggregate.Events);
}


}