using TimeManagementBot.Models;

namespace TimeManagementBot.Interfaces;

public interface ITaskManager
{
    Task AddTaskAsync(long chatId, string description);
    Task<List<TaskItem>> GetIncompleteTasksAsync(long chatId);
    Task<TaskItem?> GetTaskByIdAsync(long chatId, long id);
    Task CompleteTaskAsync(long chatId, long id);
    Task<SummaryDto> GetDaySummaryAsync(long chatId);
    Task ResetCompletedTasksAsync(long chatId);
    Task DeleteTaskAsync(long chatId, long taskId);
    Task<int> GetIncompleteTasksCountAsync(long chatId);
    int MaxIncompleteTasksCount => 15;
}
