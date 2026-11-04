
using Application.Domain.Model.ValueObjects;

namespace Application.Domain.Model.Entities;

public sealed class IssueRequest
{
    private readonly List<ToolId> _items = new();
    private readonly List<string> _events = new();

    public IssueRequestId Id { get; }
    public ExecutorId? Executor { get; private set; }
    public UsagePeriod Period { get; private set; }
    public RequestStatus Status { get; private set; }

    public IReadOnlyList<ToolId> Items =>
        _items.AsReadOnly();

    public IReadOnlyList<string> Events =>
        _events.AsReadOnly();

    private IssueRequest(
        IssueRequestId id,
        UsagePeriod period,
        ExecutorId? executor)
    {
        Id = id;
        Period = period;
        Executor = executor;
        Status = RequestStatus.Draft;
    }

    public static IssueRequest CreateDraft(
        UsagePeriod period,
        ExecutorId? executor = null)
    {
        ArgumentNullException.ThrowIfNull(period);

        var request = new IssueRequest(
            IssueRequestId.New(),
            period,
            executor);

        request._events.Add("RequestDraftCreated");

        return request;
    }

    public void AddItem(ToolId toolId)
    {
        RequireDraft();

        ArgumentNullException.ThrowIfNull(toolId);

        if (_items.Contains(toolId))
            throw new InvalidOperationException(
                "Інструмент уже додано до заявки.");

        _items.Add(toolId);
        _events.Add("RequestItemAdded");
    }

    public void RemoveItem(ToolId toolId)
    {
        RequireDraft();

        ArgumentNullException.ThrowIfNull(toolId);

        if (!_items.Contains(toolId))
            throw new InvalidOperationException(
                "Інструмент відсутній у заявці.");

        _items.Remove(toolId);
        _events.Add("RequestItemRemoved");
    }

    public void ChangePeriod(UsagePeriod period)
    {
        RequireDraft();
        ArgumentNullException.ThrowIfNull(period);

        Period = period;
        _events.Add("RequestPeriodChanged");
    }

    public void SetExecutor(ExecutorId executor)
    {
        RequireDraft();
        ArgumentNullException.ThrowIfNull(executor);

        Executor = executor;
        _events.Add("RequestExecutorAssigned");
    }

    public void Submit()
    {
        RequireDraft();

        if (_items.Count == 0)
            throw new InvalidOperationException(
                "Не можна подати порожню заявку.");

        if (Executor is null)
            throw new InvalidOperationException(
                "Не зазначено виконавця.");

        Status = RequestStatus.Submitted;
        _events.Add("RequestSubmitted");
    }

    private void RequireDraft()
    {
        if (Status != RequestStatus.Draft)
            throw new InvalidOperationException(
                "Редагування можливе лише для чернетки.");
    }
}

