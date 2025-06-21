using TimeManagementBot.Interfaces;
using TimeManagementBot.Models;

namespace TimeManagementBot.Implementations;

[Obsolete("Stores tasks in the memory and uses thread-unsafe List`1 to save tasks")]
public class BasicUnsafeTaskManager : ITaskManager
{
    private readonly List<TaskItem> _tasks = [];
    private int _taskIdCounter;

    public Task AddTaskAsync(long chatId, string description)
    {
        var task = new TaskItem
        {
            Id = Interlocked.Increment(ref _taskIdCounter),
            Description = description,
            ChatId = chatId
        };
        _tasks.Add(task);
        return Task.CompletedTask;
    }

    public Task<List<TaskItem>> GetIncompleteTasksAsync(long chatId)
    {
        return Task.FromResult(_tasks
            .Where(x => !x.IsCompleted && x.ChatId == chatId)
            .ToList());
    }

    public Task<TaskItem?> GetTaskByIdAsync(long chatId, long id)
    {
        return Task.FromResult(_tasks.FirstOrDefault(t => t.Id == id && t.ChatId == chatId));
    }

    public Task CompleteTaskAsync(long chatId, long id)
    {
        var task = GetTaskByIdAsync(chatId, id).Result;
        if (task is not null)
        {
            task.IsCompleted = true;
        }
        return Task.CompletedTask;
    }

    public Task<SummaryDto> GetDaySummaryAsync(long chatId)
    {
        var completedTasksCount = _tasks.Count(x => x.IsCompleted && x.ChatId == chatId);
        var totalTasksCount = _tasks.Count(x => x.ChatId == chatId);
        var completionPercentage = totalTasksCount > 0 ? (double)completedTasksCount / totalTasksCount * 100 : 0;
        return Task.FromResult(new SummaryDto(completedTasksCount, totalTasksCount, completionPercentage));
    }

    public Task ResetCompletedTasksAsync(long chatId)
    {
        _tasks.RemoveAll(x => x.IsCompleted && x.ChatId == chatId);
        return Task.CompletedTask;
    }

    public Task DeleteTaskAsync(long chatId, long taskId)
    {
        _tasks.RemoveAll(x => x.Id == taskId && x.ChatId == chatId);
        return Task.CompletedTask;
    }

    public Task<int> GetIncompleteTasksCountAsync(long chatId)
        => Task.FromResult(_tasks.Count(x => !x.IsCompleted && x.ChatId == chatId));
}
