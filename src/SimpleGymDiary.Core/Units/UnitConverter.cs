using System.Globalization;
using SimpleGymDiary.Core.Enums;

namespace SimpleGymDiary.Core.Units;

/// <summary>kg &lt;-&gt; lbs conversion. Storage is always kg; lbs exists only at the display layer.</summary>
public static class UnitConverter
{
    public const double LbsPerKg = 2.2046226218;

    public static double KgToDisplay(double kg, WeightUnit unit) =>
        unit == WeightUnit.Lbs ? kg * LbsPerKg : kg;

    public static double DisplayToKg(double value, WeightUnit unit) =>
        unit == WeightUnit.Lbs ? value / LbsPerKg : value;

    /// <summary>Formats a kg value in the display unit, trimming trailing zeros (e.g. "60", "57.5").</summary>
    public static string Format(double kg, WeightUnit unit) =>
        Math.Round(KgToDisplay(kg, unit), 2).ToString("0.##");

    public static string UnitLabel(WeightUnit unit) => unit == WeightUnit.Lbs ? "lbs" : "kg";

    /// <summary>
    /// Culture-tolerant decimal parse: accepts both "57,5" (decimal-comma keyboards)
    /// and "57.5". Never use raw double.Parse on user input.
    /// </summary>
    public static bool TryParseFlexible(string? text, out double value)
    {
        value = 0;
        if (string.IsNullOrWhiteSpace(text))
            return false;
        return double.TryParse(text.Trim().Replace(',', '.'),
            NumberStyles.Float, CultureInfo.InvariantCulture, out value);
    }
}
