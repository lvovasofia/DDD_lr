
namespace Application.Domain.Model.ValueObjects;

public sealed record ApplicationId
{
    public Guid Value { get; }

    public ApplicationId(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException(
                "Ідентифікатор заявки не може бути порожнім.");
        }

        Value = value;
    }

    public static ApplicationId New()
    {
        return new ApplicationId(Guid.NewGuid());
    }
}
