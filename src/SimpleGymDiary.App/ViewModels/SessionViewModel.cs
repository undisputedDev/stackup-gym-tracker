using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SimpleGymDiary.Core.Data;
using SimpleGymDiary.Core.Entities;
using SimpleGymDiary.Core.Enums;
using SimpleGymDiary.Core.Progression;
using SimpleGymDiary.Core.Units;

namespace SimpleGymDiary.App.ViewModels;

[QueryProperty(nameof(SessionId), "sessionId")]
public partial class SessionViewModel : ObservableObject
{
    private readonly AppDatabase _db;
    private readonly Services.ReviewPrompter _reviewPrompter;

    private int? _prevSessionId;
    private int? _nextSessionId;
    private bool _finishedWithProgress;

    public SessionViewModel(AppDatabase db, Services.ReviewPrompter reviewPrompter)
    {
        _db = db;
        _reviewPrompter = reviewPrompter;
        Title = "";
        HeaderText = "";
        SummaryStatsUp = SummaryStatsKeep = SummaryStatsDown = "";
        SummaryDuration = "";
    }

    /// <summary>Best-effort haptic tick; unsupported platforms (desktop) just no-op.</summary>
    internal static void Haptic()
    {
        try
        {
            HapticFeedback.Default.Perform(HapticFeedbackType.Click);
        }
        catch
        {
            // not available on this platform
        }
    }

    [ObservableProperty]
    public partial int SessionId { get; set; }

    [ObservableProperty]
    public partial string Title { get; set; }

    /// <summary>"In progress" or the completion date of the session being viewed.</summary>
    [ObservableProperty]
    public partial string HeaderText { get; set; }

    /// <summary>True when browsing a completed (historical) session — everything is read-only.</summary>
    [ObservableProperty]
    public partial bool IsReadOnly { get; set; }

    [ObservableProperty]
    public partial bool CanGoPrev { get; set; }

    [ObservableProperty]
    public partial bool CanGoNext { get; set; }

    public ObservableCollection<SessionEntryViewModel> Entries { get; } = [];

    public async Task LoadAsync()
    {
        await _db.InitializeAsync();

        var session = await _db.GetSessionAsync(SessionId);
        if (session is null)
            return;

        var split = await _db.GetSplitAsync(session.SplitId);
        Title = split?.Name ?? "Workout";

        IsReadOnly = session.CompletedAtUtc is not null;
        HeaderText = session.CompletedAtUtc is { } done
            ? $"{done.ToLocalTime():ddd, dd.MM.yyyy} · read-only"
            : "In progress";

        // Neighbours within this split's timeline (oldest -> newest, in-progress last).
        var timeline = await _db.GetSessionsForSplitAsync(session.SplitId);
        var index = timeline.FindIndex(s => s.Id == session.Id);
        _prevSessionId = index > 0 ? timeline[index - 1].Id : null;
        _nextSessionId = index >= 0 && index < timeline.Count - 1 ? timeline[index + 1].Id : null;
        CanGoPrev = _prevSessionId is not null;
        CanGoNext = _nextSessionId is not null;

        var settings = await _db.GetSettingsAsync();
        var entries = await _db.GetSessionEntriesAsync(SessionId);

        Entries.Clear();
        foreach (var entry in entries)
        {
            var exercise = await _db.GetExerciseAsync(entry.ExerciseId);
            if (exercise is null)
                continue;
            Entries.Add(new SessionEntryViewModel(_db, entry, exercise, settings, isEditable: !IsReadOnly));
        }
    }

    [RelayCommand]
    private async Task GoPrevAsync()
    {
        if (_prevSessionId is { } id)
        {
            SessionId = id;
            await LoadAsync();
        }
    }

    [RelayCommand]
    private async Task GoNextAsync()
    {
        if (_nextSessionId is { } id)
        {
            SessionId = id;
            await LoadAsync();
        }
    }

    // ---- Finish summary ----

    [ObservableProperty]
    public partial bool IsSummaryVisible { get; set; }

    [ObservableProperty]
    public partial string SummaryStatsUp { get; set; }

    [ObservableProperty]
    public partial string SummaryStatsKeep { get; set; }

    [ObservableProperty]
    public partial string SummaryStatsDown { get; set; }

    [ObservableProperty]
    public partial string SummaryDuration { get; set; }

    public ObservableCollection<SummaryLine> SummaryLines { get; } = [];

    [RelayCommand]
    private async Task FinishAsync()
    {
        if (IsReadOnly || IsSummaryVisible)
            return;

        Haptic();
        var session = await _db.GetSessionAsync(SessionId);
        var now = DateTime.UtcNow;
        await _db.CompleteSessionAsync(SessionId, now);

        var logged = Entries.Where(e => e.HasLoggedData).ToList();
        _finishedWithProgress = logged.Any(e => e.Entry.Mark == Mark.Up);
        SummaryStatsUp = $"▲ {logged.Count(e => e.Entry.Mark == Mark.Up)}";
        SummaryStatsKeep = $"▬ {logged.Count(e => e.Entry.Mark == Mark.Keep)}";
        SummaryStatsDown = $"▼ {logged.Count(e => e.Entry.Mark == Mark.Down)}";
        SummaryDuration = session is not null
            ? $"{(int)(now - session.StartedAtUtc).TotalMinutes} min"
            : "";

        SummaryLines.Clear();
        foreach (var line in Entries.Select(e => e.BuildSummaryLine()).Where(l => l is not null))
            SummaryLines.Add(line!);
        if (SummaryLines.Count == 0 && logged.Count > 0)
            SummaryLines.Add(new SummaryLine("▬", Color.FromArgb("#8A8F98"), "All weights stay — solid session."));

        IsSummaryVisible = true;
    }

    [RelayCommand]
    private async Task CloseSummaryAsync()
    {
        IsSummaryVisible = false;
        await Shell.Current.GoToAsync("..");
        // Peak-happiness moment: milestone reached + progress made -> maybe ask for a review.
        await _reviewPrompter.TryRequestAsync(_finishedWithProgress);
    }
}

/// <summary>One line in the finish summary: "Lat Pulldown  60 → 62,5 kg".</summary>
public record SummaryLine(string Glyph, Color GlyphColor, string Text);

/// <summary>One exercise card on the session screen. Every change is persisted immediately (no Save button).</summary>
public partial class SessionEntryViewModel : ObservableObject
{
    private readonly AppDatabase _db;
    private readonly EffectiveExerciseSettings _eff;
    private readonly WeightUnit _unit;
    private bool _loading = true;

    public SessionEntry Entry { get; }
    public Exercise Exercise { get; }

    /// <summary>False when the entry belongs to a completed session being browsed read-only.</summary>
    public bool IsEditable { get; }

    public SessionEntryViewModel(AppDatabase db, SessionEntry entry, Exercise exercise, AppSettings settings, bool isEditable = true)
    {
        _db = db;
        Entry = entry;
        Exercise = exercise;
        IsEditable = isEditable;
        _unit = settings.Unit;
        _eff = EffectiveExerciseSettings.Resolve(exercise, settings);

        WeightText = entry.WeightKg is { } w ? UnitConverter.Format(w, _unit) : "";
        MarkGlyph = "=";
        MarkColor = Colors.Gray;
        StripeColor = Colors.Transparent;
        foreach (var reps in RepsSerializer.Parse(entry.RepsPerSet))
            Sets.Add(new SetViewModel(this, reps));

        _loading = false;
        RefreshMarkVisuals();
    }

    /// <summary>Snapshot name from session time; falls back to the current name for pre-snapshot rows.</summary>
    public string Name => string.IsNullOrEmpty(Entry.ExerciseNameSnapshot) ? Exercise.Name : Entry.ExerciseNameSnapshot;

    public bool IsWeightBased => Exercise.TrackingType == TrackingType.WeightBased;
    public string UnitLabel => UnitConverter.UnitLabel(_unit);
    public string IconSource => $"icon_{Exercise.IconKey}.png";

    /// <summary>True once any set has reps (or a manual mark was made) — drives stripe + summary.</summary>
    public bool HasLoggedData => Sets.Any(s => s.Reps > 0) || Entry.MarkIsManual;

    /// <summary>Summary line for the finish overlay; null when nothing changes next session.</summary>
    public SummaryLine? BuildSummaryLine()
    {
        if (!HasLoggedData || Entry.Mark == Mark.Keep)
            return null;

        var next = ProgressionEngine.SuggestNext(Exercise, _eff, Entry, Sets.Count);
        string text;
        if (IsWeightBased && next.WeightKg is { } w && Entry.WeightKg is { } current)
            text = $"{Name}   {UnitConverter.Format(current, _unit)} → {UnitConverter.Format(w, _unit)} {UnitLabel}";
        else if (!IsWeightBased && next.TargetReps is { } reps)
            text = $"{Name}   aim for {reps} reps";
        else
            return null;

        return new SummaryLine(MarkGlyph, MarkColor, text);
    }

    public string TargetHint
    {
        get
        {
            // Historical entries show the range that applied back then, not today's settings.
            var min = Entry.RepMinSnapshot ?? _eff.RepMin;
            var max = Entry.RepMaxSnapshot ?? _eff.RepMax;
            var range = $"Target {min}–{max} reps";
            if (IsWeightBased && Entry.SuggestedWeightKg is { } sw)
                return $"{range} · suggested {UnitConverter.Format(sw, _unit)} {UnitLabel}";
            if (!IsWeightBased && Entry.SuggestedReps is { } sr)
                return $"{range} · aim for {sr}";
            return range;
        }
    }

    public ObservableCollection<SetViewModel> Sets { get; } = [];

    // ---- Weight ----

    [ObservableProperty]
    public partial string WeightText { get; set; }

    partial void OnWeightTextChanged(string value)
    {
        if (_loading)
            return;
        Entry.WeightKg = UnitConverter.TryParseFlexible(value, out var display)
            ? UnitConverter.DisplayToKg(display, _unit)
            : null;
        Persist();
    }

    [RelayCommand]
    private void IncrementWeight() => StepWeight(+1);

    [RelayCommand]
    private void DecrementWeight() => StepWeight(-1);

    private void StepWeight(int direction)
    {
        if (!IsEditable)
            return;
        SessionViewModel.Haptic();
        var current = Entry.WeightKg ?? Entry.SuggestedWeightKg ?? 0;
        var next = Math.Max(0, current + direction * _eff.WeightIncrementKg);
        Entry.WeightKg = next;
        _loading = true; // don't double-persist via OnWeightTextChanged
        WeightText = UnitConverter.Format(next, _unit);
        _loading = false;
        Persist();
    }

    // ---- Sets / reps ----

    [RelayCommand]
    private void AddSet()
    {
        if (!IsEditable)
            return;
        SessionViewModel.Haptic();
        Sets.Add(new SetViewModel(this, 0));
        OnRepsChanged();
    }

    [RelayCommand]
    private void RemoveSet()
    {
        if (!IsEditable || Sets.Count <= 1)
            return;
        SessionViewModel.Haptic();
        Sets.RemoveAt(Sets.Count - 1);
        OnRepsChanged();
    }

    /// <summary>Called by set chips whenever a rep count changes.</summary>
    public void OnRepsChanged()
    {
        if (_loading)
            return;
        Entry.RepsPerSet = RepsSerializer.Serialize(Sets.Select(s => s.Reps));
        ProgressionEngine.ApplyAutoMark(Entry, _eff);
        RefreshMarkVisuals();
        Persist();
    }

    // ---- Marking ----

    [ObservableProperty]
    public partial string MarkGlyph { get; set; }

    [ObservableProperty]
    public partial Color MarkColor { get; set; }

    [ObservableProperty]
    public partial bool IsManualMark { get; set; }

    /// <summary>Left edge accent: transparent until data is logged, then the mark color.</summary>
    [ObservableProperty]
    public partial Color StripeColor { get; set; }

    [RelayCommand]
    private void CycleMark()
    {
        if (!IsEditable)
            return;
        SessionViewModel.Haptic();
        // Down -> Keep -> Up -> Down…; an explicit tap always counts as a manual override.
        Entry.Mark = Entry.Mark switch
        {
            Mark.Down => Mark.Keep,
            Mark.Keep => Mark.Up,
            _ => Mark.Down,
        };
        Entry.MarkIsManual = true;
        RefreshMarkVisuals();
        Persist();
    }

    [RelayCommand]
    private void ResetMark()
    {
        if (!IsEditable)
            return;
        Entry.MarkIsManual = false;
        ProgressionEngine.ApplyAutoMark(Entry, _eff);
        RefreshMarkVisuals();
        Persist();
    }

    private void RefreshMarkVisuals()
    {
        MarkGlyph = Entry.Mark switch
        {
            Mark.Up => "▲",
            Mark.Down => "▼",
            _ => "▬",
        };
        MarkColor = Entry.Mark switch
        {
            Mark.Up => Color.FromArgb("#2E8B57"),
            Mark.Down => Color.FromArgb("#C0564F"),
            _ => Color.FromArgb("#8A8F98"),
        };
        IsManualMark = Entry.MarkIsManual;
        StripeColor = HasLoggedData ? MarkColor : Colors.Transparent;
    }

    private void Persist() => _ = _db.SaveEntryAsync(Entry);
}

/// <summary>A single set's rep count, editable as a chip.</summary>
public partial class SetViewModel : ObservableObject
{
    private readonly SessionEntryViewModel _parent;

    public SetViewModel(SessionEntryViewModel parent, int reps)
    {
        _parent = parent;
        RepsText = reps > 0 ? reps.ToString() : "";
    }

    public int Reps => int.TryParse(RepsText, out var r) && r > 0 ? r : 0;

    [ObservableProperty]
    public partial string RepsText { get; set; }

    partial void OnRepsTextChanged(string value) => _parent.OnRepsChanged();
}
