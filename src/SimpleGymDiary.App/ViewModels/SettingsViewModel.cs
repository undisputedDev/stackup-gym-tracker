using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SimpleGymDiary.Core.Data;
using SimpleGymDiary.Core.Entities;
using SimpleGymDiary.Core.Enums;
using SimpleGymDiary.Core.Export;
using SimpleGymDiary.Core.Units;

namespace SimpleGymDiary.App.ViewModels;

/// <summary>Global defaults. Changes are saved immediately; invalid input is ignored until valid.</summary>
public partial class SettingsViewModel : ObservableObject
{
    private readonly AppDatabase _db;
    private AppSettings _settings = new();
    private bool _loading = true;

    public SettingsViewModel(AppDatabase db) => _db = db;

    [ObservableProperty] private int _unitIndex;              // 0 = kg, 1 = lbs
    [ObservableProperty] private string _repMinText = "";
    [ObservableProperty] private string _repMaxText = "";
    [ObservableProperty] private string _incrementText = "";
    [ObservableProperty] private string _repIncrementText = "";
    [ObservableProperty] private string _setCountText = "";
    [ObservableProperty] private int _countingSetRuleIndex;   // matches CountingSetRule order

    public async Task LoadAsync()
    {
        await _db.InitializeAsync();
        _settings = await _db.GetSettingsAsync();

        _loading = true;
        UnitIndex = (int)_settings.Unit;
        RepMinText = _settings.DefaultRepRangeMin.ToString();
        RepMaxText = _settings.DefaultRepRangeMax.ToString();
        IncrementText = _settings.DefaultWeightIncrementKg.ToString("0.##");
        RepIncrementText = _settings.DefaultRepIncrement.ToString();
        SetCountText = _settings.DefaultSetCount.ToString();
        CountingSetRuleIndex = (int)_settings.DefaultCountingSetRule;
        _loading = false;
    }

    partial void OnUnitIndexChanged(int value)
    {
        _settings.Unit = (WeightUnit)value;
        Save();
    }

    partial void OnRepMinTextChanged(string value)
    {
        if (int.TryParse(value, out var v) && v > 0)
        {
            _settings.DefaultRepRangeMin = v;
            Save();
        }
    }

    partial void OnRepMaxTextChanged(string value)
    {
        if (int.TryParse(value, out var v) && v > 0)
        {
            _settings.DefaultRepRangeMax = v;
            Save();
        }
    }

    partial void OnIncrementTextChanged(string value)
    {
        if (UnitConverter.TryParseFlexible(value, out var v) && v > 0)
        {
            _settings.DefaultWeightIncrementKg = v;
            Save();
        }
    }

    partial void OnRepIncrementTextChanged(string value)
    {
        if (int.TryParse(value, out var v) && v > 0)
        {
            _settings.DefaultRepIncrement = v;
            Save();
        }
    }

    partial void OnSetCountTextChanged(string value)
    {
        if (int.TryParse(value, out var v) && v is > 0 and <= 10)
        {
            _settings.DefaultSetCount = v;
            Save();
        }
    }

    partial void OnCountingSetRuleIndexChanged(int value)
    {
        _settings.DefaultCountingSetRule = (CountingSetRule)value;
        Save();
    }

    private void Save()
    {
        if (!_loading)
            _ = _db.SaveSettingsAsync(_settings);
    }

    [RelayCommand]
    private async Task ExportCsvAsync()
    {
        var rows = await _db.GetExportRowsAsync();
        if (rows.Count == 0)
        {
            await Shell.Current.DisplayAlertAsync("Nothing to export", "Complete a session first.", "OK");
            return;
        }

        var csv = CsvExporter.Export(rows);
        var path = Path.Combine(FileSystem.CacheDirectory, $"gym-diary-{DateTime.Now:yyyy-MM-dd}.csv");
        await File.WriteAllTextAsync(path, csv);

        await Share.Default.RequestAsync(new ShareFileRequest
        {
            Title = "Export gym diary",
            File = new ShareFile(path, "text/csv"),
        });
    }
}
