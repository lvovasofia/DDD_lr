using Application.Domain.Model.ValueObjects;
using Xunit;

namespace Application.Tests.Application.Domain;

public class ConfirmationsTests
{
    private static readonly DateTimeOffset Date =
        new(2026, 11, 20, 0, 0, 0, TimeSpan.Zero);

    private static ToolId NewTool() => new(Guid.NewGuid());
    private static ExecutorId NewExecutor() => new(Guid.NewGuid());

    [Fact]
    public void AvailabilityConfirmation_WithoutTool_Rejects()
    {
        // Arrange
        ToolId tool = null!;

        // Act
        var act = () => new AvailabilityConfirmation(tool, true);

        // Assert
        Assert.Throws<ArgumentException>(act);
    }

    [Fact]
    public void AvailabilityConfirmations_WithSameValues_AreEqual()
    {
        // Arrange
        var tool = NewTool();

        // Act
        var first = new AvailabilityConfirmation(tool, true);
        var second = new AvailabilityConfirmation(tool, true);

        // Assert
        Assert.Equal(first, second);
    }

    [Fact]
    public void ConditionConfirmation_WithoutTool_Rejects()
    {
        // Arrange
        ToolId tool = null!;

        // Act
        var act = () => new ConditionConfirmation(tool, true);

        // Assert
        Assert.Throws<ArgumentException>(act);
    }

    [Fact]
    public void PermitConfirmation_GrantedWithDate_Succeeds()
    {
        // Arrange
        var executor = NewExecutor();
        var tool = NewTool();

        // Act
        var permit = new PermitConfirmation(
            executor, tool, PermitStatus.Granted, Date);

        // Assert
        Assert.Equal(PermitStatus.Granted, permit.Status);
        Assert.Equal(Date, permit.ValidUntil);
    }

    [Fact]
    public void PermitConfirmation_GrantedWithoutDate_Rejects()
    {
        // Arrange
        var executor = NewExecutor();
        var tool = NewTool();

        // Act
        var act = () => new PermitConfirmation(
            executor, tool, PermitStatus.Granted);

        // Assert
        Assert.Throws<ArgumentException>(act);
    }

    [Fact]
    public void PermitConfirmation_NotRequiredWithoutDate_Succeeds()
    {
        // Arrange
        var executor = NewExecutor();
        var tool = NewTool();

        // Act
        var permit = new PermitConfirmation(
            executor, tool, PermitStatus.NotRequired);

        // Assert
        Assert.Null(permit.ValidUntil);
    }

    [Fact]
    public void PermitConfirmation_WithoutExecutor_Rejects()
    {
        // Arrange
        ExecutorId executor = null!;
        var tool = NewTool();

        // Act
        var act = () => new PermitConfirmation(
            executor, tool, PermitStatus.NotRequired);

        // Assert
        Assert.Throws<ArgumentException>(act);
    }

    [Fact]
    public void CalibrationConfirmation_RequiredWithoutDate_Rejects()
    {
        // Arrange
        var tool = NewTool();

        // Act
        var act = () => new CalibrationConfirmation(tool, true);

        // Assert
        Assert.Throws<ArgumentException>(act);
    }

    [Fact]
    public void CalibrationConfirmation_NotRequiredWithoutDate_Succeeds()
    {
        // Arrange
        var tool = NewTool();

        // Act
        var calibration = new CalibrationConfirmation(tool, false);

        // Assert
        Assert.False(calibration.RequiresCalibration);
        Assert.Null(calibration.ValidUntil);
    }

    [Fact]
    public void IssueConfirmations_Contradictory_Rejects()
    {
        // Arrange
        var tool = NewTool();
        var serviceable = new ConditionConfirmation(tool, true);
        var broken = new ConditionConfirmation(tool, false);

        // Act
        var act = () => new IssueConfirmations(
            [], [serviceable, broken], [], []);

        // Assert
        Assert.Throws<ArgumentException>(act);
    }

    [Fact]
    public void IssueConfirmations_DuplicateSameFact_Succeeds()
    {
        // Arrange
        var tool = NewTool();
        var available = new AvailabilityConfirmation(tool, true);
        var sameAgain = new AvailabilityConfirmation(tool, true);

        // Act
        var confirmations = new IssueConfirmations(
            [available, sameAgain], [], [], []);

        // Assert
        Assert.True(confirmations.AvailabilityFor(tool)!.IsAvailable);
    }

    [Fact]
    public void IssueConfirmations_SameItemsInDifferentOrder_AreEqual()
    {
        // Arrange
        var a = new AvailabilityConfirmation(NewTool(), true);
        var b = new AvailabilityConfirmation(NewTool(), true);

        // Act
        var first = new IssueConfirmations([a, b], [], [], []);
        var second = new IssueConfirmations([b, a], [], [], []);

        // Assert
        Assert.Equal(first, second);
    }

    [Fact]
    public void StorekeeperId_EmptyGuid_Rejects()
    {
        // Arrange
        var empty = Guid.Empty;

        // Act
        var act = () => new StorekeeperId(empty);

        // Assert
        Assert.Throws<ArgumentException>(act);
    }
}