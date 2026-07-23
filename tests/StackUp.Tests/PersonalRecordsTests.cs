using StackUp.Core.Data;
using StackUp.Core.Progression;

namespace StackUp.Tests;

public class PersonalRecordsTests
{
    private static ExerciseHistoryPoint Point(double? weight, string reps) =>
        new() { WeightKg = weight, RepsPerSet = reps };

    [Fact]
    public void BestWeight_IgnoresPrefilledEntriesWithoutReps()
    {
        // 100 kg was only a prefilled suggestion (no reps logged) — never lifted.
        var points = new[] { Point(100, "0,0,0"), Point(60, "12,10,8"), Point(57.5, "15,12,9") };
        Assert.Equal(60, PersonalRecords.BestWeight(points));
    }

    [Fact]
    public void BestWeight_NullWhenNoPerformedHistory()
    {
        Assert.Null(PersonalRecords.BestWeight([]));
        Assert.Null(PersonalRecords.BestWeight([Point(100, "0,0,0"), Point(90, "")]));
    }

    [Fact]
    public void BestSetReps_MaxAcrossAllSets()
    {
        var points = new[] { Point(null, "8,12,9"), Point(null, "10,10") };
        Assert.Equal(12, PersonalRecords.BestSetReps(points));
    }

    [Fact]
    public void BestSetReps_NullWhenNothingLogged()
    {
        Assert.Null(PersonalRecords.BestSetReps([]));
        Assert.Null(PersonalRecords.BestSetReps([Point(null, "0,0,0")]));
    }
}
