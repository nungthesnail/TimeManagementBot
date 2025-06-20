namespace TimeManagementBot;

public class TaskItem
{
    public long Id { get; set; }
    public long ChatId { get; set; }
    public required string Description { get; set; }
    public bool IsCompleted { get; set; } = false;
    public DateTime? CreatedAt { get; set; } = DateTime.Now;
}
