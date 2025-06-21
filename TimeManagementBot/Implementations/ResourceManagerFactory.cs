using System.Collections.ObjectModel;

namespace TimeManagementBot.Implementations;

public static class ResourceManagerFactory
{
    public static ResourceManager CreateFromConfiguration(IConfiguration config)
    {
        var dictionary = config.Get<Dictionary<string, string>>()
            ?? throw new InvalidOperationException("Can't get dictionary from resources configuration");
        var readOnlyDict = new ReadOnlyDictionary<string, string>(dictionary);
        return new ResourceManager(readOnlyDict);
    }
}
