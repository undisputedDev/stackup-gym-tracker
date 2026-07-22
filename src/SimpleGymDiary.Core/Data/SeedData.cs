using SimpleGymDiary.Core.Entities;
using SQLite;

namespace SimpleGymDiary.Core.Data;

/// <summary>First-run seed: global settings plus the Upper/Lower 2-day split preset.</summary>
public static class SeedData
{
    public static async Task ApplyAsync(SQLiteAsyncConnection db)
    {
        await db.InsertOrReplaceAsync(new AppSettings());

        var upper = new Split { Name = "Upper Body", SortOrder = 0 };
        var lower = new Split { Name = "Lower Body", SortOrder = 1 };
        await db.InsertAsync(upper);
        await db.InsertAsync(lower);

        string[] upperExercises = ["Lat Pulldown", "Rowing", "Bench Press", "Butterfly", "Lateral Raise"];
        string[] lowerExercises = ["Lower Back", "Leg Press", "Leg Extension", "Hamstrings", "Hip Thrust"];

        await SeedSplitAsync(db, upper.Id, upperExercises);
        await SeedSplitAsync(db, lower.Id, lowerExercises);
    }

    private static async Task SeedSplitAsync(SQLiteAsyncConnection db, int splitId, string[] exerciseNames)
    {
        for (var i = 0; i < exerciseNames.Length; i++)
        {
            var exercise = new Exercise { Name = exerciseNames[i] };
            await db.InsertAsync(exercise);
            await db.InsertAsync(new SplitExercise { SplitId = splitId, ExerciseId = exercise.Id, SortOrder = i });
        }
    }
}
