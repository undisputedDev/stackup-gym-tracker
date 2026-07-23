using SQLite;
using StackUp.Core.Data;

namespace StackUp.Tests;

/// <summary>Backup creation, candidate validation, and restore-with-migrations against temp-file SQLite.</summary>
public sealed class BackupRestoreTests : IAsyncLifetime
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), $"stackup-backup-tests-{Guid.NewGuid():N}");
    private readonly List<AppDatabase> _open = [];

    public BackupRestoreTests() => Directory.CreateDirectory(_dir);

    private string PathFor(string name) => Path.Combine(_dir, name);

    private async Task<AppDatabase> CreateAsync(string name)
    {
        var db = new AppDatabase(PathFor(name));
        _open.Add(db);
        await db.InitializeAsync();
        return db;
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        foreach (var db in _open)
            await db.CloseAsync();
        if (Directory.Exists(_dir))
            Directory.Delete(_dir, recursive: true);
    }

    private static async Task<int> CompleteNewSessionAsync(AppDatabase db, int day)
    {
        var splits = await db.GetSplitsAsync();
        var session = await db.StartSessionAsync(splits[0].Id, new DateTime(2026, 7, day, 17, 0, 0, DateTimeKind.Utc));
        await db.CompleteSessionAsync(session.Id, new DateTime(2026, 7, day, 18, 0, 0, DateTimeKind.Utc));
        return session.Id;
    }

    [Fact]
    public async Task CreateBackup_ProducesValidFileWithSameData()
    {
        var db = await CreateAsync("main.db3");
        await CompleteNewSessionAsync(db, 1);
        await CompleteNewSessionAsync(db, 4);

        var backupPath = PathFor("backup.db3");
        await db.CreateBackupAsync(backupPath);

        Assert.Equal(BackupFileStatus.Valid, await AppDatabase.ValidateBackupFileAsync(backupPath));

        var restoredView = await CreateAsync("backup.db3");
        var (count, _) = await restoredView.GetCompletedSessionStatsAsync();
        Assert.Equal(2, count);
    }

    [Fact]
    public async Task Validate_GarbageFile_ReturnsInvalid()
    {
        var path = PathFor("garbage.db3");
        await File.WriteAllBytesAsync(path, [0xDE, 0xAD, 0xBE, 0xEF, 0x00, 0x42, 0x13, 0x37]);

        Assert.Equal(BackupFileStatus.Invalid, await AppDatabase.ValidateBackupFileAsync(path));
    }

    [Fact]
    public async Task Validate_MissingTables_ReturnsInvalid()
    {
        var path = PathFor("other.db3");
        var conn = new SQLiteAsyncConnection(path);
        await conn.ExecuteAsync("CREATE TABLE SomethingElse (Id INTEGER PRIMARY KEY)");
        await conn.CloseAsync();

        Assert.Equal(BackupFileStatus.Invalid, await AppDatabase.ValidateBackupFileAsync(path));
    }

    [Fact]
    public async Task Validate_NewerUserVersion_ReturnsNewerAppVersion()
    {
        var db = await CreateAsync("main.db3");
        var backupPath = PathFor("future.db3");
        await db.CreateBackupAsync(backupPath);

        var conn = new SQLiteAsyncConnection(backupPath);
        await conn.ExecuteAsync("PRAGMA user_version = 99");
        await conn.CloseAsync();

        Assert.Equal(BackupFileStatus.NewerAppVersion, await AppDatabase.ValidateBackupFileAsync(backupPath));
    }

    [Fact]
    public async Task Restore_ReplacesData()
    {
        var source = await CreateAsync("source.db3");
        await CompleteNewSessionAsync(source, 1);
        await CompleteNewSessionAsync(source, 4);
        var backupPath = PathFor("transfer.db3");
        await source.CreateBackupAsync(backupPath);

        var target = await CreateAsync("target.db3");
        Assert.Equal(0, (await target.GetCompletedSessionStatsAsync()).Count);

        await target.RestoreFromFileAsync(backupPath);

        Assert.Equal(2, (await target.GetCompletedSessionStatsAsync()).Count);
    }

    [Fact]
    public async Task Restore_OlderVersion_RerunsMigrations()
    {
        var db = await CreateAsync("main.db3");
        var backupPath = PathFor("old.db3");
        await db.CreateBackupAsync(backupPath);

        // Pretend the backup came from an old app version: migrations must re-run on restore.
        var conn = new SQLiteAsyncConnection(backupPath);
        await conn.ExecuteAsync("PRAGMA user_version = 2");
        await conn.CloseAsync();

        await db.RestoreFromFileAsync(backupPath);

        // Re-running migrations (incl. the v3 preset seeder) must not duplicate presets.
        Assert.Equal(SeedData.Exercises.Length, (await db.GetExercisesAsync()).Count);
        Assert.Equal(SeedData.Splits.Length, (await db.GetSplitsAsync()).Count);

        // The restored file is migrated fully forward — same version as a fresh install.
        await db.CloseAsync();
        var restoredConn = new SQLiteAsyncConnection(PathFor("main.db3"), SQLiteOpenFlags.ReadOnly);
        var restoredVersion = await restoredConn.ExecuteScalarAsync<int>("PRAGMA user_version");
        await restoredConn.CloseAsync();

        await CreateAsync("target.db3");
        var freshConn = new SQLiteAsyncConnection(PathFor("target.db3"), SQLiteOpenFlags.ReadOnly);
        var freshVersion = await freshConn.ExecuteScalarAsync<int>("PRAGMA user_version");
        await freshConn.CloseAsync();

        Assert.True(restoredVersion > 2);
        Assert.Equal(freshVersion, restoredVersion);
    }

    [Fact]
    public async Task Restore_AfterFailedCopy_ConnectionStillWorks()
    {
        var db = await CreateAsync("main.db3");
        await CompleteNewSessionAsync(db, 1);

        await Assert.ThrowsAnyAsync<IOException>(() => db.RestoreFromFileAsync(PathFor("does-not-exist.db3")));

        // Old data is untouched and the reopened connection is usable.
        await db.InitializeAsync();
        Assert.Equal(1, (await db.GetCompletedSessionStatsAsync()).Count);
    }
}
