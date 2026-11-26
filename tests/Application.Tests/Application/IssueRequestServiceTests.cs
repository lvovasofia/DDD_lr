using Application.App.Ports;
using Application.App.Services;
using Application.Domain.Model.Entities;
using Application.Domain.Model.ValueObjects;
using Xunit;

namespace Application.Tests.Application;

public class IssueRequestServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 10, 8, 0, 0, TimeSpan.Zero);
    private static readonly UsagePeriod Period = new(Now.AddHours(1), Now.AddDays(2));

    // ---- двійники (заглушки портів) ----
    private sealed class FakeRepository : IIssueRequestRepository
    {
        public LoadedRequest? Stored;
        public int StageCalls;
        public Task<LoadedRequest?> FindAsync(IssueRequestId id) => Task.FromResult(Stored);
        public void Stage(IssueRequest request, long expectedRevision) => StageCalls++;
    }
    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public int SaveCalls;
        public Task SaveChangesAsync() { SaveCalls++; return Task.CompletedTask; }
    }
    private sealed class FakeActor : IActorContext
    {
        public StorekeeperId Storekeeper { get; } = new(Guid.NewGuid());
    }
    private sealed class FakeClock : IClock
    {
        public DateTimeOffset Now => ServiceTestsTime.Now;
    }
    private static class ServiceTestsTime { public static readonly DateTimeOffset Now = new(2026, 11, 10, 8, 0, 0, TimeSpan.Zero); }
    private sealed class FakeFacts : IIssueFacts
    {
        public bool ToolServiceable = true;
        public bool Unavailable;
        public Task<IssueConfirmations> GetConfirmationsAsync(IssueRequest r)
        {
            if (Unavailable) throw new InvalidOperationException("Джерело фактів недоступне.");
            var e = r.Executor!;
            var tools = r.Items;
            return Task.FromResult(new IssueConfirmations(
                tools.Select(t => new AvailabilityConfirmation(t, true)),
                tools.Select(t => new ConditionConfirmation(t, ToolServiceable)),
                tools.Select(t => new PermitConfirmation(e, t, PermitStatus.Granted, Now.AddDays(30))),
                tools.Select(t => new CalibrationConfirmation(t, false))));
        }
    }

    private static IssueRequest SubmittedRequest()
    {
        var r = IssueRequest.CreateDraft(Period, new ExecutorId(Guid.NewGuid()));
        r.AddItem(new ToolId(Guid.NewGuid()));
        r.Submit();
        return r;
    }

    private static (IssueRequestService svc, FakeRepository repo, FakeUnitOfWork uow, FakeFacts facts, IssueRequest req) Build()
    {
        var req = SubmittedRequest();
        var repo = new FakeRepository { Stored = new LoadedRequest(req, 1) };
        var uow = new FakeUnitOfWork();
        var facts = new FakeFacts();
        var svc = new IssueRequestService(repo, uow, new FakeActor(), new FakeClock(), facts);
        return (svc, repo, uow, facts, req);
    }

    [Fact]
    public async Task ApproveAsync_AllConditionsMet_ApprovesStagesAndSaves()
    {
        var (svc, repo, uow, _, req) = Build();

        await svc.ApproveAsync(req.Id);

        Assert.Equal(RequestStatus.ApprovedForIssue, req.Status);
        Assert.Contains("RequestApprovedForIssue", req.Events);
        Assert.Equal(1, repo.StageCalls);
        Assert.Equal(1, uow.SaveCalls);
    }

    [Fact]
    public async Task ApproveAsync_ConditionNotMet_DoesNotStageOrSave()
    {
        var (svc, repo, uow, facts, req) = Build();
        facts.ToolServiceable = false;
        var eventsBefore = req.Events.ToArray();

        await Assert.ThrowsAsync<InvalidOperationException>(() => svc.ApproveAsync(req.Id));

        Assert.Equal(RequestStatus.Submitted, req.Status);
        Assert.Equal(eventsBefore, req.Events);
        Assert.Equal(0, repo.StageCalls);
        Assert.Equal(0, uow.SaveCalls);
    }

    [Fact]
    public async Task ApproveAsync_FactsUnavailable_DoesNotChangeOrSave()
    {
        var (svc, repo, uow, facts, req) = Build();
        facts.Unavailable = true;

        await Assert.ThrowsAsync<InvalidOperationException>(() => svc.ApproveAsync(req.Id));

        Assert.Equal(RequestStatus.Submitted, req.Status);
        Assert.Equal(0, repo.StageCalls);
        Assert.Equal(0, uow.SaveCalls);
    }

    [Fact]
    public async Task ApproveAsync_RequestNotFound_ThrowsAndSavesNothing()
    {
        var (svc, repo, uow, _, req) = Build();
        repo.Stored = null;

        await Assert.ThrowsAsync<InvalidOperationException>(() => svc.ApproveAsync(req.Id));

        Assert.Equal(0, uow.SaveCalls);
    }
}