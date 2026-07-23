using CommunityToolkit.Maui.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StackUp.App.Resources.Strings;
using StackUp.Core.Data;
using StackUp.Core.Entities;
using StackUp.Core.Enums;
using StackUp.Core.Units;

namespace StackUp.App.ViewModels;

/// <summary>Global defaults. Changes are saved immediately; invalid input is ignored until valid.</summary>
public partial class SettingsViewModel : ObservableObject
{
    private readonly AppDatabase _db;
    private AppSettings _settings = new();
    private bool _loading = true;

    public SettingsViewModel(AppDatabase db)
    {
        _db = db;
        RepMinText = RepMaxText = IncrementText = RepIncrementText = SetCountText = "";
    }

    /// <summary>Unit picker items; index matches <see cref="WeightUnit"/>.</summary>
    public string[] UnitOptions { get; } = [AppStrings.Settings_UnitKg, AppStrings.Settings_UnitLbs];

    /// <summary>Counting-set picker items; index matches <see cref="CountingSetRule"/>.</summary>
    public string[] CountingSetOptions { get; } =
        [AppStrings.Settings_FirstSet, AppStrings.Settings_LastSet, AppStrings.Settings_BestSet];

    /// <summary>0 = kg, 1 = lbs.</summary>
    [ObservableProperty] public partial int UnitIndex { get; set; }
    [ObservableProperty] public partial string RepMinText { get; set; }
    [ObservableProperty] public partial string RepMaxText { get; set; }
    [ObservableProperty] public partial string IncrementText { get; set; }
    [ObservableProperty] public partial string RepIncrementText { get; set; }
    [ObservableProperty] public partial string SetCountText { get; set; }
    /// <summary>Matches CountingSetRule order.</summary>
    [ObservableProperty] public partial int CountingSetRuleIndex { get; set; }

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

    // ---- Backup & restore ----

    [RelayCommand]
    private async Task CreateBackupAsync()
    {
        try
        {
            // Share covers other apps/cloud; Save covers the "keep a local copy first" step
            // (the share sheet can't write to local storage on Android).
            var choice = await Shell.Current.DisplayActionSheetAsync(AppStrings.Settings_CreateBackup,
                AppStrings.Common_Cancel, null, AppStrings.Backup_ShareOption, AppStrings.Backup_SaveOption);
            if (choice is null || choice == AppStrings.Common_Cancel)
                return;

            await _db.InitializeAsync();
            var fileName = $"stackup-backup-{DateTime.Now:yyyy-MM-dd}.db3";
            var path = Path.Combine(FileSystem.CacheDirectory, fileName);
            if (File.Exists(path))
                File.Delete(path);
            await _db.CreateBackupAsync(path);

            if (choice == AppStrings.Backup_ShareOption)
            {
                await Share.Default.RequestAsync(new ShareFileRequest
                {
                    Title = AppStrings.Backup_ShareTitle,
                    File = new ShareFile(path),
                });
                return;
            }

            // Save to device: system save dialog (SAF on Android — Downloads, SD card, …).
            using var stream = File.OpenRead(path);
            var result = await FileSaver.Default.SaveAsync(fileName, stream, CancellationToken.None);
            if (result.IsSuccessful)
                await Shell.Current.DisplayAlertAsync(AppStrings.Settings_CreateBackup,
                    string.Format(AppStrings.Backup_SavedFormat, result.FilePath), AppStrings.Common_OK);
            // Canceled dialogs land here as non-success — stay quiet.
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync(AppStrings.Backup_FailedTitle, ex.Message, AppStrings.Common_OK);
        }
    }

    [RelayCommand]
    private async Task RestoreBackupAsync()
    {
        try
        {
            // No file-type filter: .db3 has no reliable MIME type on Android; validation is the gate.
            var picked = await FilePicker.Default.PickAsync(new PickOptions { PickerTitle = AppStrings.Backup_PickerTitle });
            if (picked is null)
                return;

            // Android SAF hands out a stream, not a real path — stage a local copy first.
            var temp = Path.Combine(FileSystem.CacheDirectory, "restore-candidate.db3");
            using (var source = await picked.OpenReadAsync())
            using (var destination = File.Create(temp))
                await source.CopyToAsync(destination);

            switch (await AppDatabase.ValidateBackupFileAsync(temp))
            {
                case BackupFileStatus.Invalid:
                    await Shell.Current.DisplayAlertAsync(AppStrings.Settings_RestoreBackup,
                        AppStrings.Backup_InvalidFile, AppStrings.Common_OK);
                    return;
                case BackupFileStatus.NewerAppVersion:
                    await Shell.Current.DisplayAlertAsync(AppStrings.Settings_RestoreBackup,
                        AppStrings.Backup_NewerVersion, AppStrings.Common_OK);
                    return;
            }

            var confirmed = await Shell.Current.DisplayAlertAsync(AppStrings.Settings_RestoreBackup,
                AppStrings.Backup_RestoreConfirm, AppStrings.Backup_RestoreAction, AppStrings.Common_Cancel);
            if (!confirmed)
                return;

            await _db.RestoreFromFileAsync(temp);
            File.Delete(temp);
            await LoadAsync();
            await Shell.Current.DisplayAlertAsync(AppStrings.Settings_RestoreBackup,
                AppStrings.Backup_Restored, AppStrings.Common_OK);
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync(AppStrings.Backup_RestoreFailedTitle, ex.Message, AppStrings.Common_OK);
        }
    }

    // ---- Progression explainer (re-openable; the one-time flag is only set on the session screen) ----

    [ObservableProperty]
    public partial bool IsExplainerVisible { get; set; }

    [RelayCommand]
    private void ShowExplainer() => IsExplainerVisible = true;

    [RelayCommand]
    private void CloseExplainer() => IsExplainerVisible = false;

    // TODO before store release: replace with the dedicated feedback address.
    private const string FeedbackAddress = "feedback-address-not-set@example.com";

    public string VersionText => $"StackUp {AppInfo.Current.VersionString}";

    [RelayCommand]
    private async Task SendFeedbackAsync()
    {
        var subject = "StackUp — Feedback";
        try
        {
            await Email.Default.ComposeAsync(new EmailMessage { Subject = subject, To = [FeedbackAddress] });
        }
        catch (FeatureNotSupportedException)
        {
            // No mail app registered — fall back to a mailto: link.
            await Launcher.Default.OpenAsync(new Uri($"mailto:{FeedbackAddress}?subject={Uri.EscapeDataString(subject)}"));
        }
        catch
        {
            // User canceled or no handler at all — nothing to do.
        }
    }
}
