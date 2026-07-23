using StackUp.Core.Enums;
using SQLite;

namespace StackUp.Core.Entities;

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

    /// <summary>Movement-glyph key, resolved to Resources/Images/icon_{key}.svg. See SeedData.IconKeys.</summary>
    public string IconKey { get; set; } = "dumbbell";

    public int? RepRangeMinOverride { get; set; }

    public int? RepRangeMaxOverride { get; set; }

    public double? WeightIncrementKgOverride { get; set; }

    public int? RepIncrementOverride { get; set; }

    public CountingSetRule? CountingSetRuleOverride { get; set; }

    /// <summary>Whether this exercise's chart is shown on the stats tab (all on by default).</summary>
    public bool IsVisibleInStats { get; set; } = true;

    /// <summary>Soft delete — history keeps referencing archived exercises.</summary>
    public bool IsArchived { get; set; }

    /// <summary>Stable seed identity (null = user-created). Idempotency key for preset seeding across languages.</summary>
    public string? PresetKey { get; set; }
}
