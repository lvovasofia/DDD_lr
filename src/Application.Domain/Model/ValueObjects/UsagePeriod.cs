
namespace Application.Domain.Model.ValueObjects;

public sealed record UsagePeriod
{
    public DateTimeOffset Start { get; }
    public DateTimeOffset End { get; }

    public UsagePeriod(DateTimeOffset start, DateTimeOffset end)
    {
        if (start >= end)
        {
            throw new ArgumentException(
                "Дата початку повинна бути раніше дати завершення.");
        }

        Start = start;
        End = end;
    }
}
