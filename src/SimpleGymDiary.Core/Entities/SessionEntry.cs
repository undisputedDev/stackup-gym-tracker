using SimpleGymDiary.Core.Enums;
using SQLite;

namespace SimpleGymDiary.Core.Entities;

/// <summary>
/// One exercise within a session. Suggested* columns are snapshots taken at session start,
/// so history shows what was suggested vs. what was done and later settings changes
/// never rewrite history.
/// </summary>
[Table("SessionEntry")]
public class SessionEntry
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [Indexed]
    public int SessionId { get; set; }

    [Indexed]
    public int ExerciseId { get; set; }

    public int OrderInSession { get; set; }

    /// <summary>Suggested working weight (weight-based exercises); null on first-ever session.</summary>
    public double? SuggestedWeightKg { get; set; }

    /// <summary>Actual working weight entered by the user (weight-based exercises).</summary>
    public double? WeightKg { get; set; }

    /// <summary>Suggested target reps (rep-based exercises); null for weight-based.</summary>
    public int? SuggestedReps { get; set; }

    /// <summary>Reps achieved per set, serialized as "15,12,9". Empty string = nothing logged yet.</summary>
    public string RepsPerSet { get; set; } = "";

    /// <summary>Marking computed from the reps + effective rep range.</summary>
    public Mark AutoMark { get; set; } = Mark.Keep;

    /// <summary>Effective marking; equals <see cref="AutoMark"/> unless the user overrode it.</summary>
    public Mark Mark { get; set; } = Mark.Keep;

    /// <summary>True once the user manually overrode the marking; the override then survives recomputation.</summary>
    public bool MarkIsManual { get; set; }
}
