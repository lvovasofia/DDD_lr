using Application.Domain.Model.Entities;
using Application.Domain.Model.ValueObjects;

namespace Application.App.Ports;

public sealed record LoadedRequest(IssueRequest Aggregate, long Revision);

public interface IIssueRequestRepository
{
    Task<LoadedRequest?> FindAsync(IssueRequestId id);
    void Stage(IssueRequest request, long expectedRevision);
}

public interface IUnitOfWork { Task SaveChangesAsync(); }

public interface IActorContext { StorekeeperId Storekeeper { get; } }

public interface IClock { DateTimeOffset Now { get; } }

public interface IIssueFacts
{
    Task<IssueConfirmations> GetConfirmationsAsync(IssueRequest request);
}