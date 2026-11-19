using Application.Domain.Model.Entities;
using Application.Domain.Model.ValueObjects;
using Xunit;

namespace Application.Tests.Application.Domain;

public class ReturnTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 11, 10, 8, 0, 0, TimeSpan.Zero);

    private static readonly DateTimeOffset IssueTime = Now.AddHours(2);

    private static readonly UsagePeriod Period =
        new(Now.AddHours(1), Now.AddDays(2));

    private static readonly StorekeeperId Storekeeper =
        new(Guid.NewGuid());



    private static IssueConfirmations ValidConfirmations(IssueRequest request)
    {
        var executor = request.Executor!;
        var tools = request.Items;

        return new IssueConfirmations(
            tools.Select(t => new AvailabilityConfirmation(t, true)),
            tools.Select(t => new ConditionConfirmation(t, true)),
            tools.Select(t => new PermitConfirmation(
                executor, t, PermitStatus.Granted, Now.AddDays(30))),
            tools.Select(t => new CalibrationConfirmation(t, false)));
    }

    private static IssueRequest ApprovedRequest(int toolCount = 1)
    {
        var request = IssueRequest.CreateDraft(
            Period, new ExecutorId(Guid.NewGuid()));

        for (var i = 0; i < toolCount; i++)
            request.AddItem(new ToolId(Guid.NewGuid()));

        request.Submit();
        request.ApproveForIssue(Storekeeper, ValidConfirmations(request), Now);
        return request;
    }

    private static IssueRequest SubmittedRequest()
    {
        var request = IssueRequest.CreateDraft(
            Period, new ExecutorId(Guid.NewGuid()));
        request.AddItem(new ToolId(Guid.NewGuid()));
        request.Submit();
        return request;
    }

    private static IssueRequest IssuedRequest(int toolCount = 1)
    {
        var request = ApprovedRequest(toolCount);
        request.ConfirmIssued(Storekeeper, IssueTime);
        return request;
    }

    private static IssueRequest ReturnedRequest(int toolCount = 1)
    {
        var request = IssuedRequest(toolCount);

        foreach (var tool in request.Items)
            request.RegisterReturn(tool, IssueTime.AddHours(1));

        return request;
    }
    
    private sealed record Snapshot(
        RequestStatus Status,
        ToolId[] Items,
        string[] Events,
        IssueHandover? Handover,
        ToolReturn[] Returns,
        ReturnInspection[] Inspections,
        DateTimeOffset? ClosedAt);

    private static Snapshot Take(IssueRequest r) =>
        new(r.Status, r.Items.ToArray(), r.Events.ToArray(),
            r.Handover, r.Returns.ToArray(), r.Inspections.ToArray(),
            r.ClosedAt);

    private static void AssertUnchanged(Snapshot before, IssueRequest r)
    {
        Assert.Equal(before.Status, r.Status);
        Assert.Equal(before.Items, r.Items);
        Assert.Equal(before.Events, r.Events);
        Assert.Equal(before.Handover, r.Handover);
        Assert.Equal(before.Returns, r.Returns);
        Assert.Equal(before.Inspections, r.Inspections);
        Assert.Equal(before.ClosedAt, r.ClosedAt);
    }

   
    [Fact]
    public void ConfirmIssued_WhenApproved_Succeeds()
    {
        // Arrange
        var request = ApprovedRequest();

        // Act
        request.ConfirmIssued(Storekeeper, IssueTime);

        // Assert
        Assert.Equal(RequestStatus.Issued, request.Status);
        Assert.Contains("RequestIssued", request.Events);
        Assert.NotNull(request.Handover);
        Assert.Equal(Storekeeper, request.Handover!.IssuedBy);
        Assert.Equal(IssueTime, request.Handover.IssuedAt);
    }

    [Fact]
    public void ConfirmIssued_WhenSubmitted_Rejects()
    {
       
        var request = SubmittedRequest();
        var before = Take(request);

        
        var act = () => request.ConfirmIssued(Storekeeper, IssueTime);

        
        Assert.Throws<InvalidOperationException>(act);
        AssertUnchanged(before, request);
    }

    [Fact]
    public void ConfirmIssued_WhenAlreadyIssued_Rejects()
    {
        
        var request = IssuedRequest();
        var before = Take(request);

       
        var act = () => request.ConfirmIssued(Storekeeper, IssueTime);

        
        Assert.Throws<InvalidOperationException>(act);
        AssertUnchanged(before, request);
    }
    

    [Fact]
    public void RegisterReturn_OneOfTwoTools_SetsPartiallyReturned()
    {
        var request = IssuedRequest(toolCount: 2);
        
        request.RegisterReturn(request.Items[0], IssueTime.AddHours(1));
        Assert.Equal(RequestStatus.PartiallyReturned, request.Status);
        Assert.Contains("RequestToolReturned", request.Events);
        Assert.Single(request.Returns);
    }

    [Fact]
    public void RegisterReturn_LastTool_SetsReturned()
    {
        var request = IssuedRequest(toolCount: 2);
        request.RegisterReturn(request.Items[0], IssueTime.AddHours(1));
        
        request.RegisterReturn(request.Items[1], IssueTime.AddHours(2));
        
        Assert.Equal(RequestStatus.Returned, request.Status);
        Assert.Equal(2, request.Returns.Count);
    }

    [Fact]
    public void RegisterReturn_ToolNotInRequest_Rejects()
    {
        var request = IssuedRequest();
        var foreignTool = new ToolId(Guid.NewGuid());
        var before = Take(request);
        
        var act = () => request.RegisterReturn(foreignTool, IssueTime.AddHours(1));
        
        Assert.Throws<InvalidOperationException>(act);
        AssertUnchanged(before, request);
    }

    [Fact]
    public void RegisterReturn_SameToolTwice_Rejects()
    {
        var request = IssuedRequest(toolCount: 2);
        var tool = request.Items[0];
        request.RegisterReturn(tool, IssueTime.AddHours(1));
        var before = Take(request);
        
        var act = () => request.RegisterReturn(tool, IssueTime.AddHours(2));
        
        Assert.Throws<InvalidOperationException>(act);
        AssertUnchanged(before, request);
    }

    [Fact]
    public void RegisterReturn_BeforeIssue_Rejects()
    {
        var request = ApprovedRequest();
        var before = Take(request);
        
        var act = () => request.RegisterReturn(request.Items[0], IssueTime);
        
        Assert.Throws<InvalidOperationException>(act);
        AssertUnchanged(before, request);
    }

    [Fact]
    public void RegisterReturn_AtIssueMoment_Succeeds()
    {
        
        var request = IssuedRequest();

       
        request.RegisterReturn(request.Items[0], IssueTime);

        
        Assert.Equal(RequestStatus.Returned, request.Status);
    }

    [Fact]
    public void RegisterReturn_BeforeIssueMoment_Rejects()
    {
        
        var request = IssuedRequest();
        var before = Take(request);

      
        var act = () => request.RegisterReturn(
            request.Items[0], IssueTime.AddMinutes(-1));

       
        Assert.Throws<InvalidOperationException>(act);
        AssertUnchanged(before, request);
    }
    

    [Fact]
    public void RecordInspection_ReturnedTool_Succeeds()
    {
        
        var request = ReturnedRequest();
        var inspection = new ReturnInspection(request.Items[0], true);

      
        request.RecordInspection(inspection);

        
        Assert.Equal(RequestStatus.Returned, request.Status);
        Assert.Contains("RequestInspectionRecorded", request.Events);
        Assert.Single(request.Inspections);
        Assert.Equal(inspection, request.Inspections[0]);
    }

    [Fact]
    public void RecordInspection_ToolNotReturned_Rejects()
    {
        var request = IssuedRequest(toolCount: 2);
        request.RegisterReturn(request.Items[0], IssueTime.AddHours(1));
        var notReturned = new ReturnInspection(request.Items[1], true);
        var before = Take(request);

       
        var act = () => request.RecordInspection(notReturned);

      
        Assert.Throws<InvalidOperationException>(act);
        AssertUnchanged(before, request);
    }

    [Fact]
    public void RecordInspection_Repeated_Rejects()
    {
       
        var request = ReturnedRequest();
        request.RecordInspection(new ReturnInspection(request.Items[0], true));
        var before = Take(request);

        
        var act = () => request.RecordInspection(
            new ReturnInspection(request.Items[0], false));

        
        Assert.Throws<InvalidOperationException>(act);
        AssertUnchanged(before, request);
    }

    

    [Fact]
    public void Close_AllReturnedAndOperational_Succeeds()
    {
        
        var request = ReturnedRequest(toolCount: 2);
        foreach (var tool in request.Items)
            request.RecordInspection(new ReturnInspection(tool, true));

        
        request.Close(Storekeeper, IssueTime.AddDays(1));

        
        Assert.Equal(RequestStatus.Closed, request.Status);
        Assert.Contains("RequestClosed", request.Events);
        Assert.Equal(Storekeeper, request.ClosedBy);
        Assert.Equal(IssueTime.AddDays(1), request.ClosedAt);
    }

    [Fact]
    public void Close_WhenPartiallyReturned_Rejects()
    {
        
        var request = IssuedRequest(toolCount: 2);
        request.RegisterReturn(request.Items[0], IssueTime.AddHours(1));
        request.RecordInspection(new ReturnInspection(request.Items[0], true));
        var before = Take(request);

      
        var act = () => request.Close(Storekeeper, IssueTime.AddDays(1));

       
        Assert.Throws<InvalidOperationException>(act);
        AssertUnchanged(before, request);
    }

    [Fact]
    public void Close_WhenInspectionMissing_Rejects()
    {
       
        var request = ReturnedRequest(toolCount: 2);
        request.RecordInspection(new ReturnInspection(request.Items[0], true));
        var before = Take(request);

       
        var act = () => request.Close(Storekeeper, IssueTime.AddDays(1));

       
        Assert.Throws<InvalidOperationException>(act);
        AssertUnchanged(before, request);
    }

    [Fact]
    public void Close_WhenToolDamaged_Rejects()
    {
        
        var request = ReturnedRequest(toolCount: 2);
        request.RecordInspection(new ReturnInspection(request.Items[0], true));
        request.RecordInspection(new ReturnInspection(request.Items[1], false));
        var before = Take(request);

     
        var act = () => request.Close(Storekeeper, IssueTime.AddDays(1));

        
        Assert.Throws<InvalidOperationException>(act);
        AssertUnchanged(before, request);
    }

    [Fact]
    public void Close_WhenAlreadyClosed_Rejects()
    {
       
        var request = ReturnedRequest();
        request.RecordInspection(new ReturnInspection(request.Items[0], true));
        request.Close(Storekeeper, IssueTime.AddDays(1));
        var before = Take(request);

        
        var act = () => request.Close(Storekeeper, IssueTime.AddDays(2));

        
        Assert.Throws<InvalidOperationException>(act);
        AssertUnchanged(before, request);
    }
    

    [Fact]
    public void IssueHandover_WithoutStorekeeper_Rejects()
    {
        var act = () => new IssueHandover(null!, IssueTime);

        Assert.Throws<ArgumentException>(act);
    }

    [Fact]
    public void ToolReturn_WithoutTool_Rejects()
    {
        var act = () => new ToolReturn(null!, IssueTime);

        Assert.Throws<ArgumentException>(act);
    }

    [Fact]
    public void ReturnInspection_WithoutTool_Rejects()
    {
        var act = () => new ReturnInspection(null!, true);

        Assert.Throws<ArgumentException>(act);
    }

    [Fact]
    public void ReturnInspections_WithSameValues_AreEqual()
    {
        var tool = new ToolId(Guid.NewGuid());

        var first = new ReturnInspection(tool, true);
        var second = new ReturnInspection(tool, true);

        Assert.Equal(first, second);
    }
}