using Application.Domain.Model.ValueObjects;

namespace Application.Domain.Model.Entities;

public sealed partial class IssueRequest
{
    public IssueApproval? Approval { get; private set; }
    public IssueRejection? Rejection { get; private set; }

    public void ApproveForIssue(
        StorekeeperId storekeeper,
        IssueConfirmations confirmations,
        DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(storekeeper);
        ArgumentNullException.ThrowIfNull(confirmations);
        RequireSubmitted();

        var violations = FindViolations(confirmations, now);

        if (violations.Count > 0)
            throw new InvalidOperationException(
                "Видачу не дозволено: " + string.Join("; ", violations));

        Approval = new IssueApproval(storekeeper, now);
        Status = RequestStatus.ApprovedForIssue;
        _events.Add("RequestApprovedForIssue");
    }

    public void RejectIssue(
        StorekeeperId storekeeper,
        IssueConfirmations confirmations,
        DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(storekeeper);
        ArgumentNullException.ThrowIfNull(confirmations);
        RequireSubmitted();

        var violations = FindViolations(confirmations, now);

        if (violations.Count == 0)
            throw new InvalidOperationException(
                "Усі умови видачі виконано, тому заявку не можна відхилити.");

        Rejection = new IssueRejection(
            storekeeper, now, string.Join("; ", violations));
        Status = RequestStatus.Rejected;
        _events.Add("RequestRejected");
    }

    private void RequireSubmitted()
    {
        if (Status != RequestStatus.Submitted)
            throw new InvalidOperationException(
                "Рішення щодо видачі можливе лише для поданої заявки.");
    }

    private List<string> FindViolations(
        IssueConfirmations confirmations,
        DateTimeOffset now)
    {
        var violations = new List<string>();

        foreach (var tool in _items)
        {
            var availability = confirmations.AvailabilityFor(tool);
            if (availability is null)
                violations.Add($"немає підтвердження наявності інструмента {tool.Value}");
            else if (!availability.IsAvailable)
                violations.Add($"інструмента {tool.Value} немає в наявності");

            var condition = confirmations.ConditionFor(tool);
            if (condition is null)
                violations.Add($"немає підтвердження справності інструмента {tool.Value}");
            else if (!condition.IsServiceable)
                violations.Add($"інструмент {tool.Value} несправний");

            var permit = Executor is null
                ? null
                : confirmations.PermitFor(Executor, tool);
            if (permit is null)
                violations.Add($"немає підтвердження допуску виконавця до інструмента {tool.Value}");
            else if (permit.Status == PermitStatus.NotGranted)
                violations.Add($"виконавець не має допуску до інструмента {tool.Value}");
            else if (permit.Status == PermitStatus.Granted && permit.ValidUntil < now)
                violations.Add($"допуск виконавця до інструмента {tool.Value} прострочений");

            var calibration = confirmations.CalibrationFor(tool);
            if (calibration is null)
                violations.Add($"немає підтвердження калібрування інструмента {tool.Value}");
            else if (calibration.RequiresCalibration && calibration.ValidUntil < Period.End)
                violations.Add($"калібрування інструмента {tool.Value} закінчується раніше за кінець періоду використання");
        }

        return violations;
    }
}