using System.Collections.Generic;

public interface IAnalyticsManager : IService
{
    void LogEvent(string eventName, Dictionary<string, object> parameters = null);
}
