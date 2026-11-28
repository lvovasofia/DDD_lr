using Application.App.Ports;
using Application.App.Services;
using Application.Domain.Model.Entities;
using Application.Domain.Model.ValueObjects;
using Application.Infrastructure.Persistence;
using Application.Integration.Contracts;
using Application.Integration.Translation;
using Application.Integration.Validation;
using Warehouse;
using Xunit;

namespace Application.Tests.Integration;

public class IssueConfirmedApplyTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 11, 10, 8, 0, 0, TimeSpan.Zero);

    private static readonly DateTimeOffset IssuedAt = Now.AddHours(1);

    private static readonly UsagePeriod Period =
        new(Now.AddHours(1), Now.AddDays(2));

    private static readonly StorekeeperId Storekeeper = new(Guid.NewGuid());

    private sealed class FixedClock(DateTimeOffset now) : IClock
    {
        public DateTimeOffset Now { get; } = now;
    }

    private sealed class FixedActor : IActorContext
    {
        public StorekeeperId Storekeeper { get; } = IssueConfirmedApplyTests.Storekeeper;
    }

    private sealed class NoFacts : IIssueFacts
    {
        public Task<IssueConfirmations> GetConfirmationsAsync(IssueRequest request) =>
            throw new NotSupportedException();
    }

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

    private static IssueRequest Approved()
    {
        var r = Submitted();
        r.ApproveForIssue(Storekeeper, Confirmations(r), Now);
        return r;
    }

    private static IssueOrder Sent(IssueRequest request)
    {
        var order = new IssueOrder(request.Id.Value, Storekeeper.Value);
        order.ConfirmIssue(IssuedAt);
        return order;
    }

    private static IssueRequestService Service(MemorySession session) =>
        new(session, session, new FixedActor(), new FixedClock(Now.AddHours(2)), new NoFacts());

    private static Task Apply(IssueRequestService service, string json) =>
        service.ApplyIssueConfirmedAsync<IssueConfirmedV1>(
            json,
            IssueConfirmedContract.Parse,
            IssueConfirmedTranslator.RequestIdOf,
            IssueConfirmedTranslator.ToLocal);

    [Fact]
    public async Task ApprovedRequest_BecomesIssued_AndSavesOnce()
    {
        var request = Approved();
        var session = new MemorySession(request);

        await Apply(Service(session), Sent(request).PendingJson());

        var saved = await session.FindAsync(request.Id);
        Assert.Equal(RequestStatus.Issued, saved!.Aggregate.Status);
        Assert.Equal(2, saved.Revision);
    }

    [Fact]
    public async Task NotApprovedRequest_IsRefused_AndNothingChanges()
    {
        var request = Submitted();
        var session = new MemorySession(request);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => Apply(Service(session), Sent(request).PendingJson()));

        var saved = await session.FindAsync(request.Id);
        Assert.Equal(RequestStatus.Submitted, saved!.Aggregate.Status);
        Assert.Equal(1, saved.Revision);
    }

    [Fact]
    public async Task SenderMessage_IsAppliedToRequest_ThenAcknowledged()
    {
        var request = Approved();
        var session = new MemorySession(request);
        var order = Sent(request);

        await Apply(Service(session), order.PendingJson());
        order.Acknowledge(order.Pending!.MessageId);

        var saved = await session.FindAsync(request.Id);
        Assert.Equal(RequestStatus.Issued, saved!.Aggregate.Status);
        Assert.Equal(Storekeeper, saved.Aggregate.Handover!.IssuedBy);
        Assert.Equal(IssuedAt, saved.Aggregate.Handover.IssuedAt);
        Assert.Null(order.Pending);
    }
}