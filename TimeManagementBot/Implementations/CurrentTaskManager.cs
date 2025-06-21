using System.Collections.Concurrent;
using TimeManagementBot.Interfaces;
using TimeManagementBot.Models;

namespace TimeManagementBot.Implementations;

public class CurrentTaskManager : ICurrentTaskManager
{
    private readonly ConcurrentDictionary<long, TaskItem> _currentTasks = new();

    public TaskItem? GetCurrentTask(long chatId)
        => _currentTasks.GetValueOrDefault(chatId);
    
    public void ResetCurrentTask(long chatId)
        => _currentTasks.TryRemove(chatId, out _);

    public void SetCurrentTask(long chatId, TaskItem task)
        => _currentTasks.AddOrUpdate(chatId, task, (_, _) => task);
}
