using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SimpleGymDiary.Core.Data;
using SimpleGymDiary.Core.Entities;

namespace SimpleGymDiary.App.ViewModels;

public partial class WorkoutHomeViewModel : ObservableObject
{
    private readonly AppDatabase _db;

    public WorkoutHomeViewModel(AppDatabase db) => _db = db;

    public ObservableCollection<SplitCardViewModel> Splits { get; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasInProgress))]
    private Session? _inProgressSession;

    [ObservableProperty]
    private string _inProgressText = "";

    public bool HasInProgress => InProgressSession is not null;

    public async Task LoadAsync()
    {
        await _db.InitializeAsync();

        InProgressSession = await _db.GetInProgressSessionAsync();
        if (InProgressSession is not null)
        {
            var split = await _db.GetSplitAsync(InProgressSession.SplitId);
            var started = InProgressSession.StartedAtUtc.ToLocalTime();
            InProgressText = $"Resume {split?.Name ?? "session"} — started {started:HH:mm}";
        }

        Splits.Clear();
        foreach (var split in await _db.GetSplitsAsync())
        {
            var last = await _db.GetLastCompletedSessionForSplitAsync(split.Id);
            Splits.Add(new SplitCardViewModel(split, LastDoneText(last)));
        }
    }

    private static string LastDoneText(Session? last)
    {
        if (last is null)
            return "Not done yet";
        var days = (DateTime.UtcNow.Date - last.StartedAtUtc.Date).Days;
        return days switch
        {
            0 => "Done today",
            1 => "Last done yesterday",
            _ => $"Last done {days} days ago",
        };
    }

    [RelayCommand]
    private async Task StartAsync(SplitCardViewModel card)
    {
        // Resume instead of stacking a second in-progress session for the same split.
        var session = InProgressSession?.SplitId == card.Split.Id
            ? InProgressSession
            : await _db.StartSessionAsync(card.Split.Id, DateTime.UtcNow);

        await Shell.Current.GoToAsync($"session?sessionId={session!.Id}");
    }

    [RelayCommand]
    private async Task ResumeAsync()
    {
        if (InProgressSession is not null)
            await Shell.Current.GoToAsync($"session?sessionId={InProgressSession.Id}");
    }
}

public record SplitCardViewModel(Split Split, string LastDoneText);
