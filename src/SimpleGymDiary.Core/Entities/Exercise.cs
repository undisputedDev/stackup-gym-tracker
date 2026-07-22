using SimpleGymDiary.Core.Enums;
using SQLite;

namespace SimpleGymDiary.Core.Entities;

/// <summary>
/// An exercise, reusable across splits. Override columns are nullable:
/// null means "inherit the global default from <see cref="AppSettings"/>".
/// </summary>
[Table("Exercise")]
public class Exercise
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    public string Name { get; set; } = "";

    public TrackingType TrackingType { get; set; } = TrackingType.WeightBased;

    public int? RepRangeMinOverride { get; set; }

    public int? RepRangeMaxOverride { get; set; }

    public double? WeightIncrementKgOverride { get; set; }

    public int? RepIncrementOverride { get; set; }

    public CountingSetRule? CountingSetRuleOverride { get; set; }

    /// <summary>Soft delete — history keeps referencing archived exercises.</summary>
    public bool IsArchived { get; set; }
}
