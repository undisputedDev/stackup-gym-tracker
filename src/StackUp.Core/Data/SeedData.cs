using System.Globalization;
using System.Resources;
using StackUp.Core.Entities;
using StackUp.Core.Enums;
using SQLite;

namespace StackUp.Core.Data;

/// <summary>
/// Preset library: the most common splits (Upper/Lower, Push/Pull/Legs, Full Body)
/// and their canonical exercises. Rows are seeded with the device-language name but
/// carry a stable <c>PresetKey</c>, so seeding stays idempotent across languages and
/// renames — names are user data the moment they hit the database.
/// </summary>
public static class SeedData
{
    /// <param name="Key">Stable identity, never shown to users.</param>
    /// <param name="Name">English name — the neutral fallback and the historical identity of pre-key rows.</param>
    public sealed record PresetExercise(string Key, string Name, TrackingType Type, string IconKey);

    public sealed record PresetSplit(string Key, string Name, string[] ExerciseKeys);

    private static readonly ResourceManager Names =
        new("StackUp.Core.Resources.PresetStrings", typeof(SeedData).Assembly);

    /// <summary>All available icon keys, in picker order.</summary>
    public static readonly string[] IconKeys =
        ["press", "pulldown", "row", "fly", "raise", "legs", "hip", "core", "dumbbell"];

    public static readonly PresetExercise[] Exercises =
    [
        new("lat_pulldown", "Lat Pulldown", TrackingType.WeightBased, "pulldown"),
        new("rowing", "Rowing", TrackingType.WeightBased, "row"),
        new("bench_press", "Bench Press", TrackingType.WeightBased, "press"),
        new("butterfly", "Butterfly", TrackingType.WeightBased, "fly"),
        new("lateral_raise", "Lateral Raise", TrackingType.WeightBased, "raise"),
        new("lower_back", "Lower Back", TrackingType.WeightBased, "hip"),
        new("leg_press", "Leg Press", TrackingType.WeightBased, "legs"),
        new("leg_extension", "Leg Extension", TrackingType.WeightBased, "legs"),
        new("hamstrings", "Hamstrings", TrackingType.WeightBased, "legs"),
        new("hip_thrust", "Hip Thrust", TrackingType.WeightBased, "hip"),
        new("shoulder_press", "Shoulder Press", TrackingType.WeightBased, "press"),
        new("incline_press", "Incline Press", TrackingType.WeightBased, "press"),
        new("triceps_pushdown", "Triceps Pushdown", TrackingType.WeightBased, "dumbbell"),
        new("biceps_curl", "Biceps Curl", TrackingType.WeightBased, "dumbbell"),
        new("face_pull", "Face Pull", TrackingType.WeightBased, "row"),
        new("squat", "Squat", TrackingType.WeightBased, "legs"),
        new("calf_raise", "Calf Raise", TrackingType.WeightBased, "legs"),
        new("pull_ups", "Pull-Ups", TrackingType.RepBased, "pulldown"),
        new("push_ups", "Push-Ups", TrackingType.RepBased, "press"),
        new("crunches", "Crunches", TrackingType.RepBased, "core"),
    ];

    public static readonly PresetSplit[] Splits =
    [
        new("upper_body", "Upper Body", ["lat_pulldown", "rowing", "bench_press", "butterfly", "lateral_raise"]),
        new("lower_body", "Lower Body", ["lower_back", "leg_press", "leg_extension", "hamstrings", "hip_thrust"]),
        new("push_day", "Push Day", ["bench_press", "shoulder_press", "incline_press", "butterfly", "lateral_raise", "triceps_pushdown"]),
        new("pull_day", "Pull Day", ["lat_pulldown", "rowing", "pull_ups", "face_pull", "biceps_curl"]),
        new("leg_day", "Leg Day", ["squat", "leg_press", "leg_extension", "hamstrings", "calf_raise", "hip_thrust"]),
        new("full_body", "Full Body", ["squat", "bench_press", "lat_pulldown", "shoulder_press", "rowing"]),
    ];

    internal static string LocalizedExerciseName(PresetExercise preset) =>
        Names.GetString($"Exercise_{preset.Key}", CultureInfo.CurrentUICulture) is { Length: > 0 } s ? s : preset.Name;

    internal static string LocalizedSplitName(PresetSplit preset) =>
        Names.GetString($"Split_{preset.Key}", CultureInfo.CurrentUICulture) is { Length: > 0 } s ? s : preset.Name;

    /// <summary>First-run seed: global settings plus the full preset library.</summary>
    public static async Task ApplyAsync(SQLiteAsyncConnection db)
    {
        await db.InsertOrReplaceAsync(new AppSettings());
        await EnsurePresetsAsync(db);
    }

    /// <summary>
    /// Inserts any preset exercise/split that doesn't exist yet. Existing rows are matched by
    /// PresetKey, English name, or localized name (including archived rows, so deleted presets
    /// stay deleted and no language change resurrects or duplicates them). Never modifies
    /// existing splits' exercise lists.
    /// </summary>
    public static async Task EnsurePresetsAsync(SQLiteAsyncConnection db)
    {
        // This also runs from migration v3 on old databases where the PresetKey
        // migration (v7) hasn't executed yet — make sure the columns exist.
        await AppDatabase.AddColumnIfMissingAsync(db, "Exercise", "PresetKey", "TEXT");
        await AppDatabase.AddColumnIfMissingAsync(db, "Split", "PresetKey", "TEXT");

        var allExercises = await db.Table<Exercise>().ToListAsync();
        foreach (var preset in Exercises)
        {
            var localized = LocalizedExerciseName(preset);
            if (allExercises.Any(e => e.PresetKey == preset.Key
                    || string.Equals(e.Name, preset.Name, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(e.Name, localized, StringComparison.OrdinalIgnoreCase)))
                continue;
            var exercise = new Exercise
            {
                Name = localized,
                PresetKey = preset.Key,
                TrackingType = preset.Type,
                IconKey = preset.IconKey,
            };
            await db.InsertAsync(exercise);
            allExercises.Add(exercise);
        }

        var allSplits = await db.Table<Split>().ToListAsync();
        var nextSortOrder = allSplits.Count == 0 ? 0 : allSplits.Max(s => s.SortOrder) + 1;
        foreach (var preset in Splits)
        {
            var localized = LocalizedSplitName(preset);
            if (allSplits.Any(s => s.PresetKey == preset.Key
                    || string.Equals(s.Name, preset.Name, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(s.Name, localized, StringComparison.OrdinalIgnoreCase)))
                continue;

            var split = new Split { Name = localized, PresetKey = preset.Key, SortOrder = nextSortOrder++ };
            await db.InsertAsync(split);

            for (var i = 0; i < preset.ExerciseKeys.Length; i++)
            {
                var key = preset.ExerciseKeys[i];
                var enName = Exercises.First(p => p.Key == key).Name;
                var exercise = allExercises.First(e => e.PresetKey == key
                    || string.Equals(e.Name, enName, StringComparison.OrdinalIgnoreCase));
                await db.InsertAsync(new SplitExercise { SplitId = split.Id, ExerciseId = exercise.Id, SortOrder = i });
            }
        }
    }
}
