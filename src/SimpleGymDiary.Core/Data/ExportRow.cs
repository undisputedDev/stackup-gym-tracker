using SimpleGymDiary.Core.Enums;

namespace SimpleGymDiary.Core.Data;

/// <summary>Flat row for CSV export (one row per session entry; sets are expanded by the exporter).</summary>
public class ExportRow
{
    public DateTime StartedAtUtc { get; set; }
    public string SplitName { get; set; } = "";
    public string ExerciseName { get; set; } = "";
    public TrackingType TrackingType { get; set; }
    public double? WeightKg { get; set; }
    public string RepsPerSet { get; set; } = "";
    public Mark Mark { get; set; }
}
