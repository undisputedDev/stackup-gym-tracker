namespace StackUp.Core.Data;

/// <summary>Result of <see cref="AppDatabase.ValidateBackupFileAsync"/>.</summary>
public enum BackupFileStatus
{
    Valid,

    /// <summary>Not a StackUp database (corrupt, wrong format, or missing core tables).</summary>
    Invalid,

    /// <summary>Written by a newer app version (user_version above this build's migrations).</summary>
    NewerAppVersion,
}
