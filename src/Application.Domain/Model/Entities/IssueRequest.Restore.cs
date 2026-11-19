using Application.Domain.Model.ValueObjects;

namespace Application.Domain.Model.Entities;

public sealed record IssueRequestState(
    IssueRequestId Id,
    UsagePeriod Period,
    ExecutorId? Executor,
    RequestStatus Status,
    IReadOnlyList<ToolId> Items,
    IssueApproval? Approval,
    IssueRejection? Rejection,
    IssueHandover? Handover,
    IReadOnlyList<ToolReturn> Returns,
    IReadOnlyList<ReturnInspection> Inspections,
    StorekeeperId? ClosedBy,
    DateTimeOffset? ClosedAt);

public sealed partial class IssueRequest
{
    public IssueRequestState ToState() => new(
        Id, Period, Executor, Status,
        _items.ToArray(), Approval, Rejection, Handover,
        _returns.ToArray(), _inspections.ToArray(),
        ClosedBy, ClosedAt);

    public static IssueRequest Restore(IssueRequestState s)
    {
        ArgumentNullException.ThrowIfNull(s);

        static void Check(bool ok, string message)
        {
            if (!ok)
                throw new InvalidOperationException(
                    "Збережений стан суперечливий: " + message);
        }

        var st = s.Status;
        var approved = st is RequestStatus.ApprovedForIssue
            or RequestStatus.Issued or RequestStatus.PartiallyReturned
            or RequestStatus.Returned or RequestStatus.Closed;
        var issued = st is RequestStatus.Issued
            or RequestStatus.PartiallyReturned
            or RequestStatus.Returned or RequestStatus.Closed;

        Check(s.Items.Distinct().Count() == s.Items.Count,
            "інструменти повторюються.");
        Check(st == RequestStatus.Draft ||
              (s.Items.Count > 0 && s.Executor is not null),
            "подана заявка без інструментів або виконавця.");
        Check((s.Approval is not null) == approved,
            "дозвіл не відповідає статусу.");
        Check((s.Rejection is not null) == (st == RequestStatus.Rejected),
            "відмова не відповідає статусу.");
        Check((s.Handover is not null) == issued,
            "видача не відповідає статусу.");
        Check(s.Returns.Select(r => r.Tool).Distinct().Count() == s.Returns.Count
              && s.Returns.All(r => s.Items.Contains(r.Tool)),
            "повернення дублюються або стосуються чужих інструментів.");
        Check(st is not (RequestStatus.Returned or RequestStatus.Closed)
              || s.Returns.Count == s.Items.Count,
            "не всі інструменти повернено.");
        Check(s.Inspections.All(i => s.Returns.Any(r => r.Tool == i.Tool)),
            "огляд стосується неповерненого інструмента.");
        Check((s.ClosedBy is not null && s.ClosedAt is not null)
              == (st == RequestStatus.Closed),
            "дані закриття не відповідають статусу.");

        var request = new IssueRequest(s.Id, s.Period, s.Executor)
        {
            Status = s.Status,
            Approval = s.Approval,
            Rejection = s.Rejection,
            Handover = s.Handover,
            ClosedBy = s.ClosedBy,
            ClosedAt = s.ClosedAt
        };
        request._items.AddRange(s.Items);
        request._returns.AddRange(s.Returns);
        request._inspections.AddRange(s.Inspections);
        return request;
    }
}
    