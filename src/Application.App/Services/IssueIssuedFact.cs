using Application.Domain.Model.ValueObjects;

namespace Application.App.Services;

public sealed record IssueIssuedFact(StorekeeperId Storekeeper, DateTimeOffset IssuedAt);