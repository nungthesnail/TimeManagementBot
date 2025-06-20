using System.Collections.Concurrent;

namespace TimeManagementBot;

public class TaskManager
{
    private readonly List<TaskItem> _tasks = [];
    private readonly ConcurrentDictionary<long, TaskItem> _currentTasks = new();
    private int _taskIdCounter;

    public void AddTask(long chatId, string description)
    {
        var task = new TaskItem
        {
            Id = Interlocked.Increment(ref _taskIdCounter),
            Description = description,
            ChatId = chatId
        };
        _tasks.Add(task);
    }

    public List<TaskItem> GetIncompleteTasks(long chatId)
    {
        return _tasks
            .Where(x => !x.IsCompleted && x.ChatId == chatId)
            .ToList();
    }

    public TaskItem? GetTaskById(long chatId, long id)
    {
        return _tasks.FirstOrDefault(t => t.Id == id && t.ChatId == chatId);
    }

    public void CompleteTask(long chatId, long id)
    {
        var task = GetTaskById(chatId, id);
        if (task is not null)
        {
            task.IsCompleted = true;
        }
    }

    public string GetDaySummary(long chatId)
    {
        var completedTasksCount = _tasks.Count(x => x.IsCompleted && x.ChatId == chatId);
        var totalTasksCount = _tasks.Count(x => x.ChatId == chatId);
        var completionPercentage = totalTasksCount > 0 ? (double)completedTasksCount / totalTasksCount * 100 : 0;

        var summary = $"Статистика за день:\n\n" +
                      $"Всего задач: {totalTasksCount}\n" +
                      $"Выполнено задач: {completedTasksCount}\n" +
                      $"Процент выполнения: {completionPercentage:F0}%\n\n" +
                      $"Невыполненные задачи будут перенесены на следующий день.";

        return summary;
    }

    public void ResetCompletedTasks(long chatId)
        => _tasks.RemoveAll(x => x.IsCompleted && x.ChatId == chatId);

    public TaskItem? GetCurrentTask(long chatId)
        =>_currentTasks.GetValueOrDefault(chatId);
    
    public void ResetCurrentTask(long chatId)
        => _currentTasks.TryRemove(chatId, out _);

    public void SetCurrentTask(long chatId, long taskId)
    {
        var task = _tasks.FirstOrDefault(x => x.Id == taskId && x.ChatId == chatId);
        if (task is null)
            throw new InvalidOperationException($"Task with id={taskId} not found or not owned by chat with id={chatId}");
        _currentTasks.AddOrUpdate(chatId, task, (_, _) => task);
    }

    public void DeleteTask(long chatId, long currentTaskId)
    {
        _tasks.RemoveAll(x => x.Id == currentTaskId && x.ChatId == chatId);
    }
}
