namespace SimpleGymDiary.Core.Enums;

/// <summary>How progress is measured for an exercise.</summary>
public enum TrackingType
{
    /// <summary>Working weight is tracked (machines, barbells). Reps decide the marking; progression adjusts the weight.</summary>
    WeightBased = 0,

    /// <summary>Bodyweight exercise (e.g. pull-ups). Reps are tracked; progression adjusts the target reps.</summary>
    RepBased = 1,
}
