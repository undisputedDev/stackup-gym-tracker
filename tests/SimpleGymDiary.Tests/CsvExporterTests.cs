using System.Globalization;
using SimpleGymDiary.Core.Data;
using SimpleGymDiary.Core.Enums;
using SimpleGymDiary.Core.Export;

namespace SimpleGymDiary.Tests;

public class CsvExporterTests
{
    [Fact]
    public void Export_OneRowPerSet_WithHeader()
    {
        var csv = CsvExporter.Export(
        [
            new ExportRow
            {
                StartedAtUtc = new DateTime(2026, 7, 20, 17, 30, 0, DateTimeKind.Utc),
                SplitName = "Upper Body",
                ExerciseName = "Lat Pulldown",
                TrackingType = TrackingType.WeightBased,
                WeightKg = 60,
                RepsPerSet = "15,12,9",
                Mark = Mark.Keep,
            },
        ]);

        var lines = csv.TrimEnd().Split(Environment.NewLine);
        Assert.Equal(4, lines.Length); // header + 3 sets
        Assert.Equal(CsvExporter.Header, lines[0]);
        Assert.Equal("2026-07-20,Upper Body,Lat Pulldown,WeightBased,60,1,15,Keep", lines[1]);
        Assert.Equal("2026-07-20,Upper Body,Lat Pulldown,WeightBased,60,3,9,Keep", lines[3]);
    }

    [Fact]
    public void Export_UsesInvariantDecimalSeparator_RegardlessOfCulture()
    {
        var original = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("de-DE"); // decimal comma culture
            var csv = CsvExporter.Export(
            [
                new ExportRow { StartedAtUtc = DateTime.UnixEpoch, SplitName = "A", ExerciseName = "B", WeightKg = 57.5, RepsPerSet = "10" },
            ]);
            Assert.Contains("57.5", csv);
            Assert.DoesNotContain("57,5", csv);
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }

    [Fact]
    public void Export_QuotesFieldsContainingCommasAndQuotes()
    {
        var csv = CsvExporter.Export(
        [
            new ExportRow { StartedAtUtc = DateTime.UnixEpoch, SplitName = "Push, Pull", ExerciseName = "The \"Big\" Lift", RepsPerSet = "5" },
        ]);
        Assert.Contains("\"Push, Pull\"", csv);
        Assert.Contains("\"The \"\"Big\"\" Lift\"", csv);
    }

    [Fact]
    public void Export_EmptyReps_StillEmitsOneRow()
    {
        var csv = CsvExporter.Export(
        [
            new ExportRow { StartedAtUtc = DateTime.UnixEpoch, SplitName = "A", ExerciseName = "B", WeightKg = 40, RepsPerSet = "" },
        ]);
        var lines = csv.TrimEnd().Split(Environment.NewLine);
        Assert.Equal(2, lines.Length);
        Assert.EndsWith("40,1,,Keep", lines[1]);
    }
}
