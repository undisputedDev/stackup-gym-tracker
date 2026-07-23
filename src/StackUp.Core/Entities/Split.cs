using SQLite;

namespace StackUp.Core.Entities;

/// <summary>A training day, e.g. "Upper Body". Contains an ordered list of exercises via <see cref="SplitExercise"/>.</summary>
[Table("Split")]
public class Split
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    public string Name { get; set; } = "";

    public int SortOrder { get; set; }

    /// <summary>Soft delete — completed sessions keep referencing archived splits.</summary>
    public bool IsArchived { get; set; }
}
