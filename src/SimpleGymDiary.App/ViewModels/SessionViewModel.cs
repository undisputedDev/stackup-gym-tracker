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

    public SessionViewModel(AppDatabase db) => _db = db;

    [ObservableProperty]
    private int _sessionId;

    [ObservableProperty]
    private string _title = "";

    public ObservableCollection<SessionEntryViewModel> Entries { get; } = [];

    public async Task LoadAsync()
    {
        await _db.InitializeAsync();

        var session = await _db.GetSessionAsync(SessionId);
        if (session is null)
            return;

        var split = await _db.GetSplitAsync(session.SplitId);
        Title = split?.Name ?? "Workout";

        var settings = await _db.GetSettingsAsync();
        var entries = await _db.GetSessionEntriesAsync(SessionId);

        Entries.Clear();
        foreach (var entry in entries)
        {
            var exercise = await _db.GetExerciseAsync(entry.ExerciseId);
            if (exercise is null)
                continue;
            Entries.Add(new SessionEntryViewModel(_db, entry, exercise, settings));
        }
    }

    [RelayCommand]
    private async Task FinishAsync()
    {
        await _db.CompleteSessionAsync(SessionId, DateTime.UtcNow);
        await Shell.Current.GoToAsync("..");
    }
}

/// <summary>One exercise card on the session screen. Every change is persisted immediately (no Save button).</summary>
public partial class SessionEntryViewModel : ObservableObject
{
    private readonly AppDatabase _db;
    private readonly EffectiveExerciseSettings _eff;
    private readonly WeightUnit _unit;
    private bool _loading = true;

    public SessionEntry Entry { get; }
    public Exercise Exercise { get; }

    public SessionEntryViewModel(AppDatabase db, SessionEntry entry, Exercise exercise, AppSettings settings)
    {
        _db = db;
        Entry = entry;
        Exercise = exercise;
        _unit = settings.Unit;
        _eff = EffectiveExerciseSettings.Resolve(exercise, settings);

        _weightText = entry.WeightKg is { } w ? UnitConverter.Format(w, _unit) : "";
        foreach (var reps in RepsSerializer.Parse(entry.RepsPerSet))
            Sets.Add(new SetViewModel(this, reps));

        _loading = false;
        RefreshMarkVisuals();
    }

    public string Name => Exercise.Name;
    public bool IsWeightBased => Exercise.TrackingType == TrackingType.WeightBased;
    public string UnitLabel => UnitConverter.UnitLabel(_unit);

    public string TargetHint
    {
        get
        {
            var range = $"Target {_eff.RepMin}–{_eff.RepMax} reps";
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
    private string _weightText = "";

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
        Sets.Add(new SetViewModel(this, 0));
        OnRepsChanged();
    }

    [RelayCommand]
    private void RemoveSet()
    {
        if (Sets.Count <= 1)
            return;
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
    private string _markGlyph = "=";

    [ObservableProperty]
    private Color _markColor = Colors.Gray;

    [ObservableProperty]
    private bool _isManualMark;

    [RelayCommand]
    private void CycleMark()
    {
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
        _repsText = reps > 0 ? reps.ToString() : "";
    }

    public int Reps => int.TryParse(RepsText, out var r) && r > 0 ? r : 0;

    [ObservableProperty]
    private string _repsText = "";

    partial void OnRepsTextChanged(string value) => _parent.OnRepsChanged();
}
