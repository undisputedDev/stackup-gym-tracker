using System.Resources;

namespace StackUp.App.Resources.Strings;

/// <summary>
/// Typed access to AppStrings.resx; the device language (CurrentUICulture) picks the
/// satellite. Hand-written instead of a generated designer — keep in sync with the resx.
/// </summary>
public static class AppStrings
{
    private static readonly ResourceManager Rm =
        new("StackUp.App.Resources.Strings.AppStrings", typeof(AppStrings).Assembly);

    private static string Get(string key) => Rm.GetString(key) ?? key;

    // Tabs
    public static string Tab_Workout => Get(nameof(Tab_Workout));
    public static string Tab_Splits => Get(nameof(Tab_Splits));
    public static string Tab_Stats => Get(nameof(Tab_Stats));
    public static string Tab_Settings => Get(nameof(Tab_Settings));

    // Common
    public static string Common_OK => Get(nameof(Common_OK));
    public static string Common_Cancel => Get(nameof(Common_Cancel));
    public static string Common_Save => Get(nameof(Common_Save));
    public static string Common_Delete => Get(nameof(Common_Delete));
    public static string Common_Create => Get(nameof(Common_Create));

    // Workout home
    public static string Home_SessionInProgress => Get(nameof(Home_SessionInProgress));
    public static string Home_ResumeFormat => Get(nameof(Home_ResumeFormat));
    public static string Home_SessionFallback => Get(nameof(Home_SessionFallback));
    public static string Home_MomentumFormat => Get(nameof(Home_MomentumFormat));
    public static string Home_StartASession => Get(nameof(Home_StartASession));
    public static string Home_Start => Get(nameof(Home_Start));
    public static string Home_NotDoneYet => Get(nameof(Home_NotDoneYet));
    public static string Home_DoneToday => Get(nameof(Home_DoneToday));
    public static string Home_LastDoneYesterday => Get(nameof(Home_LastDoneYesterday));
    public static string Home_LastDoneDaysFormat => Get(nameof(Home_LastDoneDaysFormat));
    public static string Home_NoSplitsYet => Get(nameof(Home_NoSplitsYet));

    // Splits list
    public static string Splits_EmptyList => Get(nameof(Splits_EmptyList));
    public static string Splits_AddSemantic => Get(nameof(Splits_AddSemantic));
    public static string Splits_NewSplitTitle => Get(nameof(Splits_NewSplitTitle));
    public static string Splits_NewSplitPrompt => Get(nameof(Splits_NewSplitPrompt));
    public static string Splits_ExerciseCountFormat => Get(nameof(Splits_ExerciseCountFormat));

    // Split detail
    public static string SplitDetail_Rename => Get(nameof(SplitDetail_Rename));
    public static string SplitDetail_RenameTitle => Get(nameof(SplitDetail_RenameTitle));
    public static string SplitDetail_NamePrompt => Get(nameof(SplitDetail_NamePrompt));
    public static string SplitDetail_ExercisesHeader => Get(nameof(SplitDetail_ExercisesHeader));
    public static string SplitDetail_ReorderFooter => Get(nameof(SplitDetail_ReorderFooter));
    public static string SplitDetail_EmptyList => Get(nameof(SplitDetail_EmptyList));
    public static string SplitDetail_AddExercise => Get(nameof(SplitDetail_AddExercise));
    public static string SplitDetail_NewExerciseOption => Get(nameof(SplitDetail_NewExerciseOption));
    public static string SplitDetail_DeleteTitle => Get(nameof(SplitDetail_DeleteTitle));
    public static string SplitDetail_DeleteConfirmFormat => Get(nameof(SplitDetail_DeleteConfirmFormat));

    // Exercise edit
    public static string ExerciseEdit_NewTitle => Get(nameof(ExerciseEdit_NewTitle));
    public static string ExerciseEdit_EditTitle => Get(nameof(ExerciseEdit_EditTitle));
    public static string ExerciseEdit_Name => Get(nameof(ExerciseEdit_Name));
    public static string ExerciseEdit_NamePlaceholder => Get(nameof(ExerciseEdit_NamePlaceholder));
    public static string ExerciseEdit_Tracking => Get(nameof(ExerciseEdit_Tracking));
    public static string ExerciseEdit_TrackingWeight => Get(nameof(ExerciseEdit_TrackingWeight));
    public static string ExerciseEdit_TrackingReps => Get(nameof(ExerciseEdit_TrackingReps));
    public static string ExerciseEdit_Icon => Get(nameof(ExerciseEdit_Icon));
    public static string ExerciseEdit_OverridesHeader => Get(nameof(ExerciseEdit_OverridesHeader));
    public static string ExerciseEdit_DefaultFormat => Get(nameof(ExerciseEdit_DefaultFormat));
    public static string ExerciseEdit_MissingNameTitle => Get(nameof(ExerciseEdit_MissingNameTitle));
    public static string ExerciseEdit_MissingNameText => Get(nameof(ExerciseEdit_MissingNameText));
    public static string ExerciseEdit_DeleteTitle => Get(nameof(ExerciseEdit_DeleteTitle));
    public static string ExerciseEdit_DeleteConfirmFormat => Get(nameof(ExerciseEdit_DeleteConfirmFormat));

    // Shared field labels
    public static string Field_RepRangeMin => Get(nameof(Field_RepRangeMin));
    public static string Field_RepRangeMax => Get(nameof(Field_RepRangeMax));
    public static string Field_WeightIncrement => Get(nameof(Field_WeightIncrement));
    public static string Field_RepIncrement => Get(nameof(Field_RepIncrement));
    public static string Field_SetsPerExercise => Get(nameof(Field_SetsPerExercise));

    // Session
    public static string Session_WorkoutFallback => Get(nameof(Session_WorkoutFallback));
    public static string Session_InProgress => Get(nameof(Session_InProgress));
    public static string Session_ReadOnlyFormat => Get(nameof(Session_ReadOnlyFormat));
    public static string Session_RepsPerSet => Get(nameof(Session_RepsPerSet));
    public static string Session_TargetFormat => Get(nameof(Session_TargetFormat));
    public static string Session_SuggestedFormat => Get(nameof(Session_SuggestedFormat));
    public static string Session_DeloadFormat => Get(nameof(Session_DeloadFormat));
    public static string Session_AimForFormat => Get(nameof(Session_AimForFormat));
    public static string Session_LastFormat => Get(nameof(Session_LastFormat));
    public static string Session_LastRepsFormat => Get(nameof(Session_LastRepsFormat));
    public static string Session_Finish => Get(nameof(Session_Finish));
    public static string Session_Complete => Get(nameof(Session_Complete));
    public static string Session_DurationFormat => Get(nameof(Session_DurationFormat));
    public static string Session_NextTime => Get(nameof(Session_NextTime));
    public static string Session_AllKeep => Get(nameof(Session_AllKeep));
    public static string Session_SummaryWeightFormat => Get(nameof(Session_SummaryWeightFormat));
    public static string Session_DeloadSuffix => Get(nameof(Session_DeloadSuffix));
    public static string Session_SummaryAimFormat => Get(nameof(Session_SummaryAimFormat));
    public static string Session_NewBestWeightFormat => Get(nameof(Session_NewBestWeightFormat));
    public static string Session_NewBestRepsFormat => Get(nameof(Session_NewBestRepsFormat));
    public static string Session_Done => Get(nameof(Session_Done));
    public static string Session_Discard => Get(nameof(Session_Discard));
    public static string Session_DiscardConfirm => Get(nameof(Session_DiscardConfirm));
    public static string Session_DiscardAction => Get(nameof(Session_DiscardAction));
    public static string Session_Delete => Get(nameof(Session_Delete));
    public static string Session_DeleteConfirm => Get(nameof(Session_DeleteConfirm));

    // Live consequence hint
    public static string Session_HintRepsPrefixFormat => Get(nameof(Session_HintRepsPrefixFormat));
    public static string Session_HintNextTimeFormat => Get(nameof(Session_HintNextTimeFormat));
    public static string Session_HintStaysFormat => Get(nameof(Session_HintStaysFormat));
    public static string Session_HintNextAimFormat => Get(nameof(Session_HintNextAimFormat));
    public static string Session_HintAimAgainFormat => Get(nameof(Session_HintAimAgainFormat));
    public static string Session_HintDeloadFormat => Get(nameof(Session_HintDeloadFormat));

    // Progression explainer
    public static string Explainer_Title => Get(nameof(Explainer_Title));
    public static string Explainer_Keep => Get(nameof(Explainer_Keep));
    public static string Explainer_Up => Get(nameof(Explainer_Up));
    public static string Explainer_Down => Get(nameof(Explainer_Down));
    public static string Explainer_Override => Get(nameof(Explainer_Override));
    public static string Explainer_GotIt => Get(nameof(Explainer_GotIt));

    // Stats
    public static string Stats_Filter => Get(nameof(Stats_Filter));
    public static string Stats_Range3M => Get(nameof(Stats_Range3M));
    public static string Stats_Range1Y => Get(nameof(Stats_Range1Y));
    public static string Stats_RangeAll => Get(nameof(Stats_RangeAll));
    public static string Stats_Reps => Get(nameof(Stats_Reps));
    public static string Stats_NoDataYet => Get(nameof(Stats_NoDataYet));
    public static string Stats_EmptyFiltered => Get(nameof(Stats_EmptyFiltered));

    // Settings
    public static string Settings_Units => Get(nameof(Settings_Units));
    public static string Settings_UnitKg => Get(nameof(Settings_UnitKg));
    public static string Settings_UnitLbs => Get(nameof(Settings_UnitLbs));
    public static string Settings_ProgressionDefaults => Get(nameof(Settings_ProgressionDefaults));
    public static string Settings_CountingSetQuestion => Get(nameof(Settings_CountingSetQuestion));
    public static string Settings_FirstSet => Get(nameof(Settings_FirstSet));
    public static string Settings_LastSet => Get(nameof(Settings_LastSet));
    public static string Settings_BestSet => Get(nameof(Settings_BestSet));
    public static string Settings_Data => Get(nameof(Settings_Data));
    public static string Settings_CreateBackup => Get(nameof(Settings_CreateBackup));
    public static string Settings_RestoreBackup => Get(nameof(Settings_RestoreBackup));
    public static string Settings_Help => Get(nameof(Settings_Help));
    public static string Settings_HowProgressionWorks => Get(nameof(Settings_HowProgressionWorks));
    public static string Settings_About => Get(nameof(Settings_About));
    public static string Settings_SendFeedback => Get(nameof(Settings_SendFeedback));

    // Backup & restore
    public static string Backup_ShareTitle => Get(nameof(Backup_ShareTitle));
    public static string Backup_ShareOption => Get(nameof(Backup_ShareOption));
    public static string Backup_SaveOption => Get(nameof(Backup_SaveOption));
    public static string Backup_SavedFormat => Get(nameof(Backup_SavedFormat));
    public static string Backup_PickerTitle => Get(nameof(Backup_PickerTitle));
    public static string Backup_InvalidFile => Get(nameof(Backup_InvalidFile));
    public static string Backup_NewerVersion => Get(nameof(Backup_NewerVersion));
    public static string Backup_RestoreConfirm => Get(nameof(Backup_RestoreConfirm));
    public static string Backup_RestoreAction => Get(nameof(Backup_RestoreAction));
    public static string Backup_Restored => Get(nameof(Backup_Restored));
    public static string Backup_FailedTitle => Get(nameof(Backup_FailedTitle));
    public static string Backup_RestoreFailedTitle => Get(nameof(Backup_RestoreFailedTitle));
}
