// Fills a Simple Gym Diary database with realistic demo history:
// one Upper Body + one Lower Body session per week over ~3 months,
// driven through the real AppDatabase/ProgressionEngine flow so
// suggestions, markings and history stay consistent.
//
// Usage: dotnet run --project tools/SimpleGymDiary.DemoSeeder [path-to-gymdiary.db3]
// Default path: the Windows head's AppData database. The DB file is
// deleted and recreated (seeded splits + demo sessions).

using SimpleGymDiary.Core.Data;
using SimpleGymDiary.Core.Enums;
using SimpleGymDiary.Core.Progression;

var dbPath = args.Length > 0
    ? args[0]
    : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "User Name", "com.simplegymdiary.app", "Data", "gymdiary.db3");

Console.WriteLine($"Target DB: {dbPath}");
Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);
if (File.Exists(dbPath))
{
    File.Delete(dbPath);
    Console.WriteLine("Existing DB deleted.");
}

var db = new AppDatabase(dbPath);
await db.InitializeAsync();
var settings = await db.GetSettingsAsync();
var splits = await db.GetSplitsAsync();
var upper = splits.First(s => s.Name == "Upper Body");
var lower = splits.First(s => s.Name == "Lower Body");

// Sensible starting working weights (kg) for a beginner-ish 3-month run.
var startWeights = new Dictionary<string, double>
{
    ["Lat Pulldown"] = 50, ["Rowing"] = 45, ["Bench Press"] = 40,
    ["Butterfly"] = 35, ["Lateral Raise"] = 10,
    ["Lower Back"] = 40, ["Leg Press"] = 100, ["Leg Extension"] = 40,
    ["Hamstrings"] = 35, ["Hip Thrust"] = 60,
};

// Per-exercise simulated "first set reps" state.
var firstSetReps = new Dictionary<int, int>();
var rng = new Random(42); // deterministic

const int weeks = 13;
// Anchor on the most recent Monday/Thursday in the past; walk backwards.
var lastUpper = new DateTime(2026, 7, 20, 15, 30, 0, DateTimeKind.Utc);  // Monday
var lastLower = new DateTime(2026, 7, 16, 15, 30, 0, DateTimeKind.Utc);  // Thursday

for (var w = weeks - 1; w >= 0; w--)
{
    await SimulateSessionAsync(upper.Id, lastUpper.AddDays(-7 * w));
    await SimulateSessionAsync(lower.Id, lastLower.AddDays(-7 * w));
}

var rows = await db.GetExportRowsAsync();
Console.WriteLine($"Done: {weeks * 2} sessions, {rows.Count} exercise entries.");
await db.CloseAsync();
return;

async Task SimulateSessionAsync(int splitId, DateTime startUtc)
{
    var session = await db.StartSessionAsync(splitId, startUtc);
    var entries = await db.GetSessionEntriesAsync(session.Id);

    foreach (var entry in entries)
    {
        var exercise = (await db.GetExerciseAsync(entry.ExerciseId))!;
        var eff = EffectiveExerciseSettings.Resolve(exercise, settings);

        // Weight: engine suggestion if available, else the starting weight.
        entry.WeightKg = entry.SuggestedWeightKg ?? startWeights[exercise.Name];

        // First-set reps: continue from last time, +1..2 per week of adaptation.
        if (!firstSetReps.TryGetValue(exercise.Id, out var first))
            first = eff.RepMin + rng.Next(0, 3);              // 10..12 on day one
        else
            first = Math.Min(first + rng.Next(1, 3), eff.RepMax + 2);

        // Occasionally a bad day (poor sleep, stress): below range -> Down arrow.
        var badDay = rng.NextDouble() < 0.08;
        if (badDay)
            first = eff.RepMin - rng.Next(1, 3);              // 8..9

        // Later sets fatigue: each set loses 1-3 reps.
        var set2 = Math.Max(1, first - rng.Next(1, 3));
        var set3 = Math.Max(1, set2 - rng.Next(1, 3));
        entry.RepsPerSet = RepsSerializer.Serialize([first, set2, set3]);

        ProgressionEngine.ApplyAutoMark(entry, eff);
        await db.SaveEntryAsync(entry);

        // Update simulated state for next week based on the resulting mark:
        // weight went up -> reps drop back near the range bottom;
        // weight went down -> the lighter weight feels easy again.
        firstSetReps[exercise.Id] = entry.Mark switch
        {
            Mark.Up => eff.RepMin + rng.Next(0, 2),
            Mark.Down => eff.RepMin + rng.Next(1, 3),
            _ => first,
        };
    }

    await db.CompleteSessionAsync(session.Id, startUtc.AddMinutes(50 + rng.Next(0, 25)));
}
