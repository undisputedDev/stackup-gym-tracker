using SimpleGymDiary.Core.Entities;
using SimpleGymDiary.Core.Enums;
using SimpleGymDiary.Core.Progression;

namespace SimpleGymDiary.Tests;

public class ProgressionEngineTests
{
    private static readonly EffectiveExerciseSettings Default =
        new(RepMin: 10, RepMax: 15, WeightIncrementKg: 2.5, RepIncrement: 1, Rule: CountingSetRule.FirstSet);

    private static Exercise WeightExercise() => new() { Id = 1, Name = "Lat Pulldown", TrackingType = TrackingType.WeightBased };
    private static Exercise RepExercise() => new() { Id = 2, Name = "Pull-Ups", TrackingType = TrackingType.RepBased };

    // ---- CountingReps ----

    [Theory]
    [InlineData("15,12,9", CountingSetRule.FirstSet, 15)]
    [InlineData("15,12,9", CountingSetRule.LastSet, 9)]
    [InlineData("12,16,9", CountingSetRule.BestSet, 16)]
    [InlineData("0,12,9", CountingSetRule.FirstSet, 12)] // untouched 0-sets are skipped
    public void CountingReps_AppliesRule(string reps, CountingSetRule rule, int expected) =>
        Assert.Equal(expected, ProgressionEngine.CountingReps(RepsSerializer.Parse(reps), rule));

    [Theory]
    [InlineData("")]
    [InlineData("0,0,0")]
    public void CountingReps_NothingLogged_ReturnsNull(string reps) =>
        Assert.Null(ProgressionEngine.CountingReps(RepsSerializer.Parse(reps), CountingSetRule.FirstSet));

    // ---- ComputeAutoMark ----

    [Theory]
    [InlineData("9,8,7", Mark.Down)]    // below range
    [InlineData("10,9,8", Mark.Keep)]   // exactly at min
    [InlineData("12,11,10", Mark.Keep)] // in range
    [InlineData("15,12,9", Mark.Keep)]  // exactly at max
    [InlineData("16,14,12", Mark.Up)]   // above range
    [InlineData("", Mark.Keep)]         // nothing logged yet
    public void ComputeAutoMark_UsesRepRange(string reps, Mark expected) =>
        Assert.Equal(expected, ProgressionEngine.ComputeAutoMark(reps, Default));

    [Fact]
    public void ComputeAutoMark_RespectsPerExerciseRange()
    {
        var tight = Default with { RepMin = 6, RepMax = 8 };
        Assert.Equal(Mark.Up, ProgressionEngine.ComputeAutoMark("9", tight));
        Assert.Equal(Mark.Keep, ProgressionEngine.ComputeAutoMark("7", tight));
        Assert.Equal(Mark.Down, ProgressionEngine.ComputeAutoMark("5", tight));
    }

    // ---- ApplyAutoMark ----

    [Fact]
    public void ApplyAutoMark_FollowsAutoMark_WhenNotManual()
    {
        var entry = new SessionEntry { RepsPerSet = "16,12,9" };
        ProgressionEngine.ApplyAutoMark(entry, Default);
        Assert.Equal(Mark.Up, entry.AutoMark);
        Assert.Equal(Mark.Up, entry.Mark);
    }

    [Fact]
    public void ApplyAutoMark_ManualOverride_SurvivesRecomputation()
    {
        var entry = new SessionEntry { RepsPerSet = "16,12,9", Mark = Mark.Keep, MarkIsManual = true };
        ProgressionEngine.ApplyAutoMark(entry, Default);
        Assert.Equal(Mark.Up, entry.AutoMark); // recomputed
        Assert.Equal(Mark.Keep, entry.Mark);   // override kept
    }

    // ---- SuggestNext: weight-based ----

    [Fact]
    public void SuggestNext_FirstSession_NoWeight_TargetIsRepMin()
    {
        var s = ProgressionEngine.SuggestNext(WeightExercise(), Default, last: null, defaultSetCount: 3);
        Assert.Null(s.WeightKg);
        Assert.Equal(10, s.TargetReps);
        Assert.Equal(3, s.SetCount);
    }

    [Theory]
    [InlineData(Mark.Up, 62.5)]
    [InlineData(Mark.Keep, 60)]
    [InlineData(Mark.Down, 57.5)]
    public void SuggestNext_WeightBased_AdjustsByMark(Mark mark, double expected)
    {
        var last = new SessionEntry { WeightKg = 60, RepsPerSet = "12,11,10", Mark = mark };
        var s = ProgressionEngine.SuggestNext(WeightExercise(), Default, last, defaultSetCount: 3);
        Assert.Equal(expected, s.WeightKg);
        Assert.Null(s.TargetReps);
        Assert.Equal(3, s.SetCount);
    }

    [Fact]
    public void SuggestNext_WeightBased_UsesEffectiveIncrement()
    {
        var eff = Default with { WeightIncrementKg = 1.25 };
        var last = new SessionEntry { WeightKg = 10, RepsPerSet = "16", Mark = Mark.Up };
        var s = ProgressionEngine.SuggestNext(WeightExercise(), eff, last, defaultSetCount: 3);
        Assert.Equal(11.25, s.WeightKg);
    }

    [Fact]
    public void SuggestNext_WeightBased_DownClampsAtZero()
    {
        var last = new SessionEntry { WeightKg = 1, RepsPerSet = "5", Mark = Mark.Down };
        var s = ProgressionEngine.SuggestNext(WeightExercise(), Default, last, defaultSetCount: 3);
        Assert.Equal(0, s.WeightKg);
    }

    [Fact]
    public void SuggestNext_KeepsLastSetCount()
    {
        var last = new SessionEntry { WeightKg = 60, RepsPerSet = "12,11,10,9", Mark = Mark.Keep };
        var s = ProgressionEngine.SuggestNext(WeightExercise(), Default, last, defaultSetCount: 3);
        Assert.Equal(4, s.SetCount);
    }

    [Fact]
    public void SuggestNext_ManualOverride_DrivesSuggestion()
    {
        // 16 reps would auto-mark Up, but the user overrode to Keep.
        var last = new SessionEntry { WeightKg = 60, RepsPerSet = "16,14,12", AutoMark = Mark.Up, Mark = Mark.Keep, MarkIsManual = true };
        var s = ProgressionEngine.SuggestNext(WeightExercise(), Default, last, defaultSetCount: 3);
        Assert.Equal(60, s.WeightKg);
    }

    // ---- SuggestNext: rep-based ----

    [Theory]
    [InlineData(Mark.Up, 9)]
    [InlineData(Mark.Keep, 8)]
    [InlineData(Mark.Down, 7)]
    public void SuggestNext_RepBased_AdjustsRepsByMark(Mark mark, int expected)
    {
        var last = new SessionEntry { RepsPerSet = "8,7,6", Mark = mark };
        var s = ProgressionEngine.SuggestNext(RepExercise(), Default, last, defaultSetCount: 3);
        Assert.Null(s.WeightKg);
        Assert.Equal(expected, s.TargetReps);
    }

    [Fact]
    public void SuggestNext_RepBased_DownClampsAtOne()
    {
        var last = new SessionEntry { RepsPerSet = "1", Mark = Mark.Down };
        var s = ProgressionEngine.SuggestNext(RepExercise(), Default, last, defaultSetCount: 3);
        Assert.Equal(1, s.TargetReps);
    }

    [Fact]
    public void SuggestNext_LastEntryWithoutData_TreatedAsFirstSession()
    {
        // Entry exists (e.g. session started, never filled in) but has no logged values.
        var last = new SessionEntry { RepsPerSet = "0,0,0", WeightKg = null };
        var s = ProgressionEngine.SuggestNext(WeightExercise(), Default, last, defaultSetCount: 3);
        Assert.Null(s.WeightKg);
        Assert.Equal(10, s.TargetReps);
    }
}
