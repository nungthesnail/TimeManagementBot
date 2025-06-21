namespace TimeManagementBot.Interfaces;

public interface ISummaryCreator
{
    Task<string> GetDaySummaryAsync(long chatId);
}
