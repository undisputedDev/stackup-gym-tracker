using StackUp.Core.Entities;
using StackUp.Core.Enums;

namespace StackUp.Core.Progression;

/// <summary>Per-exercise settings after resolving nullable overrides against global defaults.</summary>
public sealed record EffectiveExerciseSettings(
    int RepMin,
    int RepMax,
    double WeightIncrementKg,
    int RepIncrement,
    CountingSetRule Rule)
{
    /// <summary>Resolves an exercise's effective settings: override if set, else the global default.</summary>
    public static EffectiveExerciseSettings Resolve(Exercise exercise, AppSettings settings) => new(
        exercise.RepRangeMinOverride ?? settings.DefaultRepRangeMin,
        exercise.RepRangeMaxOverride ?? settings.DefaultRepRangeMax,
        exercise.WeightIncrementKgOverride ?? settings.DefaultWeightIncrementKg,
        exercise.RepIncrementOverride ?? settings.DefaultRepIncrement,
        exercise.CountingSetRuleOverride ?? settings.DefaultCountingSetRule);
}
