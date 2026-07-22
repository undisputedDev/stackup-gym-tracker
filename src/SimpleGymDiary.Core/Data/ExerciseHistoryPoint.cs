using SimpleGymDiary.Core.Enums;

namespace SimpleGymDiary.Core.Data;

/// <summary>One data point for the stats chart: an exercise's result in a completed session.</summary>
public class ExerciseHistoryPoint
{
    public DateTime StartedAtUtc { get; set; }
    public double? WeightKg { get; set; }
    public string RepsPerSet { get; set; } = "";
    public Mark Mark { get; set; }
}
