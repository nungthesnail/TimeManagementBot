using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using TimeManagementBot.Data;
using TimeManagementBot.Interfaces;
using TimeManagementBot.Models;

namespace TimeManagementBot.Implementations;

public class EfTaskManager(ApplicationDbContext dbContext) : ITaskManager
{
    public async Task AddTaskAsync(long chatId, string description)
    {
        var task = new TaskItem
        {
            Description = description,
            ChatId = chatId
        };
        dbContext.Tasks.Add(task);
        await dbContext.SaveChangesAsync();
    }

    public Task<List<TaskItem>> GetIncompleteTasksAsync(long chatId)
    {
        return dbContext.Tasks
            .Where(x => !x.IsCompleted && x.ChatId == chatId)
            .ToListAsync();
    }

    public Task<TaskItem?> GetTaskByIdAsync(long chatId, long id)
        => dbContext.Tasks.FirstOrDefaultAsync(t => t.Id == id && t.ChatId == chatId);

    public Task CompleteTaskAsync(long chatId, long id)
    {
        return dbContext.Tasks
            .Where(x => x.Id == id && x.ChatId == chatId)
            .ExecuteUpdateAsync(setter => setter.SetProperty(static x => x.IsCompleted, true));
    }

    public async Task<SummaryDto> GetDaySummaryAsync(long chatId)
    {
        var completedTasksCount = await dbContext.Tasks.CountAsync(x => x.IsCompleted && x.ChatId == chatId);
        var totalTasksCount = await dbContext.Tasks.CountAsync(x => x.ChatId == chatId);
        var completionPercentage = totalTasksCount > 0 ? (double)completedTasksCount / totalTasksCount * 100 : 0;
        return new SummaryDto(completedTasksCount, totalTasksCount, completionPercentage);
    }

    public Task ResetCompletedTasksAsync(long chatId)
        => dbContext.Tasks.Where(x => x.IsCompleted && x.ChatId == chatId).ExecuteDeleteAsync();

    public Task DeleteTaskAsync(long chatId, long taskId)
        => dbContext.Tasks.Where(x => x.Id == taskId && x.ChatId == chatId).ExecuteDeleteAsync();

    public Task<int> GetIncompleteTasksCountAsync(long chatId)
        => dbContext.Tasks.CountAsync(x => !x.IsCompleted && x.ChatId == chatId);
}
