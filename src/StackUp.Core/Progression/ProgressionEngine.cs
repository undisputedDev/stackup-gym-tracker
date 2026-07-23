using StackUp.Core.Entities;
using StackUp.Core.Enums;

namespace StackUp.Core.Progression;

/// <summary>
/// The core progression logic: computes the up/down/keep marking from logged reps
/// and suggests next-session values from the previous entry's effective marking.
/// Pure functions — no I/O, fully unit-testable.
/// </summary>
public static class ProgressionEngine
{
    /// <summary>The rep count that decides the marking, per the counting-set rule. Null if nothing was logged.</summary>
    public static int? CountingReps(int[] reps, CountingSetRule rule)
    {
        // Sets still at 0 reps (untouched prefills) don't count as logged.
        var logged = reps.Where(r => r > 0).ToArray();
        if (logged.Length == 0)
            return null;

        return rule switch
        {
            CountingSetRule.FirstSet => logged[0],
            CountingSetRule.LastSet => logged[^1],
            CountingSetRule.BestSet => logged.Max(),
            _ => logged[0],
        };
    }

    /// <summary>Computes the automatic marking from an entry's logged reps and the effective rep range.</summary>
    public static Mark ComputeAutoMark(string repsPerSet, EffectiveExerciseSettings eff)
    {
        var r = CountingReps(RepsSerializer.Parse(repsPerSet), eff.Rule);
        if (r is null)
            return Mark.Keep;
        if (r < eff.RepMin)
            return Mark.Down;
        if (r > eff.RepMax)
            return Mark.Up;
        return Mark.Keep;
    }

    /// <summary>
    /// Recomputes <see cref="SessionEntry.AutoMark"/> after a reps edit. The effective
    /// <see cref="SessionEntry.Mark"/> follows unless the user manually overrode it.
    /// </summary>
    public static void ApplyAutoMark(SessionEntry entry, EffectiveExerciseSettings eff)
    {
        entry.AutoMark = ComputeAutoMark(entry.RepsPerSet, eff);
        if (!entry.MarkIsManual)
            entry.Mark = entry.AutoMark;
    }

    /// <summary>
    /// Suggests next-session values from the most recent completed entry for the exercise
    /// (exercise-scoped: the same exercise in two splits shares its progression).
    /// </summary>
    public static Suggestion SuggestNext(Exercise exercise, EffectiveExerciseSettings eff, SessionEntry? last, int defaultSetCount)
    {
        var lastReps = last is null ? [] : RepsSerializer.Parse(last.RepsPerSet);
        var hasData = last is not null &&
                      (lastReps.Any(r => r > 0) || (exercise.TrackingType == TrackingType.WeightBased && last.WeightKg is not null));

        if (!hasData)
        {
            // First-ever session for this exercise: user types the starting values.
            return new Suggestion(WeightKg: null, TargetReps: eff.RepMin, SetCount: defaultSetCount);
        }

        var setCount = lastReps.Length > 0 ? lastReps.Length : defaultSetCount;

        if (exercise.TrackingType == TrackingType.WeightBased)
        {
            var w = last!.WeightKg ?? 0;
            w = last.Mark switch
            {
                Mark.Up => w + eff.WeightIncrementKg,
                Mark.Down => Math.Max(0, w - eff.WeightIncrementKg),
                _ => w,
            };
            return new Suggestion(WeightKg: w, TargetReps: null, SetCount: setCount);
        }

        // Rep-based: adjust the target reps instead of a weight.
        var r = CountingReps(lastReps, eff.Rule) ?? eff.RepMin;
        r = last!.Mark switch
        {
            Mark.Up => r + eff.RepIncrement,
            Mark.Down => Math.Max(1, r - eff.RepIncrement),
            _ => r,
        };
        return new Suggestion(WeightKg: null, TargetReps: r, SetCount: setCount);
    }
}
