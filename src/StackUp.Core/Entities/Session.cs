using SQLite;

namespace StackUp.Core.Entities;

/// <summary>One workout. <see cref="CompletedAtUtc"/> null = in progress (resumable).</summary>
[Table("Session")]
public class Session
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [Indexed]
    public int SplitId { get; set; }

    public DateTime StartedAtUtc { get; set; }

    public DateTime? CompletedAtUtc { get; set; }
}
