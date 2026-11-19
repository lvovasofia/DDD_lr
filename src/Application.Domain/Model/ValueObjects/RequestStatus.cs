
namespace Application.Domain.Model.ValueObjects;

public enum RequestStatus
{
    Draft,
    Submitted,
    ApprovedForIssue,
    Issued,
    PartiallyReturned,
    Returned,
    Closed,
    Rejected

}
