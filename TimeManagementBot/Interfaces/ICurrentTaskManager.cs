using TimeManagementBot.Models;

namespace TimeManagementBot.Interfaces;

public interface ICurrentTaskManager
{
    TaskItem? GetCurrentTask(long chatId);
    void ResetCurrentTask(long chatId);
    void SetCurrentTask(long chatId, TaskItem task);
}
