using StackUp.Core.Entities;
using StackUp.Core.Review;

namespace StackUp.Tests;

public class ReviewMilestoneTests
{
    private static readonly DateTime Now = new(2026, 7, 23, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Requests_AtMilestone_WithProgress()
    {
        var settings = new AppSettings();
        Assert.True(ReviewMilestone.ShouldRequest(settings, completedSessions: 6, sessionHadProgress: true, Now));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(5)]
    public void TooFewSessions_DoesNotRequest(int sessions) =>
        Assert.False(ReviewMilestone.ShouldRequest(new AppSettings(), sessions, sessionHadProgress: true, Now));

    [Fact]
    public void NoProgressThisSession_DoesNotRequest() =>
        Assert.False(ReviewMilestone.ShouldRequest(new AppSettings(), 10, sessionHadProgress: false, Now));

    [Fact]
    public void LifetimeCap_StopsRequesting()
    {
        var settings = new AppSettings { ReviewRequestCount = 3 };
        Assert.False(ReviewMilestone.ShouldRequest(settings, 50, sessionHadProgress: true, Now));
    }

    [Fact]
    public void RecentRequest_Backs_Off()
    {
        var settings = new AppSettings { ReviewRequestCount = 1, LastReviewRequestUtc = Now.AddDays(-5) };
        Assert.False(ReviewMilestone.ShouldRequest(settings, 10, sessionHadProgress: true, Now));
    }

    [Fact]
    public void OldRequest_AllowsRetry()
    {
        var settings = new AppSettings { ReviewRequestCount = 1, LastReviewRequestUtc = Now.AddDays(-15) };
        Assert.True(ReviewMilestone.ShouldRequest(settings, 10, sessionHadProgress: true, Now));
    }
}
