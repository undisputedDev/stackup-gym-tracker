namespace StackUp.Core.Progression;

/// <summary>Suggested starting values for an exercise in a new session.</summary>
/// <param name="WeightKg">Suggested working weight; null for rep-based exercises or the first-ever session.</param>
/// <param name="TargetReps">Suggested target reps; set for rep-based exercises (and as the rep goal on a first session).</param>
/// <param name="SetCount">How many rep chips to prefill (same count as last time, or the global default).</param>
public sealed record Suggestion(double? WeightKg, int? TargetReps, int SetCount);
