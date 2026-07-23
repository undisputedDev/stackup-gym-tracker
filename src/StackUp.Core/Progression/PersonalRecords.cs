using StackUp.Core.Data;

namespace StackUp.Core.Progression;

/// <summary>
/// All-time bests from exercise history. Only performed entries count —
/// StartSessionAsync prefills WeightKg with the suggestion, so a weight
/// without any logged reps was never actually lifted.
/// </summary>
public static class PersonalRecords
{
    /// <summary>Heaviest performed weight, or null when nothing was ever performed.</summary>
    public static double? BestWeight(IEnumerable<ExerciseHistoryPoint> points) =>
        points.Where(p => p.WeightKg is not null && RepsSerializer.Parse(p.RepsPerSet).Any(r => r > 0))
              .Max(p => p.WeightKg); // Max over an empty nullable sequence -> null

    /// <summary>Most reps in a single set across all history, or null when nothing was logged.</summary>
    public static int? BestSetReps(IEnumerable<ExerciseHistoryPoint> points) =>
        points.SelectMany(p => RepsSerializer.Parse(p.RepsPerSet))
              .Where(r => r > 0)
              .Max(r => (int?)r);
}
