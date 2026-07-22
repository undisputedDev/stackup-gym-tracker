using System.Globalization;
using System.Text;
using SimpleGymDiary.Core.Data;
using SimpleGymDiary.Core.Progression;

namespace SimpleGymDiary.Core.Export;

/// <summary>
/// Exports history as CSV, one row per set. Numbers and dates always use
/// InvariantCulture so the file opens identically everywhere.
/// </summary>
public static class CsvExporter
{
    public const string Header = "Date,Split,Exercise,TrackingType,WeightKg,SetNumber,Reps,Mark";

    public static string Export(IEnumerable<ExportRow> rows)
    {
        var sb = new StringBuilder();
        sb.AppendLine(Header);

        foreach (var row in rows)
        {
            var reps = RepsSerializer.Parse(row.RepsPerSet);
            var date = row.StartedAtUtc.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            var weight = row.WeightKg?.ToString(CultureInfo.InvariantCulture) ?? "";

            if (reps.Length == 0)
            {
                AppendRow(sb, date, row, weight, setNumber: 1, reps: "");
                continue;
            }

            for (var i = 0; i < reps.Length; i++)
                AppendRow(sb, date, row, weight, setNumber: i + 1, reps: reps[i].ToString(CultureInfo.InvariantCulture));
        }

        return sb.ToString();
    }

    private static void AppendRow(StringBuilder sb, string date, ExportRow row, string weight, int setNumber, string reps)
    {
        sb.Append(date).Append(',')
          .Append(Quote(row.SplitName)).Append(',')
          .Append(Quote(row.ExerciseName)).Append(',')
          .Append(row.TrackingType).Append(',')
          .Append(weight).Append(',')
          .Append(setNumber).Append(',')
          .Append(reps).Append(',')
          .Append(row.Mark)
          .AppendLine();
    }

    /// <summary>Quotes a field if it contains a comma, quote, or newline (doubling embedded quotes).</summary>
    private static string Quote(string field)
    {
        if (field.IndexOfAny(['"', ',', '\n', '\r']) < 0)
            return field;
        return $"\"{field.Replace("\"", "\"\"")}\"";
    }
}
