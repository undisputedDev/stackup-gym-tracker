using SQLite;

namespace StackUp.Core.Entities;

/// <summary>Join table: which exercises belong to a split, in which order.</summary>
[Table("SplitExercise")]
public class SplitExercise
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [Indexed]
    public int SplitId { get; set; }

    [Indexed]
    public int ExerciseId { get; set; }

    public int SortOrder { get; set; }
}
