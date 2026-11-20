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

        // предметне рішення: правила перевіряє агрегат, не служба
        loaded.Aggregate.ApproveForIssue(
            _actor.Storekeeper, confirmations, _clock.Now);

        _repository.Stage(loaded.Aggregate, loaded.Revision);
        await _unitOfWork.SaveChangesAsync();   // після цього рядка можна повідомляти про успіх
    }
}