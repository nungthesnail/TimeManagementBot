namespace TimeManagementBot.Models;

public class TaskItem
{
    public long Id { get; set; }
    public long ChatId { get; set; }
    // ReSharper disable once EntityFramework.ModelValidation.UnlimitedStringLength
    public required string Description { get; set; }
    public bool IsCompleted { get; set; }
    
    public static int MaxDescriptionLength => 75;
}
