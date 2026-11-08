namespace Application.Domain.Model.ValueObjects;

public sealed record AvailabilityConfirmation
{
    public ToolId Tool { get; }
    public bool IsAvailable { get; }

    public AvailabilityConfirmation(ToolId tool, bool isAvailable)
    {
        if (tool is null)
            throw new ArgumentException(
                "Не зазначено інструмент у підтвердженні наявності.");

        Tool = tool;
        IsAvailable = isAvailable;
    }
}

public sealed record ConditionConfirmation
{
    public ToolId Tool { get; }
    public bool IsServiceable { get; }

    public ConditionConfirmation(ToolId tool, bool isServiceable)
    {
        if (tool is null)
            throw new ArgumentException(
                "Не зазначено інструмент у підтвердженні справності.");

        Tool = tool;
        IsServiceable = isServiceable;
    }
}

public enum PermitStatus
{
    NotRequired, 
    Granted,     
    NotGranted   
}

public sealed record PermitConfirmation
{
    public ExecutorId Executor { get; }
    public ToolId Tool { get; }
    public PermitStatus Status { get; }
    public DateTimeOffset? ValidUntil { get; }

    public PermitConfirmation(
        ExecutorId executor,
        ToolId tool,
        PermitStatus status,
        DateTimeOffset? validUntil = null)
    {
        if (executor is null)
            throw new ArgumentException(
                "Не зазначено виконавця в підтвердженні допуску.");

        if (tool is null)
            throw new ArgumentException(
                "Не зазначено інструмент у підтвердженні допуску.");

        if (status == PermitStatus.Granted && validUntil is null)
            throw new ArgumentException(
                "Для чинного допуску потрібно вказати дату його закінчення.");

        Executor = executor;
        Tool = tool;
        Status = status;
        ValidUntil = validUntil;
    }
}

public sealed record CalibrationConfirmation
{
    public ToolId Tool { get; }
    public bool RequiresCalibration { get; }
    public DateTimeOffset? ValidUntil { get; }

    public CalibrationConfirmation(
        ToolId tool,
        bool requiresCalibration,
        DateTimeOffset? validUntil = null)
    {
        if (tool is null)
            throw new ArgumentException(
                "Не зазначено інструмент у підтвердженні калібрування.");

        if (requiresCalibration && validUntil is null)
            throw new ArgumentException(
                "Для приладу, що підлягає калібруванню, потрібно вказати дату закінчення калібрування.");

        Tool = tool;
        RequiresCalibration = requiresCalibration;
        ValidUntil = validUntil;
    }
}