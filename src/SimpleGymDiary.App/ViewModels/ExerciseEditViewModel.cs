using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SimpleGymDiary.Core.Data;
using SimpleGymDiary.Core.Entities;
using SimpleGymDiary.Core.Enums;
using SimpleGymDiary.Core.Units;

namespace SimpleGymDiary.App.ViewModels;

/// <summary>
/// Create (exerciseId=0) or edit an exercise. Override fields are free-text entries:
/// empty = inherit the global default (shown as placeholder).
/// </summary>
[QueryProperty(nameof(ExerciseId), "exerciseId")]
[QueryProperty(nameof(SplitId), "splitId")]
public partial class ExerciseEditViewModel : ObservableObject
{
    private readonly AppDatabase _db;
    private Exercise _exercise = new();

    public ExerciseEditViewModel(AppDatabase db)
    {
        _db = db;
        PageTitle = "New exercise";
        Name = "";
        RepMinText = RepMaxText = IncrementText = RepIncrementText = "";
        RepMinPlaceholder = RepMaxPlaceholder = IncrementPlaceholder = RepIncrementPlaceholder = "";
    }

    [ObservableProperty]
    public partial int ExerciseId { get; set; }

    /// <summary>When creating from a split's Add flow, the new exercise is appended to this split.</summary>
    [ObservableProperty]
    public partial int SplitId { get; set; }

    [ObservableProperty]
    public partial string PageTitle { get; set; }

    [ObservableProperty]
    public partial string Name { get; set; }

    /// <summary>0 = weight-based, 1 = rep-based (bodyweight); matches picker index.</summary>
    [ObservableProperty]
    public partial int TrackingTypeIndex { get; set; }

    [ObservableProperty]
    public partial bool IsExisting { get; set; }

    // Override fields (empty = use global default)
    [ObservableProperty] public partial string RepMinText { get; set; }
    [ObservableProperty] public partial string RepMaxText { get; set; }
    [ObservableProperty] public partial string IncrementText { get; set; }
    [ObservableProperty] public partial string RepIncrementText { get; set; }

    // Placeholders showing the effective global defaults
    [ObservableProperty] public partial string RepMinPlaceholder { get; set; }
    [ObservableProperty] public partial string RepMaxPlaceholder { get; set; }
    [ObservableProperty] public partial string IncrementPlaceholder { get; set; }
    [ObservableProperty] public partial string RepIncrementPlaceholder { get; set; }

    public async Task LoadAsync()
    {
        await _db.InitializeAsync();
        var settings = await _db.GetSettingsAsync();
        RepMinPlaceholder = $"Default: {settings.DefaultRepRangeMin}";
        RepMaxPlaceholder = $"Default: {settings.DefaultRepRangeMax}";
        IncrementPlaceholder = $"Default: {settings.DefaultWeightIncrementKg:0.##} kg";
        RepIncrementPlaceholder = $"Default: {settings.DefaultRepIncrement}";

        if (ExerciseId > 0)
        {
            var existing = await _db.GetExerciseAsync(ExerciseId);
            if (existing is null)
                return;
            _exercise = existing;
            IsExisting = true;
            PageTitle = "Edit exercise";
            Name = existing.Name;
            TrackingTypeIndex = (int)existing.TrackingType;
            RepMinText = existing.RepRangeMinOverride?.ToString() ?? "";
            RepMaxText = existing.RepRangeMaxOverride?.ToString() ?? "";
            IncrementText = existing.WeightIncrementKgOverride?.ToString("0.##") ?? "";
            RepIncrementText = existing.RepIncrementOverride?.ToString() ?? "";
        }
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            await Shell.Current.DisplayAlertAsync("Missing name", "Please enter an exercise name.", "OK");
            return;
        }

        _exercise.Name = Name.Trim();
        _exercise.TrackingType = (TrackingType)TrackingTypeIndex;
        _exercise.RepRangeMinOverride = ParseIntOrNull(RepMinText);
        _exercise.RepRangeMaxOverride = ParseIntOrNull(RepMaxText);
        _exercise.WeightIncrementKgOverride =
            UnitConverter.TryParseFlexible(IncrementText, out var inc) ? inc : null;
        _exercise.RepIncrementOverride = ParseIntOrNull(RepIncrementText);

        var isNew = _exercise.Id == 0;
        await _db.SaveExerciseAsync(_exercise);

        if (isNew && SplitId > 0)
        {
            var current = await _db.GetSplitExercisesAsync(SplitId);
            await _db.SetSplitExercisesAsync(SplitId, current.Select(e => e.Id).Append(_exercise.Id).ToList());
        }

        await Shell.Current.GoToAsync("..");
    }

    [RelayCommand]
    private async Task ArchiveAsync()
    {
        var confirmed = await Shell.Current.DisplayAlertAsync("Delete exercise",
            $"Remove \"{Name}\" from all splits? Your history is kept.", "Delete", "Cancel");
        if (!confirmed)
            return;
        await _db.ArchiveExerciseAsync(_exercise.Id);
        await Shell.Current.GoToAsync("..");
    }

    private static int? ParseIntOrNull(string text) =>
        int.TryParse(text, out var v) ? v : null;
}
