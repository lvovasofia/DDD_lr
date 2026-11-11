using Application.Domain.Model.ValueObjects;

namespace Application.Domain.Model.Entities;

public sealed partial class IssueRequest
{
    private readonly List<ToolReturn> _returns = new();
    private readonly List<ReturnInspection> _inspections = new();

    public IssueHandover? Handover { get; private set; }
    public StorekeeperId? ClosedBy { get; private set; }
    public DateTimeOffset? ClosedAt { get; private set; }

    public IReadOnlyList<ToolReturn> Returns =>
        _returns.AsReadOnly();

    public IReadOnlyList<ReturnInspection> Inspections =>
        _inspections.AsReadOnly();

    
    public void ConfirmIssued(StorekeeperId storekeeper, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(storekeeper);

        if (Status != RequestStatus.ApprovedForIssue)
            throw new InvalidOperationException(
                "Видачу можна зареєструвати лише для заявки, на яку дано дозвіл.");

        Handover = new IssueHandover(storekeeper, now);
        Status = RequestStatus.Issued;
        _events.Add("RequestIssued");
    }

   
    public void RegisterReturn(ToolId tool, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(tool);

        if (Status != RequestStatus.Issued &&
            Status != RequestStatus.PartiallyReturned)
            throw new InvalidOperationException(
                "Повернення можливе лише після видачі та до завершення повернення.");

        if (!_items.Contains(tool))
            throw new InvalidOperationException(
                "Інструмента немає в заявці.");

        if (_returns.Any(r => r.Tool == tool))
            throw new InvalidOperationException(
                "Інструмент уже повернено.");

        if (Handover is null || now < Handover.IssuedAt)
            throw new InvalidOperationException(
                "Повернення не може бути раніше моменту видачі.");

        _returns.Add(new ToolReturn(tool, now));
        Status = _returns.Count == _items.Count
            ? RequestStatus.Returned
            : RequestStatus.PartiallyReturned;
        _events.Add("RequestToolReturned");
    }

    
    public void RecordInspection(ReturnInspection inspection)
    {
        ArgumentNullException.ThrowIfNull(inspection);

        if (Status != RequestStatus.PartiallyReturned &&
            Status != RequestStatus.Returned)
            throw new InvalidOperationException(
                "Огляд можна прийняти лише після повернення інструмента.");

        if (!_returns.Any(r => r.Tool == inspection.Tool))
            throw new InvalidOperationException(
                "Інструмент ще не повернено, тому огляд неможливий.");

        if (_inspections.Any(i => i.Tool == inspection.Tool))
            throw new InvalidOperationException(
                "Результат огляду цього інструмента вже прийнято.");

        _inspections.Add(inspection);
        _events.Add("RequestInspectionRecorded");
    }
    
    public void Close(StorekeeperId storekeeper, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(storekeeper);

        if (Status != RequestStatus.Returned)
            throw new InvalidOperationException(
                "Закрити можна лише заявку, за якою повернено всі інструменти.");

        var violations = new List<string>();

        foreach (var tool in _items)
        {
            var inspection = _inspections.FirstOrDefault(i => i.Tool == tool);

            if (inspection is null)
                violations.Add($"немає результату огляду інструмента {tool.Value}");
            else if (!inspection.IsOperational)
                violations.Add($"інструмент {tool.Value} непрацездатний");
        }

        if (violations.Count > 0)
            throw new InvalidOperationException(
                "Заявку не можна закрити: " + string.Join("; ", violations));

        ClosedBy = storekeeper;
        ClosedAt = now;
        Status = RequestStatus.Closed;
        _events.Add("RequestClosed");
    }
}