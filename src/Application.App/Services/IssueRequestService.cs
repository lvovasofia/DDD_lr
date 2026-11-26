using Application.App.Ports;
using Application.Domain.Model.ValueObjects;

namespace Application.App.Services;

public sealed class IssueRequestService
{
    private readonly IIssueRequestRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IActorContext _actor;
    private readonly IClock _clock;
    private readonly IIssueFacts _facts;

    public IssueRequestService(
        IIssueRequestRepository repository, IUnitOfWork unitOfWork,
        IActorContext actor, IClock clock, IIssueFacts facts)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _actor = actor;
        _clock = clock;
        _facts = facts;
    }

    public async Task ApproveAsync(IssueRequestId id)
    {
        var loaded = await _repository.FindAsync(id)
                     ?? throw new InvalidOperationException("Заявку не знайдено.");

        var confirmations = await _facts.GetConfirmationsAsync(loaded.Aggregate);


        loaded.Aggregate.ApproveForIssue(
            _actor.Storekeeper, confirmations, _clock.Now);

        _repository.Stage(loaded.Aggregate, loaded.Revision);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task ApplyIssueConfirmedAsync<TMessage>(
        string json,
        Func<string, DateTimeOffset, TMessage> parser,
        Func<TMessage, IssueRequestId> requestId,
        Func<TMessage, IssueIssuedFact> mapper)
    {
        var message = parser(json, _clock.Now);

        var loaded = await _repository.FindAsync(requestId(message))
                     ?? throw new InvalidOperationException("Заявку не знайдено.");

        var fact = mapper(message);
        loaded.Aggregate.ConfirmIssued(fact.Storekeeper, fact.IssuedAt);

        _repository.Stage(loaded.Aggregate, loaded.Revision);
        await _unitOfWork.SaveChangesAsync();
    }
}
