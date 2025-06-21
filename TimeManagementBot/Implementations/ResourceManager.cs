using System.Collections.ObjectModel;
using TimeManagementBot.Interfaces;

namespace TimeManagementBot.Implementations;

public class ResourceManager(ReadOnlyDictionary<string, string> replicas) : IResourceManager
{
    public string GetTextResource(string resourceName, params object?[] args)
    {
        var replica = replicas[resourceName];
        return string.Format(replica, args);
    }
}
