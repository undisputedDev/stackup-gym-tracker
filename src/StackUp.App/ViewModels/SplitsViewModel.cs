using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StackUp.App.Resources.Strings;
using StackUp.Core.Data;
using StackUp.Core.Entities;

namespace StackUp.App.ViewModels;

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
            Splits.Add(new SplitListItemViewModel(split, string.Format(AppStrings.Splits_ExerciseCountFormat, exercises.Count)));
        }
    }

    [RelayCommand]
    private async Task AddSplitAsync()
    {
        var name = await Shell.Current.DisplayPromptAsync(AppStrings.Splits_NewSplitTitle,
            AppStrings.Splits_NewSplitPrompt, AppStrings.Common_Create, AppStrings.Common_Cancel);
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
