using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SimpleGymDiary.Core.Data;
using SimpleGymDiary.Core.Entities;

namespace SimpleGymDiary.App.ViewModels;

public partial class SplitsViewModel : ObservableObject
{
    private readonly AppDatabase _db;

    public SplitsViewModel(AppDatabase db) => _db = db;

    public ObservableCollection<SplitListItemViewModel> Splits { get; } = [];

    public async Task LoadAsync()
    {
        await _db.InitializeAsync();
        Splits.Clear();
        foreach (var split in await _db.GetSplitsAsync())
        {
            var exercises = await _db.GetSplitExercisesAsync(split.Id);
            Splits.Add(new SplitListItemViewModel(split, $"{exercises.Count} exercises"));
        }
    }

    [RelayCommand]
    private async Task AddSplitAsync()
    {
        var name = await Shell.Current.DisplayPromptAsync("New split", "Name of the training day:", "Create", "Cancel");
        if (string.IsNullOrWhiteSpace(name))
            return;

        var split = new Split { Name = name.Trim(), SortOrder = Splits.Count };
        await _db.SaveSplitAsync(split);
        await Shell.Current.GoToAsync($"splitdetail?splitId={split.Id}");
    }

    [RelayCommand]
    private static async Task OpenSplitAsync(SplitListItemViewModel item) =>
        await Shell.Current.GoToAsync($"splitdetail?splitId={item.Split.Id}");
}

public record SplitListItemViewModel(Split Split, string Subtitle);
