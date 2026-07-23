using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using StackUp.App.Resources.Strings;
using StackUp.Core.Data;
using StackUp.Core.Entities;
using StackUp.Core.Enums;
using StackUp.Core.Progression;
using StackUp.Core.Units;
using SkiaSharp;

namespace StackUp.App.ViewModels;

/// <summary>
/// Stats tab: one chart per exercise, all shown by default. The filter panel
/// toggles per-exercise visibility, persisted on <see cref="Exercise.IsVisibleInStats"/>.
/// </summary>
public partial class StatsViewModel : ObservableObject
{
    private readonly AppDatabase _db;
    private WeightUnit _unit;
    private List<Exercise> _exercises = [];

    public StatsViewModel(AppDatabase db)
    {
        _db = db;
        RangeIndex = 2;
    }

    public ObservableCollection<ExerciseStatCardViewModel> Cards { get; } = [];

    public ObservableCollection<StatFilterItemViewModel> FilterItems { get; } = [];

    /// <summary>0 = 3 months, 1 = 1 year, 2 = all.</summary>
    [ObservableProperty]
    public partial int RangeIndex { get; set; }

    [ObservableProperty]
    public partial bool IsFilterOpen { get; set; }

    [ObservableProperty]
    public partial bool HasCards { get; set; }

    public async Task LoadAsync()
    {
        await _db.InitializeAsync();
        _unit = (await _db.GetSettingsAsync()).Unit;
        _exercises = await _db.GetExercisesAsync();

        FilterItems.Clear();
        foreach (var exercise in _exercises)
            FilterItems.Add(new StatFilterItemViewModel(this, exercise, _db));

        await RebuildCardsAsync();
    }

    partial void OnRangeIndexChanged(int value) => _ = RebuildCardsAsync();

    [RelayCommand]
    private void SetRange(string index) => RangeIndex = int.Parse(index);

    [RelayCommand]
    private void ToggleFilter() => IsFilterOpen = !IsFilterOpen;

    /// <summary>Called by filter items after a visibility toggle was persisted.</summary>
    public Task OnFilterChangedAsync() => RebuildCardsAsync();

    private async Task RebuildCardsAsync()
    {
        DateTime? since = RangeIndex switch
        {
            0 => DateTime.UtcNow.AddMonths(-3),
            1 => DateTime.UtcNow.AddYears(-1),
            _ => null,
        };

        Cards.Clear();
        foreach (var exercise in _exercises.Where(e => e.IsVisibleInStats))
        {
            var history = await _db.GetExerciseHistoryAsync(exercise.Id, since);
            Cards.Add(BuildCard(exercise, history));
        }
        HasCards = Cards.Count > 0;
    }

    /// <summary>Axis density per range: 3M → biweekly day labels, 1Y → ~2-month names, All → derived from data span.</summary>
    private (long MinStep, string DateFormat) GetAxisScale(List<DateTimePoint> points)
    {
        switch (RangeIndex)
        {
            case 0:
                return (TimeSpan.FromDays(14).Ticks, "dd.MM.");
            case 1:
                return (TimeSpan.FromDays(61).Ticks, "MMM yy");
            default:
                var spanDays = points.Count >= 2 ? (points[^1].DateTime - points[0].DateTime).TotalDays : 0;
                if (spanDays <= 100)
                    return (TimeSpan.FromDays(14).Ticks, "dd.MM.");
                if (spanDays <= 500)
                    return (TimeSpan.FromDays(61).Ticks, "MMM yy");
                return (TimeSpan.FromDays(Math.Ceiling(spanDays / 6)).Ticks, "MMM yy"); // ~6 labels
        }
    }

    private ExerciseStatCardViewModel BuildCard(Exercise exercise, List<ExerciseHistoryPoint> history)
    {
        var isWeight = exercise.TrackingType == TrackingType.WeightBased;

        var points = history
            .Select(h => new DateTimePoint(
                h.StartedAtUtc.ToLocalTime(),
                isWeight
                    ? (h.WeightKg is { } w ? UnitConverter.KgToDisplay(w, _unit) : null)
                    : ProgressionEngine.CountingReps(RepsSerializer.Parse(h.RepsPerSet), CountingSetRule.BestSet)))
            .Where(p => p.Value is not null)
            .ToList();

        var accent = new SKColor(0x2E, 0x6E, 0x62);
        var unitLabel = isWeight ? UnitConverter.UnitLabel(_unit) : AppStrings.Stats_Reps;
        var latest = points.Count > 0 ? $"{points[^1].Value:0.##} {unitLabel}" : AppStrings.Stats_NoDataYet;
        var (minStep, dateFormat) = GetAxisScale(points);

        return new ExerciseStatCardViewModel
        {
            Title = exercise.Name,
            Subtitle = latest,
            IconSource = $"icon_{exercise.IconKey}.png",
            HasData = points.Count > 0,
            Series =
            [
                new LineSeries<DateTimePoint>
                {
                    Values = points,
                    GeometrySize = 6,
                    LineSmoothness = 0.2,
                    Stroke = new SolidColorPaint(accent, 2.5f),
                    GeometryStroke = new SolidColorPaint(accent, 2.5f),
                    Fill = new SolidColorPaint(accent.WithAlpha(28)),
                    Name = exercise.Name,
                },
            ],
            XAxes =
            [
                new Axis
                {
                    Labeler = v => new DateTime((long)Math.Max(0, v)).ToString(dateFormat),
                    UnitWidth = TimeSpan.FromDays(1).Ticks,
                    MinStep = minStep,
                    LabelsPaint = new SolidColorPaint(SKColors.Gray),
                    TextSize = 11,
                },
            ],
            YAxes =
            [
                new Axis
                {
                    LabelsPaint = new SolidColorPaint(SKColors.Gray),
                    TextSize = 11,
                    MinLimit = 0,
                },
            ],
        };
    }
}

/// <summary>One exercise's chart card on the stats tab.</summary>
public class ExerciseStatCardViewModel
{
    public required string Title { get; init; }
    public required string Subtitle { get; init; }
    public required string IconSource { get; init; }
    public required bool HasData { get; init; }
    public ISeries[] Series { get; init; } = [];
    public Axis[] XAxes { get; init; } = [];
    public Axis[] YAxes { get; init; } = [];
}

/// <summary>Filter row: toggling persists Exercise.IsVisibleInStats and rebuilds the cards.</summary>
public partial class StatFilterItemViewModel : ObservableObject
{
    private readonly StatsViewModel _parent;
    private readonly Exercise _exercise;
    private readonly AppDatabase _db;
    private bool _loading = true;

    public StatFilterItemViewModel(StatsViewModel parent, Exercise exercise, AppDatabase db)
    {
        _parent = parent;
        _exercise = exercise;
        _db = db;
        Name = exercise.Name;
        IsShown = exercise.IsVisibleInStats;
        _loading = false;
    }

    public string Name { get; }

    [ObservableProperty]
    public partial bool IsShown { get; set; }

    partial void OnIsShownChanged(bool value)
    {
        if (_loading)
            return;
        _exercise.IsVisibleInStats = value;
        _ = PersistAndRefreshAsync();
    }

    private async Task PersistAndRefreshAsync()
    {
        await _db.SaveExerciseAsync(_exercise);
        await _parent.OnFilterChangedAsync();
    }
}
