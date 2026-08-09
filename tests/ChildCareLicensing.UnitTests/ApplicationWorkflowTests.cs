using ChildCareLicensing.Domain.Enums;
using ChildCareLicensing.Domain.Licensing;

namespace ChildCareLicensing.UnitTests;

public class ApplicationWorkflowTests
{
    [Theory]
    [InlineData(ApplicationStatus.Draft, ApplicationStatus.Submitted)]
    [InlineData(ApplicationStatus.Submitted, ApplicationStatus.UnderReview)]
    [InlineData(ApplicationStatus.UnderReview, ApplicationStatus.Approved)]
    [InlineData(ApplicationStatus.UnderReview, ApplicationStatus.Rejected)]
    [InlineData(ApplicationStatus.UnderReview, ApplicationStatus.AdditionalInfoRequired)]
    [InlineData(ApplicationStatus.AdditionalInfoRequired, ApplicationStatus.Submitted)]
    public void PermittedTransitionsAreAllowed(ApplicationStatus from, ApplicationStatus to)
        => Assert.True(ApplicationWorkflow.CanTransition(from, to));

    [Theory]
    [InlineData(ApplicationStatus.Draft, ApplicationStatus.Approved)]
    [InlineData(ApplicationStatus.Submitted, ApplicationStatus.Approved)]
    [InlineData(ApplicationStatus.Submitted, ApplicationStatus.Submitted)]
    [InlineData(ApplicationStatus.AdditionalInfoRequired, ApplicationStatus.Approved)]
    public void ShortcutsToADecisionAreRefused(ApplicationStatus from, ApplicationStatus to)
        => Assert.False(ApplicationWorkflow.CanTransition(from, to));

    [Theory]
    [InlineData(ApplicationStatus.Approved)]
    [InlineData(ApplicationStatus.Rejected)]
    public void DecidedApplicationsCannotMoveAgain(ApplicationStatus decided)
    {
        Assert.Empty(ApplicationWorkflow.NextStates(decided));
        Assert.True(ApplicationWorkflow.IsClosed(decided));
    }

    [Fact]
    public void OnlySubmittedAndUnderReviewSitInTheQueue()
    {
        var queued = Enum.GetValues<ApplicationStatus>()
            .Where(ApplicationWorkflow.IsAwaitingReview)
            .ToArray();

        Assert.Equal([ApplicationStatus.Submitted, ApplicationStatus.UnderReview], queued);
    }
}
