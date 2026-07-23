using StackUp.Core.Entities;
using StackUp.Core.Enums;
using SQLite;

namespace StackUp.Core.Data;

/// <summary>
/// Preset library: the most common splits (Upper/Lower, Push/Pull/Legs, Full Body)
/// and their canonical exercises. Seeding is idempotent by name, so it runs on both
/// fresh installs and upgrade migrations without duplicating user data.
/// </summary>
public static class SeedData
{
    public sealed record PresetExercise(string Name, TrackingType Type, string IconKey);

    public sealed record PresetSplit(string Name, string[] ExerciseNames);

    /// <summary>All available icon keys, in picker order.</summary>
    public static readonly string[] IconKeys =
        ["press", "pulldown", "row", "fly", "raise", "legs", "hip", "core", "dumbbell"];

    public static readonly PresetExercise[] Exercises =
    [
        new("Lat Pulldown", TrackingType.WeightBased, "pulldown"),
        new("Rowing", TrackingType.WeightBased, "row"),
        new("Bench Press", TrackingType.WeightBased, "press"),
        new("Butterfly", TrackingType.WeightBased, "fly"),
        new("Lateral Raise", TrackingType.WeightBased, "raise"),
        new("Lower Back", TrackingType.WeightBased, "hip"),
        new("Leg Press", TrackingType.WeightBased, "legs"),
        new("Leg Extension", TrackingType.WeightBased, "legs"),
        new("Hamstrings", TrackingType.WeightBased, "legs"),
        new("Hip Thrust", TrackingType.WeightBased, "hip"),
        new("Shoulder Press", TrackingType.WeightBased, "press"),
        new("Incline Press", TrackingType.WeightBased, "press"),
        new("Triceps Pushdown", TrackingType.WeightBased, "dumbbell"),
        new("Biceps Curl", TrackingType.WeightBased, "dumbbell"),
        new("Face Pull", TrackingType.WeightBased, "row"),
        new("Squat", TrackingType.WeightBased, "legs"),
        new("Calf Raise", TrackingType.WeightBased, "legs"),
        new("Pull-Ups", TrackingType.RepBased, "pulldown"),
        new("Push-Ups", TrackingType.RepBased, "press"),
        new("Crunches", TrackingType.RepBased, "core"),
    ];

    public static readonly PresetSplit[] Splits =
    [
        new("Upper Body", ["Lat Pulldown", "Rowing", "Bench Press", "Butterfly", "Lateral Raise"]),
        new("Lower Body", ["Lower Back", "Leg Press", "Leg Extension", "Hamstrings", "Hip Thrust"]),
        new("Push Day", ["Bench Press", "Shoulder Press", "Incline Press", "Butterfly", "Lateral Raise", "Triceps Pushdown"]),
        new("Pull Day", ["Lat Pulldown", "Rowing", "Pull-Ups", "Face Pull", "Biceps Curl"]),
        new("Leg Day", ["Squat", "Leg Press", "Leg Extension", "Hamstrings", "Calf Raise", "Hip Thrust"]),
        new("Full Body", ["Squat", "Bench Press", "Lat Pulldown", "Shoulder Press", "Rowing"]),
    ];

    /// <summary>First-run seed: global settings plus the full preset library.</summary>
    public static async Task ApplyAsync(SQLiteAsyncConnection db)
    {
        await db.InsertOrReplaceAsync(new AppSettings());
        await EnsurePresetsAsync(db);
    }

    /// <summary>
    /// Inserts any preset exercise/split that doesn't exist yet (matched by name,
    /// including archived rows so deleted presets stay deleted). Never modifies
    /// existing splits' exercise lists.
    /// </summary>
    public static async Task EnsurePresetsAsync(SQLiteAsyncConnection db)
    {
        var allExercises = await db.Table<Exercise>().ToListAsync();
        foreach (var preset in Exercises)
        {
            if (allExercises.Any(e => string.Equals(e.Name, preset.Name, StringComparison.OrdinalIgnoreCase)))
                continue;
            var exercise = new Exercise { Name = preset.Name, TrackingType = preset.Type, IconKey = preset.IconKey };
            await db.InsertAsync(exercise);
            allExercises.Add(exercise);
        }

        var allSplits = await db.Table<Split>().ToListAsync();
        var nextSortOrder = allSplits.Count == 0 ? 0 : allSplits.Max(s => s.SortOrder) + 1;
        foreach (var preset in Splits)
        {
            if (allSplits.Any(s => string.Equals(s.Name, preset.Name, StringComparison.OrdinalIgnoreCase)))
                continue;

            var split = new Split { Name = preset.Name, SortOrder = nextSortOrder++ };
            await db.InsertAsync(split);

            for (var i = 0; i < preset.ExerciseNames.Length; i++)
            {
                var exercise = allExercises.First(e =>
                    string.Equals(e.Name, preset.ExerciseNames[i], StringComparison.OrdinalIgnoreCase));
                await db.InsertAsync(new SplitExercise { SplitId = split.Id, ExerciseId = exercise.Id, SortOrder = i });
            }
        }
    }
}
