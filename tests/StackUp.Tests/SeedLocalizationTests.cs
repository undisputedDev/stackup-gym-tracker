using System.Globalization;
using SQLite;
using StackUp.Core.Data;

namespace StackUp.Tests;

/// <summary>
/// Preset seeding across device languages: names are localized at insert time, but the
/// stable PresetKey keeps EnsurePresetsAsync idempotent across languages, renames, and
/// archived (user-deleted) rows.
/// </summary>
public sealed class SeedLocalizationTests : IAsyncLifetime
{
    private readonly string _path = Path.Combine(Path.GetTempPath(), $"stackup-l10n-test-{Guid.NewGuid():N}.db3");
    private readonly List<AppDatabase> _open = [];

    private async Task<AppDatabase> CreateAsync()
    {
        var db = new AppDatabase(_path);
        _open.Add(db);
        await db.InitializeAsync();
        return db;
    }

    /// <summary>Runs an action under the given UI culture, restoring the original afterwards.</summary>
    private static async Task WithUiCultureAsync(string culture, Func<Task> action)
    {
        var original = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = new CultureInfo(culture);
            await action();
        }
        finally
        {
            CultureInfo.CurrentUICulture = original;
        }
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        foreach (var db in _open)
            await db.CloseAsync();
        if (File.Exists(_path))
            File.Delete(_path);
    }

    [Fact]
    public async Task Seed_German_UsesGermanNames_WithKeys()
    {
        await WithUiCultureAsync("de-DE", async () =>
        {
            var db = await CreateAsync();

            var splits = await db.GetSplitsAsync();
            Assert.Contains(splits, s => s.Name == "Oberkörper" && s.PresetKey == "upper_body");
            Assert.Contains(splits, s => s.Name == "Ganzkörper" && s.PresetKey == "full_body");

            var exercises = await db.GetExercisesAsync();
            Assert.Contains(exercises, e => e.Name == "Bankdrücken" && e.PresetKey == "bench_press");
            Assert.Contains(exercises, e => e.Name == "Klimmzüge" && e.PresetKey == "pull_ups");
            Assert.Equal(SeedData.Exercises.Length, exercises.Count);
        });
    }

    [Fact]
    public async Task Seed_English_ThenGermanRerun_NoDuplicates()
    {
        await WithUiCultureAsync("en-US", async () => await CreateAsync());

        await WithUiCultureAsync("de-DE", async () =>
        {
            var db = await CreateAsync(); // reopens the same file; migrations already ran, re-run seeder directly
            var conn = new SQLiteAsyncConnection(_path);
            await SeedData.EnsurePresetsAsync(conn);
            await conn.CloseAsync();

            Assert.Equal(SeedData.Exercises.Length, (await db.GetExercisesAsync()).Count);
            Assert.Equal(SeedData.Splits.Length, (await db.GetSplitsAsync()).Count);
            // Existing rows keep their English names — names are user data.
            Assert.Contains(await db.GetExercisesAsync(), e => e.Name == "Bench Press");
        });
    }

    [Fact]
    public async Task Seed_German_ThenEnglishRerun_NoDuplicates()
    {
        await WithUiCultureAsync("de-DE", async () => await CreateAsync());

        await WithUiCultureAsync("en-US", async () =>
        {
            var db = await CreateAsync();
            var conn = new SQLiteAsyncConnection(_path);
            await SeedData.EnsurePresetsAsync(conn);
            await conn.CloseAsync();

            Assert.Equal(SeedData.Exercises.Length, (await db.GetExercisesAsync()).Count);
            Assert.Equal(SeedData.Splits.Length, (await db.GetSplitsAsync()).Count);
            Assert.Contains(await db.GetExercisesAsync(), e => e.Name == "Bankdrücken");
        });
    }

    [Fact]
    public async Task ArchivedPreset_NotResurrected_AcrossLanguages()
    {
        var db = await WithUiCultureResultAsync("en-US", CreateAsync);
        var bench = (await db.GetExercisesAsync()).First(e => e.Name == "Bench Press");
        await db.ArchiveExerciseAsync(bench.Id);

        await WithUiCultureAsync("de-DE", async () =>
        {
            var conn = new SQLiteAsyncConnection(_path);
            await SeedData.EnsurePresetsAsync(conn);
            await conn.CloseAsync();

            var visible = await db.GetExercisesAsync();
            Assert.DoesNotContain(visible, e => e.Name == "Bench Press");
            Assert.DoesNotContain(visible, e => e.Name == "Bankdrücken");
        });
    }

    [Fact]
    public async Task RenamedPreset_NotReseeded()
    {
        var db = await WithUiCultureResultAsync("en-US", CreateAsync);
        var bench = (await db.GetExercisesAsync()).First(e => e.Name == "Bench Press");
        bench.Name = "Flat DB Press";
        await db.SaveExerciseAsync(bench);

        var conn = new SQLiteAsyncConnection(_path);
        await SeedData.EnsurePresetsAsync(conn); // key match prevents re-seeding under the old name
        await conn.CloseAsync();

        var exercises = await db.GetExercisesAsync();
        Assert.Equal(SeedData.Exercises.Length, exercises.Count);
        Assert.DoesNotContain(exercises, e => e.Name == "Bench Press");
    }

    [Fact]
    public async Task Migration_BackfillsPresetKeys_UserRowsStayNull()
    {
        var db = await WithUiCultureResultAsync("en-US", CreateAsync);

        var custom = new StackUp.Core.Entities.Exercise { Name = "Cable Crossover" };
        await db.SaveExerciseAsync(custom);

        var exercises = await db.GetExercisesAsync();
        Assert.All(exercises.Where(e => e.Name != "Cable Crossover"), e => Assert.False(string.IsNullOrEmpty(e.PresetKey)));
        Assert.Null(exercises.First(e => e.Name == "Cable Crossover").PresetKey);
    }

    private async Task<AppDatabase> WithUiCultureResultAsync(string culture, Func<Task<AppDatabase>> factory)
    {
        AppDatabase? db = null;
        await WithUiCultureAsync(culture, async () => db = await factory());
        return db!;
    }
}
