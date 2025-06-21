using TimeManagementBot.Models;

namespace TimeManagementBot.Interfaces;

public interface IUserStateManager
{
    UserState GetUserState(long userId);
    UserState SetUserState(long userId, UserState state);
}