using SimpleGymDiary.Core.Data;
using SimpleGymDiary.Core.Enums;
using SimpleGymDiary.Core.Progression;

namespace SimpleGymDiary.Tests;

/// <summary>End-to-end tests against a real (temp-file) SQLite database, including the full two-session progression loop.</summary>
public sealed class AppDatabaseTests : IAsyncLifetime
{
    private readonly string _path = Path.Combine(Path.GetTempPath(), $"gymdiary-test-{Guid.NewGuid():N}.db3");
    private readonly List<AppDatabase> _open = [];

    private async Task<AppDatabase> CreateAsync()
    {
        var db = new AppDatabase(_path);
        _open.Add(db);
        await db.InitializeAsync();
        return db;
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        foreach (var db in _open)
            await db.CloseAsync();
        if (File.Exists(_path))
            File.Delete(_path);
    }

    [Fact]
    public async Task Initialize_SeedsSplitsExercisesAndSettings()
    {
        var db = await CreateAsync();

        var settings = await db.GetSettingsAsync();
        Assert.Equal(10, settings.DefaultRepRangeMin);
        Assert.Equal(2.5, settings.DefaultWeightIncrementKg);

        var splits = await db.GetSplitsAsync();
        Assert.Equal(["Upper Body", "Lower Body", "Push Day", "Pull Day", "Leg Day", "Full Body"],
            splits.Select(s => s.Name).ToArray());

        var upper = await db.GetSplitExercisesAsync(splits[0].Id);
        Assert.Equal(["Lat Pulldown", "Rowing", "Bench Press", "Butterfly", "Lateral Raise"],
            upper.Select(e => e.Name).ToArray());

        // Shared exercises are reused across splits, not duplicated.
        var exercises = await db.GetExercisesAsync();
        Assert.Equal(SeedData.Exercises.Length, exercises.Count);
        Assert.Single(exercises, e => e.Name == "Bench Press");
    }

    [Fact]
    public async Task Seed_AssignsIconsAndTrackingTypes()
    {
        var db = await CreateAsync();
        var exercises = await db.GetExercisesAsync();

        Assert.Equal("pulldown", exercises.First(e => e.Name == "Lat Pulldown").IconKey);
        Assert.Equal("legs", exercises.First(e => e.Name == "Squat").IconKey);
        var pullUps = exercises.First(e => e.Name == "Pull-Ups");
        Assert.Equal(TrackingType.RepBased, pullUps.TrackingType);
        Assert.Equal("pulldown", pullUps.IconKey);
    }

    [Fact]
    public async Task Initialize_IsIdempotent_NoDoubleSeed()
    {
        var db = await CreateAsync();
        // Second connection to the same file: migrations must not re-run.
        var db2 = await CreateAsync();

        Assert.Equal(SeedData.Splits.Length, (await db2.GetSplitsAsync()).Count);
        Assert.Equal(SeedData.Exercises.Length, (await db2.GetExercisesAsync()).Count);
    }

    [Fact]
    public async Task EnsurePresets_RespectsUserDeletions_ByName()
    {
        var db = await CreateAsync();
        var splits = await db.GetSplitsAsync();
        var pushDay = splits.First(s => s.Name == "Push Day");
        await db.ArchiveSplitAsync(pushDay.Id);

        // Re-running the preset seeder must not resurrect the archived split.
        var db2 = await CreateAsync();
        Assert.DoesNotContain(await db2.GetSplitsAsync(), s => s.Name == "Push Day");
    }

    [Fact]
    public async Task FullLoop_SecondSessionGetsAdjustedSuggestion()
    {
        var db = await CreateAsync();
        var splits = await db.GetSplitsAsync();
        var upperId = splits[0].Id;
        var settings = await db.GetSettingsAsync();

        // --- Session 1: user types 60 kg and manages 16 reps (above range -> Up) ---
        var s1 = await db.StartSessionAsync(upperId, new DateTime(2026, 7, 15, 17, 0, 0, DateTimeKind.Utc));
        var entries = await db.GetSessionEntriesAsync(s1.Id);
        Assert.Equal(5, entries.Count);
        Assert.Null(entries[0].SuggestedWeightKg); // first-ever session: no suggestion

        var lat = entries[0];
        lat.WeightKg = 60;
        lat.RepsPerSet = "16,14,12";
        var exercise = (await db.GetExerciseAsync(lat.ExerciseId))!;
        ProgressionEngine.ApplyAutoMark(lat, EffectiveExerciseSettings.Resolve(exercise, settings));
        await db.SaveEntryAsync(lat);
        Assert.Equal(Mark.Up, lat.Mark);

        await db.CompleteSessionAsync(s1.Id, new DateTime(2026, 7, 15, 18, 0, 0, DateTimeKind.Utc));
        Assert.Null(await db.GetInProgressSessionAsync());

        // --- Session 2: suggestion must be 60 + 2.5 ---
        var s2 = await db.StartSessionAsync(upperId, new DateTime(2026, 7, 18, 17, 0, 0, DateTimeKind.Utc));
        var entries2 = await db.GetSessionEntriesAsync(s2.Id);
        Assert.Equal(62.5, entries2[0].SuggestedWeightKg);
        Assert.Equal(62.5, entries2[0].WeightKg);
        Assert.Equal("0,0,0", entries2[0].RepsPerSet);
    }

    [Fact]
    public async Task InProgressSession_IsResumable()
    {
        var db = await CreateAsync();
        var splits = await db.GetSplitsAsync();
        var session = await db.StartSessionAsync(splits[0].Id, DateTime.UtcNow);

        var resumed = await db.GetInProgressSessionAsync();
        Assert.NotNull(resumed);
        Assert.Equal(session.Id, resumed.Id);
    }

    [Fact]
    public async Task ArchivedExercise_DisappearsFromSplit_ButHistoryRemains()
    {
        var db = await CreateAsync();
        var splits = await db.GetSplitsAsync();
        var upper = await db.GetSplitExercisesAsync(splits[0].Id);
        var latId = upper[0].Id;

        var session = await db.StartSessionAsync(splits[0].Id, DateTime.UtcNow);
        await db.CompleteSessionAsync(session.Id, DateTime.UtcNow);

        await db.ArchiveExerciseAsync(latId);

        Assert.Equal(4, (await db.GetSplitExercisesAsync(splits[0].Id)).Count);
        Assert.Equal(5, (await db.GetSessionEntriesAsync(session.Id)).Count); // history untouched
    }

    [Fact]
    public async Task StartSession_SnapshotsNameAndRepRange()
    {
        var db = await CreateAsync();
        var splits = await db.GetSplitsAsync();
        var lat = (await db.GetSplitExercisesAsync(splits[0].Id))[0];
        lat.RepRangeMinOverride = 6;
        lat.RepRangeMaxOverride = 8;
        await db.SaveExerciseAsync(lat);

        var session = await db.StartSessionAsync(splits[0].Id, DateTime.UtcNow);
        var entry = (await db.GetSessionEntriesAsync(session.Id))[0];

        Assert.Equal("Lat Pulldown", entry.ExerciseNameSnapshot);
        Assert.Equal(6, entry.RepMinSnapshot);
        Assert.Equal(8, entry.RepMaxSnapshot);
    }

    [Fact]
    public async Task RenamingExercise_DoesNotRewriteHistorySnapshots()
    {
        var db = await CreateAsync();
        var splits = await db.GetSplitsAsync();
        var session = await db.StartSessionAsync(splits[0].Id, DateTime.UtcNow);
        await db.CompleteSessionAsync(session.Id, DateTime.UtcNow);

        var lat = (await db.GetSplitExercisesAsync(splits[0].Id))[0];
        lat.Name = "Wide-Grip Pulldown";
        await db.SaveExerciseAsync(lat);

        var entry = (await db.GetSessionEntriesAsync(session.Id))[0];
        Assert.Equal("Lat Pulldown", entry.ExerciseNameSnapshot);
    }

    [Fact]
    public async Task SessionsForSplit_OrderedOldestFirst_IncludingInProgress()
    {
        var db = await CreateAsync();
        var splits = await db.GetSplitsAsync();

        var s1 = await db.StartSessionAsync(splits[0].Id, new DateTime(2026, 7, 1, 17, 0, 0, DateTimeKind.Utc));
        await db.CompleteSessionAsync(s1.Id, new DateTime(2026, 7, 1, 18, 0, 0, DateTimeKind.Utc));
        var s2 = await db.StartSessionAsync(splits[0].Id, new DateTime(2026, 7, 8, 17, 0, 0, DateTimeKind.Utc));
        await db.CompleteSessionAsync(s2.Id, new DateTime(2026, 7, 8, 18, 0, 0, DateTimeKind.Utc));
        var s3 = await db.StartSessionAsync(splits[0].Id, new DateTime(2026, 7, 15, 17, 0, 0, DateTimeKind.Utc)); // in progress
        await db.StartSessionAsync(splits[1].Id, DateTime.UtcNow); // other split — excluded

        var timeline = await db.GetSessionsForSplitAsync(splits[0].Id);
        Assert.Equal([s1.Id, s2.Id, s3.Id], timeline.Select(s => s.Id).ToArray());
    }

    [Fact]
    public async Task ReviewTracking_DefaultsAndRoundTrips()
    {
        var db = await CreateAsync();
        var settings = await db.GetSettingsAsync();
        Assert.Equal(0, settings.ReviewRequestCount);
        Assert.Null(settings.LastReviewRequestUtc);

        settings.ReviewRequestCount = 2;
        settings.LastReviewRequestUtc = new DateTime(2026, 7, 1, 10, 0, 0, DateTimeKind.Utc);
        await db.SaveSettingsAsync(settings);

        var reloaded = await db.GetSettingsAsync();
        Assert.Equal(2, reloaded.ReviewRequestCount);
        Assert.Equal(settings.LastReviewRequestUtc, reloaded.LastReviewRequestUtc);
    }

    [Fact]
    public async Task IsVisibleInStats_DefaultsTrue_AndPersists()
    {
        var db = await CreateAsync();
        var exercises = await db.GetExercisesAsync();
        Assert.All(exercises, e => Assert.True(e.IsVisibleInStats));

        exercises[0].IsVisibleInStats = false;
        await db.SaveExerciseAsync(exercises[0]);

        var reloaded = await db.GetExerciseAsync(exercises[0].Id);
        Assert.False(reloaded!.IsVisibleInStats);
    }

    [Fact]
    public async Task ExerciseHistory_OrderedByDate()
    {
        var db = await CreateAsync();
        var splits = await db.GetSplitsAsync();
        var latId = (await db.GetSplitExercisesAsync(splits[0].Id))[0].Id;

        foreach (var (day, weight) in new[] { (1, 60.0), (4, 62.5), (8, 65.0) })
        {
            var s = await db.StartSessionAsync(splits[0].Id, new DateTime(2026, 7, day, 17, 0, 0, DateTimeKind.Utc));
            var e = (await db.GetSessionEntriesAsync(s.Id))[0];
            e.WeightKg = weight;
            e.RepsPerSet = "12,11,10";
            await db.SaveEntryAsync(e);
            await db.CompleteSessionAsync(s.Id, new DateTime(2026, 7, day, 18, 0, 0, DateTimeKind.Utc));
        }

        var history = await db.GetExerciseHistoryAsync(latId);
        Assert.Equal([60.0, 62.5, 65.0], history.Select(h => h.WeightKg!.Value).ToArray());
    }
}
