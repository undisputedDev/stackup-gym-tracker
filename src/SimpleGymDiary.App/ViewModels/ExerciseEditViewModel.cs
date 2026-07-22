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

    public ExerciseEditViewModel(AppDatabase db) => _db = db;

    [ObservableProperty]
    private int _exerciseId;

    /// <summary>When creating from a split's Add flow, the new exercise is appended to this split.</summary>
    [ObservableProperty]
    private int _splitId;

    [ObservableProperty]
    private string _pageTitle = "New exercise";

    [ObservableProperty]
    private string _name = "";

    /// <summary>0 = weight-based, 1 = rep-based (bodyweight); matches picker index.</summary>
    [ObservableProperty]
    private int _trackingTypeIndex;

    [ObservableProperty]
    private bool _isExisting;

    // Override fields (empty = use global default)
    [ObservableProperty] private string _repMinText = "";
    [ObservableProperty] private string _repMaxText = "";
    [ObservableProperty] private string _incrementText = "";
    [ObservableProperty] private string _repIncrementText = "";

    // Placeholders showing the effective global defaults
    [ObservableProperty] private string _repMinPlaceholder = "";
    [ObservableProperty] private string _repMaxPlaceholder = "";
    [ObservableProperty] private string _incrementPlaceholder = "";
    [ObservableProperty] private string _repIncrementPlaceholder = "";

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
            await Shell.Current.DisplayAlert("Missing name", "Please enter an exercise name.", "OK");
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
        var confirmed = await Shell.Current.DisplayAlert("Delete exercise",
            $"Remove \"{Name}\" from all splits? Your history is kept.", "Delete", "Cancel");
        if (!confirmed)
            return;
        await _db.ArchiveExerciseAsync(_exercise.Id);
        await Shell.Current.GoToAsync("..");
    }

    private static int? ParseIntOrNull(string text) =>
        int.TryParse(text, out var v) ? v : null;
}
