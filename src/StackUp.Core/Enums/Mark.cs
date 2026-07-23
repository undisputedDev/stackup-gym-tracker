namespace StackUp.Core.Enums;

/// <summary>The per-exercise session marking that drives next-session progression.</summary>
public enum Mark
{
    /// <summary>Reps below range — reduce weight (or reps target) next session.</summary>
    Down = -1,

    /// <summary>Reps within range — keep the current value.</summary>
    Keep = 0,

    /// <summary>Reps above range — increase weight (or reps target) next session.</summary>
    Up = 1,
}
