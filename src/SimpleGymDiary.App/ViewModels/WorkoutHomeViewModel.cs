using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SimpleGymDiary.Core.Data;
using SimpleGymDiary.Core.Entities;
using SimpleGymDiary.Core.Enums;
using SimpleGymDiary.Core.Progression;

namespace SimpleGymDiary.App.ViewModels;

public partial class WorkoutHomeViewModel : ObservableObject
{
    private readonly AppDatabase _db;

    public WorkoutHomeViewModel(AppDatabase db)
    {
        _db = db;
        InProgressText = "";
        MomentumText = "";
    }

    /// <summary>"Week 14 · 26 sessions" — quiet sense of an ongoing streak.</summary>
    [ObservableProperty]
    public partial string MomentumText { get; set; }

    public ObservableCollection<SplitCardViewModel> Splits { get; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasInProgress))]
    public partial Session? InProgressSession { get; set; }

    [ObservableProperty]
    public partial string InProgressText { get; set; }

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

        var (count, firstUtc) = await _db.GetCompletedSessionStatsAsync();
        MomentumText = count > 0 && firstUtc is { } first
            ? $"Week {(DateTime.UtcNow.Date - first.Date).Days / 7 + 1} · {count} sessions"
            : "";

        Splits.Clear();
        foreach (var split in await _db.GetSplitsAsync())
        {
            var last = await _db.GetLastCompletedSessionForSplitAsync(split.Id);
            var (up, keep, down) = await CountMarksAsync(last);
            Splits.Add(new SplitCardViewModel(split, LastDoneText(last), up, keep, down, HasMarks: up + keep + down > 0));
        }
    }

    /// <summary>Mark counts of the split's last completed session (logged entries only).</summary>
    private async Task<(int Up, int Keep, int Down)> CountMarksAsync(Session? last)
    {
        if (last is null)
            return (0, 0, 0);

        var logged = (await _db.GetSessionEntriesAsync(last.Id))
            .Where(e => RepsSerializer.Parse(e.RepsPerSet).Any(r => r > 0))
            .ToList();
        return (
            logged.Count(e => e.Mark == Mark.Up),
            logged.Count(e => e.Mark == Mark.Keep),
            logged.Count(e => e.Mark == Mark.Down));
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

public record SplitCardViewModel(
    Split Split,
    string LastDoneText,
    int UpCount,
    int KeepCount,
    int DownCount,
    bool HasMarks)
{
    public string UpText => $"▲ {UpCount}";
    public string KeepText => $"▬ {KeepCount}";
    public string DownText => $"▼ {DownCount}";
}
