using Application.App.Services;
using Application.Domain.Model.ValueObjects;
using Application.Integration.Contracts;

namespace Application.Integration.Translation;

public static class IssueConfirmedTranslator
{
    public static IssueRequestId RequestIdOf(IssueConfirmedV1 m) =>
        new(m.RequestId);

    public static IssueIssuedFact ToLocal(IssueConfirmedV1 m) =>
        new(new StorekeeperId(m.StorekeeperId), m.IssuedAtUtc);
}