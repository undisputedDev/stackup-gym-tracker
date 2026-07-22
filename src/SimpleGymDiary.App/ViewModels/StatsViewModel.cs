using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SimpleGymDiary.Core.Data;
using SimpleGymDiary.Core.Entities;
using SimpleGymDiary.Core.Enums;
using SimpleGymDiary.Core.Progression;
using SimpleGymDiary.Core.Units;
using SkiaSharp;

namespace SimpleGymDiary.App.ViewModels;

public partial class StatsViewModel : ObservableObject
{
    private readonly AppDatabase _db;
    private WeightUnit _unit;

    public StatsViewModel(AppDatabase db) => _db = db;

    public ObservableCollection<Exercise> Exercises { get; } = [];

    [ObservableProperty]
    private Exercise? _selectedExercise;

    /// <summary>0 = 3 months, 1 = 1 year, 2 = all.</summary>
    [ObservableProperty]
    private int _rangeIndex = 2;

    [ObservableProperty]
    private ISeries[] _series = [];

    [ObservableProperty]
    private Axis[] _xAxes = [];

    [ObservableProperty]
    private Axis[] _yAxes = [];

    [ObservableProperty]
    private bool _hasData;

    [ObservableProperty]
    private string _emptyText = "Pick an exercise to see your progression.";

    public async Task LoadAsync()
    {
        await _db.InitializeAsync();
        _unit = (await _db.GetSettingsAsync()).Unit;

        var previous = SelectedExercise?.Id;
        Exercises.Clear();
        foreach (var exercise in await _db.GetExercisesAsync())
            Exercises.Add(exercise);

        SelectedExercise = Exercises.FirstOrDefault(e => e.Id == previous) ?? Exercises.FirstOrDefault();
    }

    partial void OnSelectedExerciseChanged(Exercise? value) => _ = RefreshChartAsync();

    partial void OnRangeIndexChanged(int value) => _ = RefreshChartAsync();

    [RelayCommand]
    private void SetRange(string index) => RangeIndex = int.Parse(index);

    private async Task RefreshChartAsync()
    {
        if (SelectedExercise is null)
        {
            Series = [];
            HasData = false;
            return;
        }

        DateTime? since = RangeIndex switch
        {
            0 => DateTime.UtcNow.AddMonths(-3),
            1 => DateTime.UtcNow.AddYears(-1),
            _ => null,
        };

        var isWeight = SelectedExercise.TrackingType == TrackingType.WeightBased;
        var history = await _db.GetExerciseHistoryAsync(SelectedExercise.Id, since);

        var points = history
            .Select(h => new DateTimePoint(
                h.StartedAtUtc.ToLocalTime(),
                isWeight
                    ? (h.WeightKg is { } w ? UnitConverter.KgToDisplay(w, _unit) : null)
                    : ProgressionEngine.CountingReps(RepsSerializer.Parse(h.RepsPerSet), CountingSetRule.BestSet)))
            .Where(p => p.Value is not null)
            .ToList();

        HasData = points.Count > 0;
        EmptyText = HasData ? "" : "No completed sessions for this exercise yet.";

        var accent = new SKColor(0x2E, 0x6E, 0x62);
        Series =
        [
            new LineSeries<DateTimePoint>
            {
                Values = points,
                GeometrySize = 8,
                LineSmoothness = 0.2,
                Stroke = new SolidColorPaint(accent, 3),
                GeometryStroke = new SolidColorPaint(accent, 3),
                Fill = new SolidColorPaint(accent.WithAlpha(28)),
                Name = SelectedExercise.Name,
            },
        ];

        XAxes =
        [
            new Axis
            {
                Labeler = v => new DateTime((long)Math.Max(0, v)).ToString("dd.MM."),
                UnitWidth = TimeSpan.FromDays(1).Ticks,
                MinStep = TimeSpan.FromDays(1).Ticks,
                LabelsPaint = new SolidColorPaint(SKColors.Gray),
            },
        ];

        YAxes =
        [
            new Axis
            {
                Name = isWeight ? $"Weight ({UnitConverter.UnitLabel(_unit)})" : "Reps (best set)",
                NamePaint = new SolidColorPaint(SKColors.Gray),
                LabelsPaint = new SolidColorPaint(SKColors.Gray),
                MinLimit = 0,
            },
        ];
    }
}
