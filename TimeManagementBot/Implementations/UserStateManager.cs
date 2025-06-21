using System.Collections.Concurrent;
using TimeManagementBot.Interfaces;
using TimeManagementBot.Models;

namespace TimeManagementBot.Implementations;

public class UserStateManager : IUserStateManager
{
    private readonly ConcurrentDictionary<long, UserState> _userStates = new();
    
    public UserState GetUserState(long userId)
        => _userStates.GetOrAdd(userId, _ => UserState.None);
    public UserState SetUserState(long userId, UserState state)
        => _userStates.AddOrUpdate(userId, _ => state, (_, _) => state);
}
