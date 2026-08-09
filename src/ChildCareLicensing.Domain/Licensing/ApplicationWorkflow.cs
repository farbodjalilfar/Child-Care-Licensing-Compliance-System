using ChildCareLicensing.Domain.Enums;

namespace ChildCareLicensing.Domain.Licensing;

/// <summary>
/// The states a licence application may move between. Kept in the domain so the rule is
/// stated once and tested without a database, rather than being implied by whichever
/// buttons happen to be on screen.
/// </summary>
public static class ApplicationWorkflow
{
    private static readonly Dictionary<ApplicationStatus, ApplicationStatus[]> AllowedTransitions = new()
    {
        [ApplicationStatus.Draft] = [ApplicationStatus.Submitted],
        [ApplicationStatus.Submitted] = [ApplicationStatus.UnderReview],
        [ApplicationStatus.UnderReview] =
        [
            ApplicationStatus.AdditionalInfoRequired,
            ApplicationStatus.Approved,
            ApplicationStatus.Rejected
        ],
        [ApplicationStatus.AdditionalInfoRequired] = [ApplicationStatus.Submitted],
        [ApplicationStatus.Approved] = [],
        [ApplicationStatus.Rejected] = []
    };

    public static IReadOnlyList<ApplicationStatus> NextStates(ApplicationStatus from)
        => AllowedTransitions.TryGetValue(from, out var next) ? next : [];

    public static bool CanTransition(ApplicationStatus from, ApplicationStatus to)
        => NextStates(from).Contains(to);

    /// <summary>Statuses that sit in the reviewer's queue.</summary>
    public static bool IsAwaitingReview(ApplicationStatus status)
        => status is ApplicationStatus.Submitted or ApplicationStatus.UnderReview;

    /// <summary>A decision has been made and the application is closed to further review.</summary>
    public static bool IsClosed(ApplicationStatus status)
        => status is ApplicationStatus.Approved or ApplicationStatus.Rejected;

    public static string Describe(ApplicationStatus status) => status switch
    {
        ApplicationStatus.Draft => "Draft",
        ApplicationStatus.Submitted => "Submitted",
        ApplicationStatus.UnderReview => "Under review",
        ApplicationStatus.AdditionalInfoRequired => "More information needed",
        ApplicationStatus.Approved => "Approved",
        ApplicationStatus.Rejected => "Rejected",
        _ => status.ToString()
    };
}
