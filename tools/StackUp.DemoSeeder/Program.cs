// Fills a StackUp (Simple Gym Diary) database with realistic demo history:
// one Upper Body + one Lower Body session per week over ~3 months,
// driven through the real AppDatabase/ProgressionEngine flow so
// suggestions, markings and history stay consistent.
//
// Usage: dotnet run --project tools/StackUp.DemoSeeder [path-to-gymdiary.db3]
// Default path: the Windows head's AppData database. The DB file is
// deleted and recreated (seeded splits + demo sessions).

using System.Globalization;
using StackUp.Core.Data;
using StackUp.Core.Enums;
using StackUp.Core.Progression;

// Optional culture override so demo data can be seeded in a specific language
// (preset exercise/split names are localized). Usage: STACKUP_SEED_CULTURE=en or de.
if (Environment.GetEnvironmentVariable("STACKUP_SEED_CULTURE") is { Length: > 0 } cultureName)
{
    var culture = CultureInfo.GetCultureInfo(cultureName);
    CultureInfo.CurrentUICulture = culture;
    CultureInfo.CurrentCulture = culture;
}

var dbPath = args.Length > 0
    ? args[0]
    : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "User Name", "com.stackupgym.app", "Data", "gymdiary.db3");

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
// Match by PresetKey, not display name — split names are localized (e.g. "Oberkörper"
// on a de-DE machine), so matching the English name fails under German culture.
var upper = splits.First(s => s.PresetKey == "upper_body");
var lower = splits.First(s => s.PresetKey == "lower_body");

// Sensible starting working weights (kg) for a beginner-ish 3-month run.
// Keyed by PresetKey (culture-proof — exercise display names are localized).
var startWeights = new Dictionary<string, double>
{
    ["lat_pulldown"] = 50, ["rowing"] = 45, ["bench_press"] = 40,
    ["butterfly"] = 35, ["lateral_raise"] = 10,
    ["lower_back"] = 40, ["leg_press"] = 100, ["leg_extension"] = 40,
    ["hamstrings"] = 35, ["hip_thrust"] = 60,
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

var sessionCount = (await db.GetSessionsForSplitAsync(upper.Id)).Count
                 + (await db.GetSessionsForSplitAsync(lower.Id)).Count;
Console.WriteLine($"Done: {sessionCount} sessions seeded.");
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
        entry.WeightKg = entry.SuggestedWeightKg ?? startWeights[exercise.PresetKey!];

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
