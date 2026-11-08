namespace Application.Domain.Model.ValueObjects;

public sealed record IssueConfirmations
{
    public IReadOnlyList<AvailabilityConfirmation> Availability { get; }
    public IReadOnlyList<ConditionConfirmation> Conditions { get; }
    public IReadOnlyList<PermitConfirmation> Permits { get; }
    public IReadOnlyList<CalibrationConfirmation> Calibrations { get; }

    public IssueConfirmations(
        IEnumerable<AvailabilityConfirmation> availability,
        IEnumerable<ConditionConfirmation> conditions,
        IEnumerable<PermitConfirmation> permits,
        IEnumerable<CalibrationConfirmation> calibrations)
    {
        if (availability is null || conditions is null ||
            permits is null || calibrations is null)
            throw new ArgumentException(
                "Набір підтверджень має містити всі чотири переліки.");

        var availabilityList = availability.ToArray();
        var conditionList = conditions.ToArray();
        var permitList = permits.ToArray();
        var calibrationList = calibrations.ToArray();

        if (HasContradiction(availabilityList, a => a.Tool, a => a.IsAvailable) ||
            HasContradiction(conditionList, c => c.Tool, c => c.IsServiceable) ||
            HasContradiction(calibrationList, c => c.Tool, c => (c.RequiresCalibration, c.ValidUntil)) ||
            HasContradiction(permitList, p => (p.Executor, p.Tool), p => (p.Status, p.ValidUntil)))
            throw new ArgumentException(
                "Підтвердження суперечать одне одному.");

        Availability = availabilityList;
        Conditions = conditionList;
        Permits = permitList;
        Calibrations = calibrationList;
    }

    public AvailabilityConfirmation? AvailabilityFor(ToolId tool) =>
        Availability.FirstOrDefault(a => a.Tool == tool);

    public ConditionConfirmation? ConditionFor(ToolId tool) =>
        Conditions.FirstOrDefault(c => c.Tool == tool);

    public CalibrationConfirmation? CalibrationFor(ToolId tool) =>
        Calibrations.FirstOrDefault(c => c.Tool == tool);

    public PermitConfirmation? PermitFor(ExecutorId executor, ToolId tool) =>
        Permits.FirstOrDefault(p => p.Executor == executor && p.Tool == tool);

    public bool Equals(IssueConfirmations? other) =>
        other is not null &&
        SameItems(Availability, other.Availability) &&
        SameItems(Conditions, other.Conditions) &&
        SameItems(Permits, other.Permits) &&
        SameItems(Calibrations, other.Calibrations);

    public override int GetHashCode() =>
        HashCode.Combine(Availability.Count, Conditions.Count, Permits.Count, Calibrations.Count);

    private static bool HasContradiction<T, TKey, TValue>(
        IEnumerable<T> items, Func<T, TKey> key, Func<T, TValue> value) =>
        items.GroupBy(key).Any(group => group.Select(value).Distinct().Count() > 1);

    private static bool SameItems<T>(IReadOnlyList<T> a, IReadOnlyList<T> b) =>
        a.Count == b.Count && a.All(b.Contains);
}