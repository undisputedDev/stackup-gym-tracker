using StackUp.Core.Entities;

namespace StackUp.Core.Review;

/// <summary>
/// Decides when to request the in-app store review. The OS owns the dialog itself
/// (and quota-limits how often it appears); this is our own conservative layer on top:
/// only at a positive milestone, capped for a user's lifetime, spaced well apart.
/// </summary>
public static class ReviewMilestone
{
    /// <summary>User has a real habit going before we ask (~3 weeks at 2 sessions/week).</summary>
    public const int MinCompletedSessions = 6;

    /// <summary>Never request more than this many times, ever.</summary>
    public const int MaxLifetimeRequests = 3;

    /// <summary>Minimum gap between two requests.</summary>
    public static readonly TimeSpan MinInterval = TimeSpan.FromDays(14);

    /// <param name="sessionHadProgress">The just-finished session marked at least one exercise ▲.</param>
    public static bool ShouldRequest(AppSettings settings, int completedSessions, bool sessionHadProgress, DateTime nowUtc) =>
        sessionHadProgress
        && completedSessions >= MinCompletedSessions
        && settings.ReviewRequestCount < MaxLifetimeRequests
        && (settings.LastReviewRequestUtc is not { } last || nowUtc - last >= MinInterval);
}
