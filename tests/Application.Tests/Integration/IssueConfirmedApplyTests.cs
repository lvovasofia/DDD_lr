using System.Text.Json;
using Application.App.Ports;
using Application.App.Services;
using Application.Domain.Model.Entities;
using Application.Domain.Model.ValueObjects;
using Application.Infrastructure.Persistence;
using Xunit;

namespace Application.Tests.Integration;

public class IssueConfirmedApplyTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 11, 10, 8, 0, 0, TimeSpan.Zero);

    private static readonly UsagePeriod Period =
        new(Now.AddHours(1), Now.AddDays(2));

    private static readonly StorekeeperId Storekeeper = new(Guid.NewGuid());

    private sealed record IssueConfirmedV1(
        Guid MessageId, Guid RequestId, Guid StorekeeperId, DateTimeOffset IssuedAtUtc);

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

    private static IssueConfirmedV1 Parse(string json, DateTimeOffset now) =>
        JsonSerializer.Deserialize<IssueConfirmedV1>(json)!;

    private static IssueRequestService Service(MemorySession session) =>
        new(session, session, new FixedActor(), new FixedClock(Now), new NoFacts());

    private static Task Apply(IssueRequestService service, IssueConfirmedV1 message) =>
        service.ApplyIssueConfirmedAsync<IssueConfirmedV1>(
            JsonSerializer.Serialize(message),
            Parse,
            m => new IssueRequestId(m.RequestId),
            m => new IssueIssuedFact(new StorekeeperId(m.StorekeeperId), m.IssuedAtUtc));

    [Fact]
    public async Task ApprovedRequest_BecomesIssued_AndSavesOnce()
    {
        var request = Approved();
        var session = new MemorySession(request);
        var message = new IssueConfirmedV1(
            Guid.NewGuid(), request.Id.Value, Storekeeper.Value, Now.AddHours(1));

        await Apply(Service(session), message);

        var saved = await session.FindAsync(request.Id);
        Assert.Equal(RequestStatus.Issued, saved!.Aggregate.Status);
        Assert.Equal(2, saved.Revision);
    }

    [Fact]
    public async Task NotApprovedRequest_IsRefused_AndNothingChanges()
    {
        var request = Submitted();
        var session = new MemorySession(request);
        var message = new IssueConfirmedV1(
            Guid.NewGuid(), request.Id.Value, Storekeeper.Value, Now.AddHours(1));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => Apply(Service(session), message));

        var saved = await session.FindAsync(request.Id);
        Assert.Equal(RequestStatus.Submitted, saved!.Aggregate.Status);
        Assert.Equal(1, saved.Revision);
    }
}