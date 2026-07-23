using Plugin.Maui.AppRating;
using StackUp.Core.Data;
using StackUp.Core.Review;

namespace StackUp.App.Services;

/// <summary>
/// Requests the platform in-app review dialog after a positive workout milestone.
/// The OS decides whether the dialog actually appears and never reports the outcome;
/// we only track how often we asked (see <see cref="ReviewMilestone"/> for the rules).
/// </summary>
public class ReviewPrompter
{
    private readonly AppDatabase _db;
    private readonly IAppRating _appRating;

    public ReviewPrompter(AppDatabase db, IAppRating appRating)
    {
        _db = db;
        _appRating = appRating;
    }

    public async Task TryRequestAsync(bool sessionHadProgress)
    {
        // In-app review exists only on the mobile stores.
        if (DeviceInfo.Current.Platform != DevicePlatform.Android &&
            DeviceInfo.Current.Platform != DevicePlatform.iOS)
            return;

        var settings = await _db.GetSettingsAsync();
        var (completedSessions, _) = await _db.GetCompletedSessionStatsAsync();
        if (!ReviewMilestone.ShouldRequest(settings, completedSessions, sessionHadProgress, DateTime.UtcNow))
            return;

        settings.ReviewRequestCount++;
        settings.LastReviewRequestUtc = DateTime.UtcNow;
        await _db.SaveSettingsAsync(settings);

        try
        {
            await _appRating.PerformInAppRateAsync(false);
        }
        catch
        {
            // Sideloaded builds (no Play services) can throw — never disturb the user for this.
        }
    }
}
