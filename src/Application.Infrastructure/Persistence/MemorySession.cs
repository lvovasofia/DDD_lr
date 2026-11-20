using Application.App.Ports;
using Application.Domain.Model.Entities;
using Application.Domain.Model.ValueObjects;

namespace Application.Infrastructure.Persistence;

public sealed class MemorySession : IIssueRequestRepository, IUnitOfWork
{
    private readonly Dictionary<IssueRequestId, (IssueRequestState State, long Revision)>
        _rows = new();
    private readonly List<(IssueRequestState State, long Expected)> _staged = new();
    public bool FailNextSave { get; set; }
    public MemorySession(params IssueRequest[] initial)
    {
        foreach (var r in initial)
            _rows[r.Id] = (r.ToState(), 1);
    }

    private long RevisionOf(IssueRequestId id) =>
        _rows.TryGetValue(id, out var row) ? row.Revision : 0;

    public Task<LoadedRequest?> FindAsync(IssueRequestId id)
    {
        if (!_rows.TryGetValue(id, out var row))
            return Task.FromResult<LoadedRequest?>(null);

        return Task.FromResult<LoadedRequest?>(
            new LoadedRequest(IssueRequest.Restore(row.State), row.Revision));
    }

    public void Stage(IssueRequest request, long expectedRevision) =>
        _staged.Add((request.ToState(), expectedRevision));

    public Task SaveChangesAsync()
    {
        try
             {
                if (FailNextSave)
                {
                    FailNextSave = false;

                    throw new InvalidOperationException(
                           "Імітована технічна помилка збереження.");
                }
            foreach (var item in _staged)
                if (RevisionOf(item.State.Id) != item.Expected)
                    throw new InvalidOperationException(
                        "Конфлікт ревізій: заявку вже змінив інший учасник.");

            foreach (var item in _staged)
                _rows[item.State.Id] = (item.State, item.Expected + 1);

            return Task.CompletedTask;
        }
        finally
        {
            _staged.Clear();
        }
    }
}