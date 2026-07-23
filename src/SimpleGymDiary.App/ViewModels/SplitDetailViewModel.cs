using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SimpleGymDiary.Core.Data;
using SimpleGymDiary.Core.Entities;

namespace SimpleGymDiary.App.ViewModels;

[QueryProperty(nameof(SplitId), "splitId")]
public partial class SplitDetailViewModel : ObservableObject
{
    private readonly AppDatabase _db;
    private Split? _split;

    public SplitDetailViewModel(AppDatabase db)
    {
        _db = db;
        Name = "";
    }

    [ObservableProperty]
    public partial int SplitId { get; set; }

    [ObservableProperty]
    public partial string Name { get; set; }

    public ObservableCollection<Exercise> Exercises { get; } = [];

    public async Task LoadAsync()
    {
        await _db.InitializeAsync();
        _split = await _db.GetSplitAsync(SplitId);
        if (_split is null)
            return;

        Name = _split.Name;
        Exercises.Clear();
        foreach (var exercise in await _db.GetSplitExercisesAsync(SplitId))
            Exercises.Add(exercise);
    }

    [RelayCommand]
    private async Task RenameAsync()
    {
        var name = await Shell.Current.DisplayPromptAsync("Rename split", "Name:", "Save", "Cancel", initialValue: Name);
        if (string.IsNullOrWhiteSpace(name) || _split is null)
            return;
        _split.Name = name.Trim();
        Name = _split.Name;
        await _db.SaveSplitAsync(_split);
    }

    [RelayCommand]
    private async Task AddExerciseAsync()
    {
        var all = await _db.GetExercisesAsync();
        var candidates = all.Where(e => Exercises.All(x => x.Id != e.Id)).ToList();

        const string newOption = "New exercise…";
        var options = candidates.Select(c => c.Name).Append(newOption).ToArray();
        var choice = await Shell.Current.DisplayActionSheetAsync("Add exercise", "Cancel", null, options);

        if (choice is null or "Cancel")
            return;

        if (choice == newOption)
        {
            await Shell.Current.GoToAsync($"exerciseedit?exerciseId=0&splitId={SplitId}");
            return;
        }

        var picked = candidates.First(c => c.Name == choice);
        Exercises.Add(picked);
        await SaveOrderAsync();
    }

    [RelayCommand]
    private async Task RemoveExerciseAsync(Exercise exercise)
    {
        Exercises.Remove(exercise);
        await SaveOrderAsync();
    }

    /// <summary>Persists the list order after a drag-reorder (collection is already reordered).</summary>
    public Task PersistOrderAsync() => SaveOrderAsync();

    [RelayCommand]
    private static async Task EditExerciseAsync(Exercise exercise) =>
        await Shell.Current.GoToAsync($"exerciseedit?exerciseId={exercise.Id}");

    [RelayCommand]
    private async Task ArchiveSplitAsync()
    {
        var confirmed = await Shell.Current.DisplayAlertAsync("Delete split",
            $"Remove \"{Name}\" from your plan? Completed sessions are kept.", "Delete", "Cancel");
        if (!confirmed)
            return;
        await _db.ArchiveSplitAsync(SplitId);
        await Shell.Current.GoToAsync("..");
    }

    private Task SaveOrderAsync() =>
        _db.SetSplitExercisesAsync(SplitId, Exercises.Select(e => e.Id).ToList());
}
