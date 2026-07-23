using StackUp.Core.Enums;
using SQLite;

namespace StackUp.Core.Entities;

/// <summary>Global defaults. Single row with <see cref="Id"/> = 1.</summary>
[Table("AppSettings")]
public class AppSettings
{
    public const int SingletonId = 1;

    [PrimaryKey]
    public int Id { get; set; } = SingletonId;

    public WeightUnit Unit { get; set; } = WeightUnit.Kg;

    public int DefaultRepRangeMin { get; set; } = 10;

    public int DefaultRepRangeMax { get; set; } = 15;

    public double DefaultWeightIncrementKg { get; set; } = 2.5;

    public int DefaultRepIncrement { get; set; } = 1;

    public CountingSetRule DefaultCountingSetRule { get; set; } = CountingSetRule.FirstSet;

    public int DefaultSetCount { get; set; } = 3;

    /// <summary>How often the store review dialog has been requested (lifetime cap applies).</summary>
    public int ReviewRequestCount { get; set; }

    public DateTime? LastReviewRequestUtc { get; set; }
}
