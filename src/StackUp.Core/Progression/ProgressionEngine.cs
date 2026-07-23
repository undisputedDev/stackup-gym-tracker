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

    /// <summary>After this many consecutive ▼ sessions a deload reset replaces the plain decrement.</summary>
    public const int DeloadStreakLength = 3;

    /// <summary>Deload target: last weight × this factor, rounded to the exercise's increment.</summary>
    public const double DeloadFactor = 0.9;

    /// <summary>Rounds to the nearest multiple of the increment (midpoint away from zero); increment ≤ 0 passes through.</summary>
    public static double RoundToIncrement(double kg, double increment) =>
        increment > 0
            ? Math.Round(Math.Round(kg / increment, MidpointRounding.AwayFromZero) * increment, 3)
            : kg;

    /// <summary>
    /// Suggests next-session values from the recent completed entries for the exercise
    /// (exercise-scoped: the same exercise in two splits shares its progression).
    /// </summary>
    /// <param name="history">Recent completed entries, newest first; [0] is the entry progression is computed from.</param>
    public static Suggestion SuggestNext(Exercise exercise, EffectiveExerciseSettings eff,
        IReadOnlyList<SessionEntry> history, int defaultSetCount)
    {
        var last = history.Count > 0 ? history[0] : null;
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

            // Three ▼ in a row: another −increment clearly isn't enough — suggest a real
            // reset instead. Effective marks drive the streak, so manual overrides count;
            // a skipped session defaults to Keep and (intentionally) breaks it.
            if (last.Mark == Mark.Down && history.Count >= DeloadStreakLength
                && history.Take(DeloadStreakLength).All(e => e.Mark == Mark.Down))
            {
                var deload = Math.Max(0, Math.Min(RoundToIncrement(w * DeloadFactor, eff.WeightIncrementKg),
                                                  w - eff.WeightIncrementKg)); // never weaker than a normal ▼ step
                return new Suggestion(WeightKg: deload, TargetReps: null, SetCount: setCount, IsDeload: true);
            }

            w = last.Mark switch
            {
                Mark.Up => w + eff.WeightIncrementKg,
                Mark.Down => Math.Max(0, w - eff.WeightIncrementKg),
                _ => w,
            };
            return new Suggestion(WeightKg: w, TargetReps: null, SetCount: setCount);
        }

        // Rep-based: adjust the target reps instead of a weight. No deload — the decline
        // is already gentle (×0.9 of a typical rep count ≈ one increment anyway).
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
