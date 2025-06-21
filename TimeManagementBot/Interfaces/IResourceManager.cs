namespace TimeManagementBot.Interfaces;

public interface IResourceManager
{
    string GetTextResource(string resourceName, params object?[] args);
}
