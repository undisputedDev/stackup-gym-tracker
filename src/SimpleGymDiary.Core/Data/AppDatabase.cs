using SimpleGymDiary.Core.Entities;
using SimpleGymDiary.Core.Progression;
using SQLite;

namespace SimpleGymDiary.Core.Data;

/// <summary>
/// Single async SQLite connection (register as singleton). Schema is versioned via
/// PRAGMA user_version and an ordered list of migrations run on <see cref="InitializeAsync"/>.
/// </summary>
public class AppDatabase
{
    private readonly SQLiteAsyncConnection _db;
    private bool _initialized;

    public AppDatabase(string databasePath)
    {
        SQLitePCL.Batteries_V2.Init();
        _db = new SQLiteAsyncConnection(databasePath,
            SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create | SQLiteOpenFlags.SharedCache);
    }

    /// <summary>Ordered migrations; index + 1 is the resulting user_version.</summary>
    private static readonly Func<SQLiteAsyncConnection, Task>[] Migrations =
    [
        // v1: initial schema + seed data
        async db =>
        {
            await db.CreateTableAsync<AppSettings>();
            await db.CreateTableAsync<Split>();
            await db.CreateTableAsync<Exercise>();
            await db.CreateTableAsync<SplitExercise>();
            await db.CreateTableAsync<Session>();
            await db.CreateTableAsync<SessionEntry>();
            await SeedData.ApplyAsync(db);
        },
    ];

    public async Task InitializeAsync()
    {
        if (_initialized)
            return;

        var version = await _db.ExecuteScalarAsync<int>("PRAGMA user_version");
        while (version < Migrations.Length)
        {
            await Migrations[version](_db);
            version++;
            await _db.ExecuteAsync($"PRAGMA user_version = {version}");
        }

        _initialized = true;
    }

    /// <summary>Closes the underlying connection (releases the file lock). Used by tests and app shutdown.</summary>
    public Task CloseAsync() => _db.CloseAsync();

    // ---- Settings ----

    public async Task<AppSettings> GetSettingsAsync() =>
        await _db.FindAsync<AppSettings>(AppSettings.SingletonId) ?? new AppSettings();

    public Task SaveSettingsAsync(AppSettings settings)
    {
        settings.Id = AppSettings.SingletonId;
        return _db.InsertOrReplaceAsync(settings);
    }

    // ---- Splits ----

    public Task<List<Split>> GetSplitsAsync() =>
        _db.Table<Split>().Where(s => !s.IsArchived).OrderBy(s => s.SortOrder).ToListAsync();

    public Task<Split?> GetSplitAsync(int id) => _db.FindAsync<Split>(id)!;

    public async Task SaveSplitAsync(Split split)
    {
        if (split.Id == 0)
            await _db.InsertAsync(split);
        else
            await _db.UpdateAsync(split);
    }

    public async Task ArchiveSplitAsync(int id)
    {
        var split = await _db.FindAsync<Split>(id);
        if (split is null)
            return;
        split.IsArchived = true;
        await _db.UpdateAsync(split);
    }

    // ---- Exercises ----

    public Task<List<Exercise>> GetExercisesAsync() =>
        _db.Table<Exercise>().Where(e => !e.IsArchived).OrderBy(e => e.Name).ToListAsync();

    public Task<Exercise?> GetExerciseAsync(int id) => _db.FindAsync<Exercise>(id)!;

    public async Task SaveExerciseAsync(Exercise exercise)
    {
        if (exercise.Id == 0)
            await _db.InsertAsync(exercise);
        else
            await _db.UpdateAsync(exercise);
    }

    public async Task ArchiveExerciseAsync(int id)
    {
        var exercise = await _db.FindAsync<Exercise>(id);
        if (exercise is null)
            return;
        exercise.IsArchived = true;
        await _db.UpdateAsync(exercise);
    }

    // ---- Split composition ----

    /// <summary>Exercises of a split in display order.</summary>
    public Task<List<Exercise>> GetSplitExercisesAsync(int splitId) =>
        _db.QueryAsync<Exercise>(
            """
            SELECT e.* FROM Exercise e
            JOIN SplitExercise se ON se.ExerciseId = e.Id
            WHERE se.SplitId = ? AND e.IsArchived = 0
            ORDER BY se.SortOrder
            """, splitId);

    /// <summary>Replaces a split's exercise list with the given ordered ids.</summary>
    public async Task SetSplitExercisesAsync(int splitId, IReadOnlyList<int> exerciseIdsInOrder)
    {
        await _db.ExecuteAsync("DELETE FROM SplitExercise WHERE SplitId = ?", splitId);
        for (var i = 0; i < exerciseIdsInOrder.Count; i++)
        {
            await _db.InsertAsync(new SplitExercise
            {
                SplitId = splitId,
                ExerciseId = exerciseIdsInOrder[i],
                SortOrder = i,
            });
        }
    }

    // ---- Sessions ----

    public Task<Session?> GetSessionAsync(int id) => _db.FindAsync<Session>(id)!;

    public async Task<Session?> GetInProgressSessionAsync() =>
        (await _db.Table<Session>().Where(s => s.CompletedAtUtc == null)
            .OrderByDescending(s => s.StartedAtUtc).ToListAsync())
        .FirstOrDefault();

    public async Task<Session?> GetLastCompletedSessionForSplitAsync(int splitId) =>
        (await _db.Table<Session>().Where(s => s.SplitId == splitId && s.CompletedAtUtc != null)
            .OrderByDescending(s => s.StartedAtUtc).ToListAsync())
        .FirstOrDefault();

    /// <summary>Most recent completed entry for an exercise (exercise-scoped progression source).</summary>
    public async Task<SessionEntry?> GetLastCompletedEntryForExerciseAsync(int exerciseId) =>
        (await _db.QueryAsync<SessionEntry>(
            """
            SELECT se.* FROM SessionEntry se
            JOIN Session s ON s.Id = se.SessionId
            WHERE se.ExerciseId = ? AND s.CompletedAtUtc IS NOT NULL
            ORDER BY s.StartedAtUtc DESC
            LIMIT 1
            """, exerciseId))
        .FirstOrDefault();

    /// <summary>
    /// Creates a session for a split with one entry per exercise, snapshotting
    /// next-session suggestions from each exercise's last completed entry.
    /// </summary>
    public async Task<Session> StartSessionAsync(int splitId, DateTime nowUtc)
    {
        var settings = await GetSettingsAsync();
        var exercises = await GetSplitExercisesAsync(splitId);

        var session = new Session { SplitId = splitId, StartedAtUtc = nowUtc };
        await _db.InsertAsync(session);

        for (var i = 0; i < exercises.Count; i++)
        {
            var exercise = exercises[i];
            var eff = EffectiveExerciseSettings.Resolve(exercise, settings);
            var last = await GetLastCompletedEntryForExerciseAsync(exercise.Id);
            var suggestion = ProgressionEngine.SuggestNext(exercise, eff, last, settings.DefaultSetCount);

            await _db.InsertAsync(new SessionEntry
            {
                SessionId = session.Id,
                ExerciseId = exercise.Id,
                OrderInSession = i,
                SuggestedWeightKg = suggestion.WeightKg,
                WeightKg = suggestion.WeightKg,
                SuggestedReps = suggestion.TargetReps,
                RepsPerSet = RepsSerializer.Serialize(Enumerable.Repeat(0, suggestion.SetCount)),
            });
        }

        return session;
    }

    public Task<List<SessionEntry>> GetSessionEntriesAsync(int sessionId) =>
        _db.Table<SessionEntry>().Where(e => e.SessionId == sessionId)
            .OrderBy(e => e.OrderInSession).ToListAsync();

    public Task SaveEntryAsync(SessionEntry entry) => _db.UpdateAsync(entry);

    public async Task CompleteSessionAsync(int sessionId, DateTime nowUtc)
    {
        var session = await _db.FindAsync<Session>(sessionId);
        if (session is null)
            return;
        session.CompletedAtUtc = nowUtc;
        await _db.UpdateAsync(session);
    }

    public async Task DeleteSessionAsync(int sessionId)
    {
        await _db.ExecuteAsync("DELETE FROM SessionEntry WHERE SessionId = ?", sessionId);
        await _db.ExecuteAsync("DELETE FROM Session WHERE Id = ?", sessionId);
    }

    // ---- Stats & export ----

    public Task<List<ExerciseHistoryPoint>> GetExerciseHistoryAsync(int exerciseId, DateTime? sinceUtc = null) =>
        _db.QueryAsync<ExerciseHistoryPoint>(
            """
            SELECT s.StartedAtUtc, se.WeightKg, se.RepsPerSet, se.Mark
            FROM SessionEntry se
            JOIN Session s ON s.Id = se.SessionId
            WHERE se.ExerciseId = ? AND s.CompletedAtUtc IS NOT NULL AND s.StartedAtUtc >= ?
            ORDER BY s.StartedAtUtc
            """, exerciseId, sinceUtc ?? DateTime.MinValue);

    public Task<List<ExportRow>> GetExportRowsAsync() =>
        _db.QueryAsync<ExportRow>(
            """
            SELECT s.StartedAtUtc, sp.Name AS SplitName, e.Name AS ExerciseName,
                   e.TrackingType, se.WeightKg, se.RepsPerSet, se.Mark
            FROM SessionEntry se
            JOIN Session s ON s.Id = se.SessionId
            JOIN Split sp ON sp.Id = s.SplitId
            JOIN Exercise e ON e.Id = se.ExerciseId
            WHERE s.CompletedAtUtc IS NOT NULL
            ORDER BY s.StartedAtUtc, se.OrderInSession
            """);
}
