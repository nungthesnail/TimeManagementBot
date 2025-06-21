using TimeManagementBot.Interfaces;

namespace TimeManagementBot.Implementations;

public class SummaryCreator(ITaskManager taskManager) : ISummaryCreator
{
    public async Task<string> GetDaySummaryAsync(long chatId)
    {
        var data = await taskManager.GetDaySummaryAsync(chatId);
        var summary = $"Статистика за день:\n\n" +
                      $"Всего задач: {data.TotalTasksCount}\n" +
                      $"Выполнено задач: {data.CompletedTasksCount}\n" +
                      $"Процент выполнения: {data.CompletionPercentage:F0}%\n\n" +
                      $"Невыполненные задачи будут перенесены на следующий день.";
        
        return summary;
    }
}
