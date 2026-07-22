using System.Globalization;

namespace SimpleGymDiary.Core.Progression;

/// <summary>Serializes reps-per-set to/from the "15,12,9" TEXT column format.</summary>
public static class RepsSerializer
{
    public static int[] Parse(string? repsPerSet)
    {
        if (string.IsNullOrWhiteSpace(repsPerSet))
            return [];

        return repsPerSet
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(s => int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var r) ? r : 0)
            .ToArray();
    }

    public static string Serialize(IEnumerable<int> reps) =>
        string.Join(',', reps.Select(r => r.ToString(CultureInfo.InvariantCulture)));
}
