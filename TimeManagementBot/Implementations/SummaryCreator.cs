using TimeManagementBot.Interfaces;
using TimeManagementBot.Models;

namespace TimeManagementBot.Implementations;

public class SummaryCreator(ITaskManager taskManager, IResourceManager resourceManager) : ISummaryCreator
{
    public async Task<string> GetDaySummaryAsync(long chatId)
    {
        var data = await taskManager.GetDaySummaryAsync(chatId);
        var summary = resourceManager.GetTextResource(
            TextRes.Summary,
            data.TotalTasksCount,
            data.CompletedTasksCount,
            data.CompletionPercentage);
        
        return summary;
    }
}
